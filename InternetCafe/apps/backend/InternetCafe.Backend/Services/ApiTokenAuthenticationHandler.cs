using InternetCafe.Backend.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace InternetCafe.Backend.Services;

public sealed class ApiTokenAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, SqlConnectionFactory connections)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiToken";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        var rawToken = string.Empty;
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            rawToken = authorization[7..].Trim();
        }
        else if (Request.Path.StartsWithSegments("/hubs") && Request.Query.TryGetValue("access_token", out var queryToken))
        {
            rawToken = queryToken.ToString().Trim();
        }
        else
        {
            return AuthenticateResult.NoResult();
        }

        if (rawToken.Length != 64) return AuthenticateResult.Fail("Invalid bearer token.");
        await using var connection = await connections.OpenAsync(Context.RequestAborted);
        await using var command = new SqlCommand("""
            SELECT a.Account_ID, a.Username, a.Role, a.Customer_ID, a.Employee_ID
            FROM dbo.App_Tokens t JOIN dbo.App_Accounts a ON a.Account_ID = t.Account_ID
            WHERE t.Token_Hash = @hash AND t.Revoked_At IS NULL AND t.Expires_At > SYSUTCDATETIME() AND a.Status = 'Active'
            """, connection);
        command.Parameters.AddWithValue("@hash", AuthService.TokenHash(rawToken));
        await using var reader = await command.ExecuteReaderAsync(Context.RequestAborted);
        if (!await reader.ReadAsync(Context.RequestAborted)) return AuthenticateResult.Fail("Expired or revoked bearer token.");
        var accountId = reader.GetInt32(0);
        var username = reader.GetString(1);
        var role = reader.GetString(2);
        int? customerId = reader.IsDBNull(3) ? null : reader.GetInt32(3);
        int? employeeId = reader.IsDBNull(4) ? null : reader.GetInt32(4);
        await reader.CloseAsync();

        // Sliding expiration: keep active session alive by sliding Expires_At
        try
        {
            await using var extendCmd = new SqlCommand("UPDATE dbo.App_Tokens SET Expires_At = DATEADD(hour, 24, SYSUTCDATETIME()) WHERE Token_Hash = @hash AND DATEDIFF(hour, SYSUTCDATETIME(), Expires_At) < 12", connection);
            extendCmd.Parameters.AddWithValue("@hash", AuthService.TokenHash(rawToken));
            await extendCmd.ExecuteNonQueryAsync(Context.RequestAborted);
        }
        catch { }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, accountId.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role)
        };
        if (customerId.HasValue) claims.Add(new Claim("customer_id", customerId.Value.ToString()));
        if (employeeId.HasValue) claims.Add(new Claim("employee_id", employeeId.Value.ToString()));
        var identity = new ClaimsIdentity(claims, SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
