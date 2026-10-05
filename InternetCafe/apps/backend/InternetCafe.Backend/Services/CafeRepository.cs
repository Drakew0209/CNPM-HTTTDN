using InternetCafe.Backend.Data;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace InternetCafe.Backend.Services;

public sealed class CafeRepository(SqlConnectionFactory connections, ManagementRepository management)
{
    public async Task<object> WorkspaceAsync(IdentityContext identity, CancellationToken ct)
    {
        var customers = await CustomersAsync(identity, ct);
        var computers = await ComputersAsync(identity, ct);
        var categories = await CategoriesAsync(identity, ct);
        var products = await ProductsAsync(identity, ct);
        var orders = await OrdersAsync(identity, ct);
        var topups = await TopupsAsync(identity, ct);
        var sessions = await SessionsAsync(identity, ct);
        var transactions = await TransactionsAsync(identity, ct);
        var managementWorkspace = await management.WorkspaceAsync(identity, ct);
        return new
        {
            computers, customers, products, categories, orders, topups, transactions,
            suppliers = managementWorkspace.Suppliers, inventory = managementWorkspace.Inventory, feedback = managementWorkspace.Feedback, surveys = managementWorkspace.Surveys,
            employees = managementWorkspace.Employees, shifts = managementWorkspace.Shifts, schedules = managementWorkspace.Schedules, leaves = managementWorkspace.Leaves,
            attendance = managementWorkspace.Attendance, payroll = managementWorkspace.Payroll, accounts = managementWorkspace.Accounts, sessions
        };
    }

    public async Task<object> MeAsync(IdentityContext identity, CancellationToken ct)
    {
        if (identity.CustomerId is not null)
        {
            var customer = (await CustomersAsync(identity, ct)).Single();
            return customer;
        }
        if (identity.EmployeeId is not null)
        {
            var employee = (await management.EmployeesAsync(identity, ct)).SingleOrDefault();
            if (employee is not null) return employee;
        }
        return new { id = $"a-{identity.AccountId:000}", username = identity.Username, role = identity.Role };
    }

    public async Task<OperationResult<object>> UpdateMeAsync(IdentityContext identity, JsonElement body, CancellationToken ct)
    {
        if (identity.CustomerId is null && identity.EmployeeId is null)
            return OperationResult<object>.Fail("FORBIDDEN", "Tài khoản này không có hồ sơ cá nhân để cập nhật.", StatusCodes.Status403Forbidden);
        await using var connection = await connections.OpenAsync(ct);
        if (identity.EmployeeId is not null)
        {
            await using var employee = new SqlCommand("""
                UPDATE dbo.Employees SET Full_Name = COALESCE(@fullName, Full_Name), Phone_Number = COALESCE(@phone, Phone_Number),
                    Email = COALESCE(@email, Email), Qualification = COALESCE(@qualification, Qualification)
                WHERE Employee_ID = @id
                """, connection);
            employee.Parameters.AddWithValue("@id", identity.EmployeeId.Value);
            employee.Parameters.AddWithValue("@fullName", ReadString(body, "fullName") ?? (object)DBNull.Value);
            employee.Parameters.AddWithValue("@phone", ReadString(body, "phone") ?? (object)DBNull.Value);
            employee.Parameters.AddWithValue("@email", ReadString(body, "email") ?? (object)DBNull.Value);
            employee.Parameters.AddWithValue("@qualification", ReadString(body, "qualification") ?? (object)DBNull.Value);
            await employee.ExecuteNonQueryAsync(ct);
            return OperationResult<object>.Ok(await MeAsync(identity, ct));
        }
        await using var command = new SqlCommand("""
            UPDATE dbo.Customers SET Full_Name = COALESCE(@fullName, Full_Name), Phone_Number = COALESCE(@phone, Phone_Number),
                Email = COALESCE(@email, Email), Date_Of_Birth = COALESCE(@dateOfBirth, Date_Of_Birth), Hobbies = COALESCE(@hobbies, Hobbies)
            WHERE Customer_ID = @id
            """, connection);
        command.Parameters.AddWithValue("@id", identity.CustomerId!.Value);
        command.Parameters.AddWithValue("@fullName", ReadString(body, "fullName") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@phone", ReadString(body, "phone") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@email", ReadString(body, "email") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@hobbies", ReadString(body, "hobbies") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@dateOfBirth", DateOnly.TryParse(ReadString(body, "dateOfBirth"), out var date) ? date : (object)DBNull.Value);
        await command.ExecuteNonQueryAsync(ct);
        return OperationResult<object>.Ok(await MeAsync(identity, ct));
    }

