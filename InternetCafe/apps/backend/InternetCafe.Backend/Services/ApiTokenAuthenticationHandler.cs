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
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();
        var rawToken = authorization[7..].Trim();
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
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, reader.GetInt32(0).ToString()),
            new(ClaimTypes.Name, reader.GetString(1)),
            new(ClaimTypes.Role, reader.GetString(2))
        };
        if (!reader.IsDBNull(3)) claims.Add(new Claim("customer_id", reader.GetInt32(3).ToString()));
        if (!reader.IsDBNull(4)) claims.Add(new Claim("employee_id", reader.GetInt32(4).ToString()));
        var identity = new ClaimsIdentity(claims, SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
