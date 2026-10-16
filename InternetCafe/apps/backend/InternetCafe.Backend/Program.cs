using InternetCafe.Backend;
using InternetCafe.Backend.Data;
using InternetCafe.Backend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options => options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddSingleton<DatabaseBootstrapper>();
builder.Services.AddScoped<CafeRepository>();
builder.Services.AddScoped<ManagementRepository>();
builder.Services.AddScoped<ManagementService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BusinessService>();
builder.Services.AddHostedService<SessionMonitor>();
builder.Services.AddSignalR();
builder.Services.AddAuthentication(ApiTokenAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiTokenAuthenticationHandler>(ApiTokenAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});
builder.Services.AddCors(options => options.AddPolicy("local", policy => policy
    .WithOrigins("http://127.0.0.1:5173", "http://localhost:5173", "http://127.0.0.1:4173", "http://localhost:4173")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<DatabaseBootstrapper>().ApplyAsync();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = new
            {
                code = "INTERNAL_SERVER_ERROR",
                message = "Đã xảy ra lỗi trên hệ thống máy chủ. Vui lòng thử lại sau.",
                status = StatusCodes.Status500InternalServerError
            }
        });
    });
});

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseCors("local");
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (SqlConnectionFactory connections, CancellationToken ct) =>
{
    await using var connection = await connections.OpenAsync(ct);
    await using var command = new SqlCommand("SELECT COUNT(*) FROM sys.tables", connection);
    var tables = Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    return Results.Ok(Api.Data(new { status = "ok", source = "sql-server", persistent = true, tables }));
});

var api = app.MapGroup("/api/v1");
api.MapPost("/auth/login", async (LoginRequest request, AuthService auth, CancellationToken ct) =>
{
    var result = await auth.LoginAsync(request, ct);
    return result is null
        ? Api.Error("INVALID_CREDENTIALS", "Tên đăng nhập hoặc mật khẩu không đúng.", StatusCodes.Status401Unauthorized)
        : Results.Ok(Api.Data(result));
}).AllowAnonymous().RequireRateLimiting("auth");

api.MapPost("/auth/logout", [Authorize] async (HttpContext context, AuthService auth, CancellationToken ct) =>
{
    await auth.LogoutAsync(context.User.AccountId(), ct);
    return Results.Ok(Api.Data(new { loggedOut = true }));
});

api.MapPost("/auth/register", async (RegisterRequest request, AuthService auth, CancellationToken ct) =>
{
    var outcome = await auth.RegisterAsync(request, ct);
    return outcome.Match(created => Results.Created("/api/v1/me", Api.Data(created)), error => Api.Error(error.Code, error.Message, error.Status, error.Fields));
}).AllowAnonymous().RequireRateLimiting("auth");

api.MapGet("/workspace", [Authorize] async (HttpContext context, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.Data(await repository.WorkspaceAsync(context.User.IdentityContext(), ct))));
api.MapGet("/me", [Authorize] async (HttpContext context, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.Data(await repository.MeAsync(context.User.IdentityContext(), ct))));
api.MapPatch("/me", [Authorize] async (HttpContext context, JsonElement body, CafeRepository repository, CancellationToken ct) =>
    (await repository.UpdateMeAsync(context.User.IdentityContext(), body, ct)).ToResult());
api.MapPost("/me/password", [Authorize] async (HttpContext context, ChangePasswordRequest request, AuthService auth, CancellationToken ct) =>
    (await auth.ChangePasswordAsync(context.User.AccountId(), request, ct)).ToResult());

