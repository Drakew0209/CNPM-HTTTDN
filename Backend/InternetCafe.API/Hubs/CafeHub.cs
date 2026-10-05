using System.Security.Claims;
using InternetCafe.API.Data;
using InternetCafe.API.DTOs.Computers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Hubs;

[Authorize]
public sealed class CafeHub(InternetCafeDbContext dbContext) : Hub
{
    public const string WebAdminGroup = "web-admin";
    public static string CustomerGroup(int customerId) => $"customer:{customerId}";
    public static string ComputerGroup(int computerId) => $"computer:{computerId}";

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole("Customer") == true &&
            int.TryParse(Context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, CustomerGroup(customerId));

            var activeComputerIds = await dbContext.UsageSessions
                .AsNoTracking()
                .Where(x => x.Customer_ID == customerId && x.Status == "Active")
                .Select(x => x.Computer_ID)
                .ToListAsync(Context.ConnectionAborted);

            foreach (var computerId in activeComputerIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, ComputerGroup(computerId));
            }
        }
        else if (Context.User?.IsInRole("Employee") == true || Context.User?.IsInRole("Admin") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, WebAdminGroup);
        }

        await base.OnConnectedAsync();
    }

    [Authorize(Roles = "Customer")]
    public async Task JoinActiveComputer(int computerId)
    {
        if (!int.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
        {
            throw new HubException("Customer identity is missing.");
        }

        var hasActiveSession = await dbContext.UsageSessions.AnyAsync(
            x => x.Customer_ID == customerId && x.Computer_ID == computerId && x.Status == "Active",
            Context.ConnectionAborted);
        if (!hasActiveSession)
        {
            throw new HubException("The customer has no active session on this computer.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ComputerGroup(computerId));
    }

    [Authorize(Roles = "Customer")]
    public async Task ClientCallAdmin(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Trim().Length > 500)
        {
            throw new HubException("Call message must contain between 1 and 500 characters.");
        }
        if (!int.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId))
        {
            throw new HubException("Customer identity is missing.");
        }

        var activeComputers = await (
            from session in dbContext.UsageSessions.AsNoTracking()
            join computer in dbContext.Computers.AsNoTracking()
                on session.Computer_ID equals computer.ComputerId
            where session.Customer_ID == customerId && session.Status == "Active"
            select computer.Computer_Code)
            .Take(2)
            .ToListAsync(Context.ConnectionAborted);

        if (activeComputers.Count != 1)
        {
            throw new HubException("An active session is required to call an employee.");
        }

        await Clients.Group(WebAdminGroup)
            .SendAsync("ReceiveClientCall", activeComputers[0], message.Trim(), Context.ConnectionAborted);
    }

    [Authorize(Roles = "Employee,Admin")]
    public async Task AdminReplyClient(int computerId, string message)
    {
        if (computerId <= 0 || string.IsNullOrWhiteSpace(message) || message.Trim().Length > 500)
        {
            throw new HubException("A valid computer ID and a message of at most 500 characters are required.");
        }

        var hasActiveSession = await dbContext.UsageSessions.AnyAsync(
            x => x.Computer_ID == computerId && x.Status == "Active",
            Context.ConnectionAborted);
        if (!hasActiveSession)
        {
            throw new HubException("There is no active customer session on this computer.");
        }

        await Clients.Group(ComputerGroup(computerId))
            .SendAsync("AdminReplyClient", computerId, message.Trim(), Context.ConnectionAborted);
    }

    [Authorize(Roles = "Employee,Admin")]
    public async Task LockComputer(int computerId)
    {
        await EnsureComputerExists(computerId);
        await Clients.Group(ComputerGroup(computerId)).SendAsync("LockComputer", computerId);
    }

    [Authorize(Roles = "Employee,Admin")]
    public async Task UnlockComputer(int computerId)
    {
        await EnsureComputerExists(computerId);
        await Clients.Group(ComputerGroup(computerId)).SendAsync("UnlockComputer", computerId);
    }

    [Authorize(Roles = "Employee,Admin")]
    public async Task BroadcastComputerStatusChange()
    {
        var computers = await dbContext.Computers
            .AsNoTracking()
            .OrderBy(x => x.Computer_Code)
            .Select(x => new ComputerResponse(
                x.ComputerId,
                x.Computer_Code,
                x.Status ?? "Unknown",
                x.Hourly_Rate,
                null,
                null))
            .ToListAsync(Context.ConnectionAborted);

        await Clients.Group(WebAdminGroup).SendAsync("ComputerStatusChanged", computers, Context.ConnectionAborted);
    }

    private async Task EnsureComputerExists(int computerId)
    {
        if (computerId <= 0 || !await dbContext.Computers.AnyAsync(
                x => x.ComputerId == computerId, Context.ConnectionAborted))
        {
            throw new HubException("Computer was not found.");
        }
    }
}
