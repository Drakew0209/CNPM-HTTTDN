using InternetCafe.Backend.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace InternetCafe.Backend.Services;

public sealed class AuthService(SqlConnectionFactory connections)
{
    private readonly PasswordHasher<object> passwords = new();

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password)) return null;
        await using var connection = await connections.OpenAsync(ct);
        await using var command = new SqlCommand("""
            SELECT Account_ID, Username, Password_Hash, Role, Customer_ID, Employee_ID, Full_Name, Status
            FROM dbo.App_Accounts WHERE Username = @username
            """, connection);
        command.Parameters.AddWithValue("@username", request.Username.Trim());
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        var account = new AccountRow(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), ReadInt(reader, 4), ReadInt(reader, 5), reader.GetString(6), reader.GetString(7));
        if (account.Status != "Active" || passwords.VerifyHashedPassword(new object(), account.PasswordHash, request.Password) == PasswordVerificationResult.Failed) return null;
        await reader.CloseAsync();
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        await using var token = new SqlCommand("INSERT INTO dbo.App_Tokens (Account_ID, Token_Hash, Expires_At) VALUES (@accountId, @hash, DATEADD(hour, 8, SYSUTCDATETIME()))", connection);
        token.Parameters.AddWithValue("@accountId", account.AccountId);
        token.Parameters.AddWithValue("@hash", TokenHash(rawToken));
        await token.ExecuteNonQueryAsync(ct);
        return new LoginResponse(rawToken, new ApiUser(EntityId(account), account.Username, account.FullName, account.Role, Permissions(account.Role)));
    }

    public async Task LogoutAsync(int accountId, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        await using var command = new SqlCommand("UPDATE dbo.App_Tokens SET Revoked_At = SYSUTCDATETIME() WHERE Account_ID = @accountId AND Revoked_At IS NULL", connection);
        command.Parameters.AddWithValue("@accountId", accountId);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<OperationResult<object>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (request.Username.Length is < 3 or > 50 || request.Password.Length < 8 || string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Phone))
            return OperationResult<object>.Fail("VALIDATION_ERROR", "Kiểm tra lại tên đăng nhập, mật khẩu, họ tên và số điện thoại.");
        await using var connection = await connections.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            var username = request.Username.Trim();
            await using var exists = new SqlCommand("SELECT COUNT(*) FROM dbo.App_Accounts WHERE Username = @username UNION ALL SELECT COUNT(*) FROM dbo.Customers WHERE Username = @username", connection, (SqlTransaction)transaction);
            exists.Parameters.AddWithValue("@username", username);
            await using var existsReader = await exists.ExecuteReaderAsync(ct);
            var taken = false;
            while (await existsReader.ReadAsync(ct)) taken |= existsReader.GetInt32(0) > 0;
            await existsReader.CloseAsync();
            if (taken) { await transaction.RollbackAsync(ct); return OperationResult<object>.Fail("USERNAME_EXISTS", "Tên đăng nhập đã được sử dụng.", StatusCodes.Status409Conflict); }

            await using var customer = new SqlCommand("""
                INSERT INTO dbo.Customers (Username, Password_Hash, Full_Name, Phone_Number, Email, Date_Of_Birth, Hobbies, Tier_ID, Status)
                OUTPUT inserted.Customer_ID
                VALUES (@username, @passwordHash, @fullName, @phone, @email, @dateOfBirth, @hobbies,
                    COALESCE((SELECT TOP 1 Tier_ID FROM dbo.Membership_Tier WHERE Tier_Name = 'Member'), 1), 'Active')
                """, connection, (SqlTransaction)transaction);
            customer.Parameters.AddWithValue("@username", username);
            customer.Parameters.AddWithValue("@passwordHash", passwords.HashPassword(new object(), request.Password));
            customer.Parameters.AddWithValue("@fullName", request.FullName.Trim());
            customer.Parameters.AddWithValue("@phone", request.Phone.Trim());
            customer.Parameters.AddWithValue("@email", (object?)request.Email?.Trim() ?? DBNull.Value);
            customer.Parameters.AddWithValue("@dateOfBirth", ParseDate(request.DateOfBirth));
            customer.Parameters.AddWithValue("@hobbies", (object?)request.Hobbies?.Trim() ?? DBNull.Value);
            var customerId = Convert.ToInt32(await customer.ExecuteScalarAsync(ct));
            await using var account = new SqlCommand("""
                INSERT INTO dbo.App_Accounts (Username, Password_Hash, Role, Customer_ID, Full_Name, Status)
                VALUES (@username, @passwordHash, 'Customer', @customerId, @fullName, 'Active')
                """, connection, (SqlTransaction)transaction);
            account.Parameters.AddWithValue("@username", username);
            account.Parameters.AddWithValue("@passwordHash", passwords.HashPassword(new object(), request.Password));
            account.Parameters.AddWithValue("@customerId", customerId);
            account.Parameters.AddWithValue("@fullName", request.FullName.Trim());
            await account.ExecuteNonQueryAsync(ct);
            await transaction.CommitAsync(ct);
            return OperationResult<object>.Ok(new { id = $"c-{customerId:000}", username, fullName = request.FullName.Trim(), phone = request.Phone.Trim(), email = request.Email?.Trim() ?? "", dateOfBirth = request.DateOfBirth ?? "", hobbies = request.Hobbies ?? "", tier = "Member", balance = 0m, status = "Active" });
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(ct);
            return OperationResult<object>.Fail("USERNAME_EXISTS", "Tên đăng nhập đã được sử dụng.", StatusCodes.Status409Conflict);
        }
    }

    public async Task<OperationResult<object>> ChangePasswordAsync(int accountId, ChangePasswordRequest request, CancellationToken ct)
    {
        if (request.NewPassword.Length < 8) return OperationResult<object>.Fail("INVALID_PASSWORD", "Mật khẩu mới cần tối thiểu 8 ký tự.");
        await using var connection = await connections.OpenAsync(ct);
        await using var find = new SqlCommand("SELECT Password_Hash FROM dbo.App_Accounts WHERE Account_ID = @id", connection);
        find.Parameters.AddWithValue("@id", accountId);
        var currentHash = (string?)await find.ExecuteScalarAsync(ct);
        if (currentHash is null || passwords.VerifyHashedPassword(new object(), currentHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            return OperationResult<object>.Fail("INVALID_PASSWORD", "Mật khẩu hiện tại không đúng.");
        await using var update = new SqlCommand("UPDATE dbo.App_Accounts SET Password_Hash = @hash, Updated_At = SYSUTCDATETIME() WHERE Account_ID = @id; UPDATE dbo.App_Tokens SET Revoked_At = SYSUTCDATETIME() WHERE Account_ID = @id AND Revoked_At IS NULL;", connection);
        update.Parameters.AddWithValue("@id", accountId);
        update.Parameters.AddWithValue("@hash", passwords.HashPassword(new object(), request.NewPassword));
        await update.ExecuteNonQueryAsync(ct);
        return OperationResult<object>.Ok(new { changed = true });
    }

    public static string TokenHash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    public static string[] Permissions(string role) => role switch
    {
        Roles.Admin => ["accounts:read", "accounts:write", "products:read", "suppliers:read"],
        Roles.Owner or Roles.Manager => ["management:write", "operations:write", "reports:read"],
        Roles.Cashier => ["operations:write", "reports:read"],
        _ => ["self:read", "self:write"]
    };
    private static object ParseDate(string? value) => DateOnly.TryParse(value, out var date) ? date : DBNull.Value;
    private static int? ReadInt(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    private static string EntityId(AccountRow account) => account.CustomerId is { } customer ? $"c-{customer:000}" : account.EmployeeId is { } employee ? $"e-{employee:000}" : $"a-{account.AccountId:000}";
    private sealed record AccountRow(int AccountId, string Username, string PasswordHash, string Role, int? CustomerId, int? EmployeeId, string FullName, string Status);
}

public sealed record ApiUser(string Id, string Username, string FullName, string Role, string[] Permissions);
public sealed record LoginResponse(string AccessToken, ApiUser User);