    public Task<IReadOnlyList<ComputerDto>> ComputersAsync(IdentityContext identity, CancellationToken ct) => QueryAsync("""
        SELECT c.Computer_ID, c.Computer_Code, c.Zone_Type, c.Status, c.Hourly_Rate,
            CASE WHEN h.Last_Seen_At IS NULL OR h.Last_Seen_At >= DATEADD(second, -30, SYSUTCDATETIME()) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsOnline,
            s.Customer_ID, s.Session_ID
        FROM dbo.Computers c
        LEFT JOIN dbo.Machine_Heartbeats h ON h.Computer_ID = c.Computer_ID
        OUTER APPLY (SELECT TOP 1 Customer_ID, Session_ID FROM dbo.Usage_Sessions WHERE Computer_ID = c.Computer_ID AND Status = 'Active' ORDER BY Session_ID DESC) s
        ORDER BY c.Computer_Code
        """, reader => new ComputerDto(Id("pc", reader.GetInt32(0)), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetBoolean(5), reader.GetDecimal(4), NullableId("c", reader, 6), NullableId("session", reader, 7)), ct);

    public Task<IReadOnlyList<CategoryDto>> CategoriesAsync(IdentityContext identity, CancellationToken ct) => QueryAsync("SELECT Category_ID, Category_Name FROM dbo.Product_Categories ORDER BY Category_Name", reader => new CategoryDto(Id("cat", reader.GetInt32(0)), reader.GetString(1)), ct);

    public Task<IReadOnlyList<ProductDto>> ProductsAsync(IdentityContext identity, CancellationToken ct) => QueryAsync("""
        SELECT Product_ID, Product_Name, Category_ID, Price, Stock_Quantity, Status, Image_Url FROM dbo.Products
        WHERE (@customer = 0 OR Status = 'Active') ORDER BY Product_Name
        """, reader => new ProductDto(Id("p", reader.GetInt32(0)), reader.GetString(1), Id("cat", reader.GetInt32(2)), reader.GetDecimal(3), reader.GetInt32(4), reader.GetString(5) == "Active", reader.IsDBNull(6) ? null : reader.GetString(6)), ct, ("@customer", identity.Role == Roles.Customer ? 1 : 0));

    public Task<IReadOnlyList<CustomerDto>> CustomersAsync(IdentityContext identity, CancellationToken ct)
    {
        var ownOnly = identity.Role is Roles.Customer or Roles.Staff;
        return QueryAsync("""
            SELECT c.Customer_ID, c.Username, c.Full_Name, c.Phone_Number, c.Email, c.Date_Of_Birth, c.Hobbies, t.Tier_Name, c.Balance, c.Status
            FROM dbo.Customers c JOIN dbo.Membership_Tier t ON t.Tier_ID = c.Tier_ID
            WHERE (@ownOnly = 0 OR c.Customer_ID = @customerId) ORDER BY c.Full_Name
            """, reader => new CustomerDto(Id("c", reader.GetInt32(0)), reader.GetString(1), reader.GetString(2), StringValue(reader, 3), StringValue(reader, 4), DateValue(reader, 5), StringValue(reader, 6), reader.GetString(7), reader.GetDecimal(8), reader.GetString(9)), ct,
            ("@ownOnly", ownOnly ? 1 : 0), ("@customerId", (object?)identity.CustomerId ?? DBNull.Value));
    }

