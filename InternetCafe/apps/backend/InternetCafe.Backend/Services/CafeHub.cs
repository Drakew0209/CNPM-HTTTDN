using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace InternetCafe.Backend.Services;

[Authorize]
public sealed class CafeHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var identity = Context.User?.IdentityContext() ?? throw new HubException("Unauthenticated connection.");
        if (identity.IsOperator) await Groups.AddToGroupAsync(Context.ConnectionId, "operations");
        if (identity.IsManagement) await Groups.AddToGroupAsync(Context.ConnectionId, "management");
        if (identity.Role == Roles.Admin) await Groups.AddToGroupAsync(Context.ConnectionId, "system");
        if (identity.CustomerId is { } customerId) await Groups.AddToGroupAsync(Context.ConnectionId, $"customer:{customerId}");
        if (identity.EmployeeId is { } employeeId) await Groups.AddToGroupAsync(Context.ConnectionId, $"employee:{employeeId}");
        await base.OnConnectedAsync();
    }
}
