using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;

namespace InternetCafe.Backend;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Owner = "Owner";
    public const string Manager = "Manager";
    public const string Cashier = "Cashier";
    public const string Staff = "Staff";
    public const string Customer = "Customer";
}

public sealed record LoginRequest(string Username, string Password);
public sealed record RegisterRequest(string Username, string Password, string FullName, string Phone, string? Email, string? DateOfBirth, string? Hobbies);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record StartSessionRequest(string ComputerId);
public sealed record CreateOrderRequest(string ComputerId, IReadOnlyList<OrderLineRequest> Items, string? Note, string? IdempotencyKey = null);
public sealed record OrderLineRequest(string ProductId, int Quantity, decimal? ExpectedUnitPrice = null);
public sealed record CreateTopupRequest(decimal Amount, string? Note, string? IdempotencyKey = null);
public sealed record TransitionRequest(string Status);
public sealed record DecisionRequest(string Status);
public sealed record ComputerCommandRequest(string Command);

public sealed record ApiError(string Code, string Message, int Status, IReadOnlyDictionary<string, string>? Fields = null);
public sealed record OperationResult<T>(T? Value, ApiError? Error)
{
    public static OperationResult<T> Ok(T value) => new(value, null);
    public static OperationResult<T> Fail(string code, string message, int status = StatusCodes.Status422UnprocessableEntity, IReadOnlyDictionary<string, string>? fields = null) => new(default, new(code, message, status, fields));
    public TResult Match<TResult>(Func<T, TResult> success, Func<ApiError, TResult> failure) => Error is null ? success(Value!) : failure(Error);
    public IResult ToResult() => Error is null ? Results.Ok(Api.Data(Value)) : Api.Error(Error.Code, Error.Message, Error.Status, Error.Fields);
}

public static class Api
{
    public static object Data<T>(T value) => new { data = value };
    public static object List<T>(IReadOnlyList<T> rows) => new { data = rows, meta = new { total = rows.Count, page = 1, pageSize = rows.Count } };
    public static object List<T>(IReadOnlyList<T> rows, HttpRequest request)
    {
        var properties = typeof(T).GetProperties();
        var search = request.Query["search"].ToString().Trim();
        var status = request.Query["status"].ToString().Trim();
        var sort = request.Query["sort"].ToString().Trim();
        var order = request.Query["order"].ToString().Trim();
        var from = DateOnly.TryParse(request.Query["from"].ToString(), out var fromDate) ? fromDate : (DateOnly?)null;
        var to = DateOnly.TryParse(request.Query["to"].ToString(), out var toDate) ? toDate : (DateOnly?)null;

        IEnumerable<T> filtered = rows;
        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(row => properties.Any(property =>
                Convert.ToString(property.GetValue(row))?.Contains(search, StringComparison.OrdinalIgnoreCase) == true));
        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusProperty = properties.FirstOrDefault(property => string.Equals(property.Name, "Status", StringComparison.OrdinalIgnoreCase));
            if (statusProperty is not null)
                filtered = filtered.Where(row => string.Equals(Convert.ToString(statusProperty.GetValue(row)), status, StringComparison.OrdinalIgnoreCase));
        }
        if (from is not null || to is not null)
        {
            var dateProperty = properties.FirstOrDefault(property => property.Name is "CreatedAt" or "StartTime" or "Date" or "RequestedAt");
            if (dateProperty is not null)
                filtered = filtered.Where(row =>
                {
                    var value = dateProperty.GetValue(row);
                    var date = value is DateTime timestamp ? DateOnly.FromDateTime(timestamp) : value is DateOnly day ? day : (DateOnly?)null;
                    return date is not null && (from is null || date >= from) && (to is null || date <= to);
                });
        }

        var sortProperty = properties.FirstOrDefault(property => string.Equals(property.Name, sort, StringComparison.OrdinalIgnoreCase));
        if (sortProperty is not null)
            filtered = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase)
                ? filtered.OrderByDescending(row => sortProperty.GetValue(row) as IComparable)
                : filtered.OrderBy(row => sortProperty.GetValue(row) as IComparable);

        var total = filtered.Count();
        var page = int.TryParse(request.Query["page"], out var requestedPage) ? Math.Max(1, requestedPage) : 1;
        var pageSize = int.TryParse(request.Query["pageSize"], out var requestedPageSize) ? Math.Clamp(requestedPageSize, 1, 100) : 20;
        return new { data = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList(), meta = new { total, page, pageSize } };
    }
    public static IResult Error(string code, string message, int status, IReadOnlyDictionary<string, string>? fields = null) => Results.Json(new { error = new { code, message, fields } }, statusCode: status);
}

public sealed record IdentityContext(int AccountId, string Role, int? CustomerId, int? EmployeeId, string Username)
{
    public bool IsManagement => Role is Roles.Owner or Roles.Manager;
    public bool IsOperator => Role is Roles.Owner or Roles.Manager or Roles.Cashier;
}

public static class ClaimsExtensions
{
    public static int AccountId(this ClaimsPrincipal principal) => int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Missing account claim."));
    public static IdentityContext IdentityContext(this ClaimsPrincipal principal) => new(
        principal.AccountId(),
        principal.FindFirstValue(ClaimTypes.Role) ?? throw new InvalidOperationException("Missing role claim."),
        int.TryParse(principal.FindFirstValue("customer_id"), out var customerId) ? customerId : null,
        int.TryParse(principal.FindFirstValue("employee_id"), out var employeeId) ? employeeId : null,
        principal.Identity?.Name ?? "");
}