    public async Task<IReadOnlyList<OrderDto>> OrdersAsync(IdentityContext identity, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        await using var command = new SqlCommand("""
            SELECT Order_ID, Customer_ID, Computer_ID, Total_Amount, Status, Notes, Order_Date
            FROM dbo.Orders WHERE (@customer = 0 OR Customer_ID = @customerId) ORDER BY Order_Date DESC, Order_ID DESC
            """, connection);
        command.Parameters.AddWithValue("@customer", identity.Role == Roles.Customer ? 1 : 0);
        command.Parameters.AddWithValue("@customerId", (object?)identity.CustomerId ?? DBNull.Value);
        var orders = new List<OrderDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            orders.Add(new OrderDto(Id("order", reader.GetInt32(0)), Id("c", reader.GetInt32(1)), NullableId("pc", reader, 2), [], reader.GetDecimal(3), reader.GetString(4), StringValue(reader, 5), reader.GetDateTime(6)));
        await reader.CloseAsync();
        for (var index = 0; index < orders.Count; index++)
        {
            await using var details = new SqlCommand("SELECT d.Product_ID, p.Product_Name, d.Quantity, d.Unit_Price FROM dbo.Order_Details d JOIN dbo.Products p ON p.Product_ID = d.Product_ID WHERE d.Order_ID = @id", connection);
            details.Parameters.AddWithValue("@id", ParseId(orders[index].Id));
            await using var detailReader = await details.ExecuteReaderAsync(ct);
            var items = new List<OrderItemDto>();
            while (await detailReader.ReadAsync(ct)) items.Add(new OrderItemDto(Id("p", detailReader.GetInt32(0)), detailReader.GetString(1), detailReader.GetInt32(2), detailReader.GetDecimal(3)));
            orders[index] = orders[index] with { Items = items };
        }
        return orders;
    }

    public Task<IReadOnlyList<TopupDto>> TopupsAsync(IdentityContext identity, CancellationToken ct) => QueryAsync("""
        SELECT TopUp_ID, Customer_ID, Amount, Status, Note, Requested_At FROM dbo.TopUp_Requests
        WHERE (@customer = 0 OR Customer_ID = @customerId) ORDER BY Requested_At DESC, TopUp_ID DESC
        """, reader => new TopupDto(Id("topup", reader.GetInt32(0)), Id("c", reader.GetInt32(1)), reader.GetDecimal(2), reader.GetString(3), StringValue(reader, 4), reader.GetDateTime(5)), ct,
        ("@customer", identity.Role == Roles.Customer ? 1 : 0), ("@customerId", (object?)identity.CustomerId ?? DBNull.Value));

    public Task<IReadOnlyList<SessionDto>> SessionsAsync(IdentityContext identity, CancellationToken ct) => QueryAsync("""
        SELECT Session_ID, Customer_ID, Computer_ID, Start_Time, End_Time, Start_Balance, Amount, Status, Applied_Hourly_Rate
        FROM dbo.Usage_Sessions WHERE (@customer = 0 OR Customer_ID = @customerId) ORDER BY Start_Time DESC, Session_ID DESC
        """, reader => new SessionDto(Id("session", reader.GetInt32(0)), Id("c", reader.GetInt32(1)), Id("pc", reader.GetInt32(2)), reader.GetDateTime(3), NullableDate(reader, 4), DecimalValue(reader, 5), DecimalValue(reader, 6), reader.GetString(7), DecimalValue(reader, 8)), ct,
        ("@customer", identity.Role == Roles.Customer ? 1 : 0), ("@customerId", (object?)identity.CustomerId ?? DBNull.Value));

