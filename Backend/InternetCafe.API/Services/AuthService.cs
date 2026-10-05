using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InternetCafe.API.Data;
using InternetCafe.API.DTOs.Auth;
using InternetCafe.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InternetCafe.API.Services;

public sealed class AuthService(
    InternetCafeDbContext dbContext,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var identifier = request.Identifier.Trim();
        var customer = await dbContext.Customers
            .AsNoTracking()
            .Where(x => x.Username == identifier && x.Status == "Active")
            .Select(x => new { x.CustomerId, x.Username, x.Password_Hash })
            .SingleOrDefaultAsync(cancellationToken);

        var employeeCandidates = await dbContext.Employees
            .AsNoTracking()
            .Where(x => x.Email != null && x.Email == identifier && x.Status == "Active")
            .Select(x => new { x.EmployeeId, x.Email, x.Password_Hash, x.Position_ID })
            .Take(2)
            .ToListAsync(cancellationToken);

        // Email is nullable and not unique in the current schema. Refuse ambiguous identifiers.
        if ((customer is not null && employeeCandidates.Count > 0) || employeeCandidates.Count > 1)
        {
            logger.LogWarning("Ambiguous login identifier encountered.");
            return null;
        }

        if (customer is not null && VerifyPassword(request.Password, customer.Password_Hash))
        {
            return CreateToken(customer.CustomerId, "Customer", null, customer.Username);
        }

        if (employeeCandidates.Count == 1 && VerifyPassword(request.Password, employeeCandidates[0].Password_Hash))
        {
            var employee = employeeCandidates[0];
            var accessLevel = await dbContext.Positions
                .AsNoTracking()
                .Where(x => x.PositionId == employee.Position_ID)
                .Select(x => x.Access_Level)
                .SingleOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(accessLevel))
            {
                return null;
            }

            var role = string.Equals(accessLevel, "Admin", StringComparison.OrdinalIgnoreCase)
                ? "Admin"
                : "Employee";
            return CreateToken(employee.EmployeeId, role, accessLevel, employee.Email!);
        }

        return null;
    }

    private LoginResponse CreateToken(int id, string role, string? accessLevel, string identity)
    {
        var options = jwtOptions.Value;
        var expiresAt = DateTime.UtcNow.AddMinutes(options.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, id.ToString()),
            new(ClaimTypes.Name, identity),
            new(ClaimTypes.Role, role)
        };

        if (!string.IsNullOrWhiteSpace(accessLevel))
        {
            claims.Add(new Claim("access_level", accessLevel));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            expiresAt,
            id,
            role,
            accessLevel);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        if (storedHash.Length < 60 ||
            !(storedHash.StartsWith("$2a$", StringComparison.Ordinal) ||
              storedHash.StartsWith("$2b$", StringComparison.Ordinal) ||
              storedHash.StartsWith("$2y$", StringComparison.Ordinal)))
        {
            return false;
        }

        try
        {
            return global::BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
        catch (global::BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