api.MapGet("/computers", [Authorize] async (HttpContext context, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.ComputersAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/products", [Authorize] async (HttpContext context, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.ProductsAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/categories", [Authorize] async (HttpContext context, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.CategoriesAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/orders", [Authorize] async (HttpContext context, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.OrdersAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/topups", [Authorize] async (HttpContext context, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.TopupsAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/sessions", [Authorize] async (HttpContext context, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.SessionsAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/customers", [Authorize] async (HttpContext context, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.CustomersAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/transactions", [Authorize] async (HttpContext context, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.TransactionsAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/suppliers", [Authorize] async (HttpContext context, ManagementRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.SuppliersAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/inventory", [Authorize] async (HttpContext context, ManagementRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.InventoryAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/feedback", [Authorize] async (HttpContext context, ManagementRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.FeedbackAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/surveys", [Authorize] async (HttpContext context, ManagementRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.SurveysAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/employees", [Authorize] async (HttpContext context, ManagementRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.EmployeesAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/shifts", [Authorize] async (HttpContext context, ManagementRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.ShiftsAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/schedules", [Authorize] async (HttpContext context, ManagementRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.SchedulesAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/leaves", [Authorize] async (HttpContext context, ManagementRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.LeavesAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/attendance", [Authorize] async (HttpContext context, ManagementRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.AttendanceAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/payroll", [Authorize] async (HttpContext context, ManagementRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.PayrollAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/materials", [Authorize] async (HttpContext context, ManagementRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.MaterialsAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/bom", [Authorize] async (HttpContext context, ManagementRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.BomAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/material-requests", [Authorize] async (HttpContext context, ManagementRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.MaterialRequestsAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/accounts", [Authorize(Roles = Roles.Admin)] async (HttpContext context, ManagementRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.AccountsAsync(context.User.IdentityContext(), ct), context.Request)));
api.MapGet("/combos", [Authorize] async (HttpContext context, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.CombosAsync(ct), context.Request)));
api.MapGet("/games", [Authorize] async (HttpContext context, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.GamesAsync(ct), context.Request)));
api.MapGet("/invoices", [Authorize(Roles = Roles.Owner + "," + Roles.Manager + "," + Roles.Cashier)] async (HttpContext context, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.List(await repository.InvoicesAsync(ct), context.Request)));
api.MapGet("/invoices/{id}", [Authorize(Roles = Roles.Owner + "," + Roles.Manager + "," + Roles.Cashier)] async (string id, CafeRepository repository, CancellationToken ct) =>
{
    var invoice = await repository.InvoiceAsync(id, ct);
    return invoice is null ? Api.Error("NOT_FOUND", "Không tìm thấy hóa đơn.", StatusCodes.Status404NotFound) : Results.Ok(Api.Data(invoice));
});

api.MapPost("/sessions/start", [Authorize(Roles = Roles.Customer)] async (HttpContext context, StartSessionRequest request, BusinessService service, CancellationToken ct) =>
    (await service.StartSessionAsync(context.User.IdentityContext(), request, ct)).ToResult());
api.MapPost("/sessions/{id}/end", [Authorize] async (HttpContext context, string id, BusinessService service, CancellationToken ct) =>
    (await service.EndSessionAsync(context.User.IdentityContext(), id, ct)).ToResult());
api.MapPost("/sessions/{id}/transfer", [Authorize] async (HttpContext context, string id, TransferSessionRequest request, BusinessService service, CancellationToken ct) =>
    (await service.TransferSessionAsync(context.User.IdentityContext(), id, request, ct)).ToResult());
api.MapPost("/orders", [Authorize(Roles = Roles.Customer)] async (HttpContext context, CreateOrderRequest request, BusinessService service, CancellationToken ct) =>
    (await service.CreateOrderAsync(context.User.IdentityContext(), request, ct)).ToResult());
api.MapPost("/orders/{id}/transition", [Authorize] async (HttpContext context, string id, TransitionRequest request, BusinessService service, CancellationToken ct) =>
    (await service.TransitionOrderAsync(context.User.IdentityContext(), id, request, ct)).ToResult());
api.MapPost("/topups", [Authorize(Roles = Roles.Customer)] async (HttpContext context, CreateTopupRequest request, BusinessService service, CancellationToken ct) =>
    (await service.CreateTopupAsync(context.User.IdentityContext(), request, ct)).ToResult());
api.MapPost("/topups/{id}/decision", [Authorize] async (HttpContext context, string id, DecisionRequest request, BusinessService service, CancellationToken ct) =>
    (await service.DecideTopupAsync(context.User.IdentityContext(), id, request, ct)).ToResult());
api.MapPost("/customers/{id}/topup", [Authorize(Roles = Roles.Owner + "," + Roles.Manager + "," + Roles.Cashier)] async (HttpContext context, string id, AdminTopupRequest request, BusinessService service, CancellationToken ct) =>
    (await service.AdminTopupAsync(context.User.IdentityContext(), id, request, ct)).ToResult());
api.MapPost("/customers/{id}/refund", [Authorize(Roles = Roles.Owner + "," + Roles.Manager + "," + Roles.Cashier)] async (HttpContext context, string id, RefundRequest request, BusinessService service, CancellationToken ct) =>
    (await service.RefundCustomerBalanceAsync(context.User.IdentityContext(), id, request, ct)).ToResult());
api.MapPost("/computers/{id}/command", [Authorize] async (HttpContext context, string id, ComputerCommandRequest request, BusinessService service, CancellationToken ct) =>
    (await service.CommandComputerAsync(context.User.IdentityContext(), id, request, ct)).ToResult());
api.MapPost("/machines/{id}/heartbeat", [Authorize] async (HttpContext context, string id, BusinessService service, CancellationToken ct) =>
    (await service.HeartbeatAsync(context.User.IdentityContext(), id, ct)).ToResult());
api.MapGet("/reports/summary", [Authorize(Roles = Roles.Owner + "," + Roles.Manager + "," + Roles.Cashier)] async (DateOnly? from, DateOnly? to, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.Data(await repository.ReportAsync(from, to, ct))));
api.MapGet("/reports/analytics", [Authorize(Roles = Roles.Owner + "," + Roles.Manager)] async (DateOnly? from, DateOnly? to, CafeRepository repository, CancellationToken ct) =>
    Results.Ok(Api.Data(await repository.AnalyticsReportAsync(from, to, ct))));

// The workspace is the authorization-aware read model. Detail routes select from
// that model so a caller can never bypass row-level filtering by guessing an ID.
api.MapGet("/{resource}/{id}", [Authorize] async (HttpContext context, string resource, string id, CafeRepository repository, CancellationToken ct) =>
{
    var workspace = await repository.WorkspaceAsync(context.User.IdentityContext(), ct);
    var collection = workspace.GetType().GetProperties()
        .FirstOrDefault(property => string.Equals(property.Name, resource, StringComparison.OrdinalIgnoreCase))
        ?.GetValue(workspace) as System.Collections.IEnumerable;
    if (collection is null)
        return Api.Error("NOT_FOUND", "Không tìm thấy loại dữ liệu.", StatusCodes.Status404NotFound);

    foreach (var row in collection)
    {
        var rowId = row?.GetType().GetProperty("Id")?.GetValue(row) as string;
        if (string.Equals(rowId, id, StringComparison.OrdinalIgnoreCase))
            return Results.Ok(Api.Data(row));
    }
    return Api.Error("NOT_FOUND", "Không tìm thấy dữ liệu hoặc bạn không có quyền xem.", StatusCodes.Status404NotFound);
});

api.MapPost("/{resource}", [Authorize] async (HttpContext context, string resource, JsonElement body, ManagementService service, CancellationToken ct) =>
    (await service.CreateAsync(context.User.IdentityContext(), resource, body, ct)).ToResult());
api.MapPatch("/{resource}/{id}", [Authorize] async (HttpContext context, string resource, string id, JsonElement body, ManagementService service, CancellationToken ct) =>
    (await service.UpdateAsync(context.User.IdentityContext(), resource, id, body, ct)).ToResult());
api.MapDelete("/{resource}/{id}", [Authorize] async (HttpContext context, string resource, string id, ManagementService service, CancellationToken ct) =>
    (await service.DeleteAsync(context.User.IdentityContext(), resource, id, ct)).ToResult());
api.MapPost("/leaves/{id}/decision", [Authorize] async (HttpContext context, string id, DecisionRequest request, ManagementService service, CancellationToken ct) =>
    (await service.DecideLeaveAsync(context.User.IdentityContext(), id, request, ct)).ToResult());
api.MapPost("/material-requests/{id}/decision", [Authorize] async (HttpContext context, string id, DecisionRequest request, ManagementService service, CancellationToken ct) =>
    (await service.DecideMaterialRequestAsync(context.User.IdentityContext(), id, request, ct)).ToResult());
api.MapPost("/attendance/check-in", [Authorize] async (HttpContext context, JsonElement body, ManagementService service, CancellationToken ct) =>
    (await service.CheckInAsync(context.User.IdentityContext(), body, ct)).ToResult());
api.MapPost("/attendance/{id}/check-out", [Authorize] async (HttpContext context, string id, JsonElement body, ManagementService service, CancellationToken ct) =>
    (await service.CheckOutAsync(context.User.IdentityContext(), id, body, ct)).ToResult());
api.MapPost("/attendance/{id}/admin-checkout", [Authorize(Roles = Roles.Owner + "," + Roles.Manager)] async (HttpContext context, string id, JsonElement body, ManagementService service, CancellationToken ct) =>
    (await service.AdminCheckOutAsync(context.User.IdentityContext(), id, body, ct)).ToResult());
api.MapPost("/payroll/calculate-month", [Authorize(Roles = Roles.Owner + "," + Roles.Manager)] async (HttpContext context, JsonElement body, ManagementService service, CancellationToken ct) =>
    (await service.CalculateMonthlyPayrollAsync(context.User.IdentityContext(), body, ct)).ToResult());
api.MapPost("/surveys/{id}/publish", [Authorize] async (HttpContext context, string id, JsonElement body, ManagementService service, CancellationToken ct) =>
    (await service.PublishSurveyAsync(context.User.IdentityContext(), id, body, ct)).ToResult());
api.MapPost("/surveys/{id}/responses", [Authorize] async (HttpContext context, string id, JsonElement body, ManagementService service, CancellationToken ct) =>
    (await service.SubmitSurveyAsync(context.User.IdentityContext(), id, body, ct)).ToResult());

app.MapHub<CafeHub>("/hubs/internetcafe").RequireAuthorization();
app.Run();

public partial class Program;