    public Task<IReadOnlyList<TransactionDto>> TransactionsAsync(IdentityContext identity, CancellationToken ct) => QueryAsync("""
        SELECT Transaction_ID, Customer_ID, Trans_Type, Amount, COALESCE(Order_ID, Session_ID, Transaction_ID), Trans_Date
        FROM dbo.Transactions WHERE (@customer = 0 OR Customer_ID = @customerId) ORDER BY Trans_Date DESC, Transaction_ID DESC
        """, reader => new TransactionDto(Id("tx", reader.GetInt32(0)), Id("c", reader.GetInt32(1)), reader.GetString(2), reader.GetDecimal(3), reader.GetInt32(4).ToString(), reader.GetDateTime(5)), ct,
        ("@customer", identity.Role == Roles.Customer ? 1 : 0), ("@customerId", (object?)identity.CustomerId ?? DBNull.Value));

    public async Task<object> ReportAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        await using var command = new SqlCommand("""
            SELECT Trans_Type, COALESCE(SUM(Amount), 0) FROM dbo.Transactions
            WHERE (@from IS NULL OR Trans_Date >= @from) AND (@to IS NULL OR Trans_Date < DATEADD(day, 1, @to)) GROUP BY Trans_Type
            """, connection);
        command.Parameters.AddWithValue("@from", (object?)from ?? DBNull.Value);
        command.Parameters.AddWithValue("@to", (object?)to ?? DBNull.Value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(ct)) totals[reader.GetString(0)] = reader.GetDecimal(1);
        totals.TryGetValue("Rental", out var rental); totals.TryGetValue("FoodOrder", out var food); totals.TryGetValue("Refund", out var refunds); totals.TryGetValue("TopUp", out var topups);
        return new { from = from?.ToString("yyyy-MM-dd"), to = to?.ToString("yyyy-MM-dd"), rentalRevenue = rental, foodRevenue = food, refunds, netRevenue = rental + food - refunds, topups, source = "sql-server" };
    }

    private async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, Func<SqlDataReader, T> map, CancellationToken ct, params (string Name, object Value)[] parameters)
    {
        await using var connection = await connections.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<T>();
        while (await reader.ReadAsync(ct)) rows.Add(map(reader));
        return rows;
    }

    public static int ParseId(string id) => int.Parse(id[(id.LastIndexOf('-') + 1)..]);
    public static string Id(string prefix, int value) => $"{prefix}-{value:000}";
    private static string? NullableId(string prefix, SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Id(prefix, reader.GetInt32(ordinal));
    private static string StringValue(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal);
    private static string DateValue(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? "" : reader.GetDateTime(ordinal).ToString("yyyy-MM-dd");
    private static DateTime? NullableDate(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    private static decimal DecimalValue(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
    private static string? ReadString(JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}

public sealed record ComputerDto(string Id, string Name, string Zone, string Status, bool Online, decimal HourlyRate, string? CustomerId, string? SessionId);
public sealed record CustomerDto(string Id, string Username, string FullName, string Phone, string Email, string DateOfBirth, string Hobbies, string Tier, decimal Balance, string Status);
public sealed record CategoryDto(string Id, string Name);
public sealed record ProductDto(string Id, string Name, string CategoryId, decimal Price, int Stock, bool Active, string? ImageUrl);
public sealed record OrderItemDto(string ProductId, string Name, int Quantity, decimal UnitPrice);
public sealed record OrderDto(string Id, string CustomerId, string? ComputerId, IReadOnlyList<OrderItemDto> Items, decimal Total, string Status, string Note, DateTime CreatedAt);
public sealed record TopupDto(string Id, string CustomerId, decimal Amount, string Status, string Note, DateTime CreatedAt);
public sealed record SessionDto(string Id, string CustomerId, string ComputerId, DateTime StartTime, DateTime? EndTime, decimal StartBalance, decimal Amount, string Status, decimal AppliedHourlyRate);
public sealed record TransactionDto(string Id, string CustomerId, string Type, decimal Amount, string ReferenceId, DateTime CreatedAt);
