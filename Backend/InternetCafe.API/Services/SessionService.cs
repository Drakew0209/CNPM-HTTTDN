using System.Data;
using InternetCafe.API.Data;
using InternetCafe.API.Data.Entities;
using InternetCafe.API.DTOs.Computers;
using InternetCafe.API.DTOs.Sessions;
using InternetCafe.API.ExceptionHandling;
using InternetCafe.API.Hubs;
using InternetCafe.API.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Services;

public sealed class SessionService(
    InternetCafeDbContext dbContext,
    IHubContext<CafeHub> hubContext,
    ILogger<SessionService> logger) : ISessionService
{
    public async Task<StartSessionResponse> StartSessionAsync(
        int customerId,
        int computerId,
        int? employeeId,
        CancellationToken cancellationToken)
    {
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);
        if (customer is null)
        {
            throw new KeyNotFoundException("Customer was not found.");
        }
        if (customer.Status != "Active")
        {
            throw new ConflictException("Only active customers can start a session.");
        }

        var balance = customer.Balance ?? 0m;
        if (balance <= 0m)
        {
            throw new ConflictException("The customer must have a positive balance to start a session.");
        }

        var computer = await dbContext.Computers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ComputerId == computerId, cancellationToken);
        if (computer is null)
        {
            throw new KeyNotFoundException("Computer was not found.");
        }
        if (computer.Status != "Available")
        {
            throw new ConflictException("The computer is not available.");
        }

        var computerAlreadyInUse = await dbContext.UsageSessions.AnyAsync(
            x => x.Computer_ID == computerId && x.Status == "Active", cancellationToken);
        if (computerAlreadyInUse)
        {
            throw new ConflictException("The computer already has an active session.");
        }

        var customerAlreadyPlaying = await dbContext.UsageSessions.AnyAsync(
            x => x.Customer_ID == customerId && x.Status == "Active", cancellationToken);
        if (customerAlreadyPlaying)
        {
            throw new ConflictException("The customer already has an active session.");
        }

        var startTime = DateTime.UtcNow;
        var session = new UsageSession
        {
            Customer_ID = customerId,
            Computer_ID = computerId,
            Employee_ID = employeeId,
            Start_Time = startTime,
            Start_Balance = balance,
            Status = "Active"
            // SQL triggers set Computer.Status and Applied_Hourly_Rate.
        };

        dbContext.UsageSessions.Add(session);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new ConflictException("The computer or customer already has an active session.");
        }

        var appliedRate = await dbContext.UsageSessions
            .AsNoTracking()
            .Where(x => x.SessionId == session.SessionId)
            .Select(x => x.Applied_Hourly_Rate)
            .SingleAsync(cancellationToken);
        if (appliedRate is null)
        {
            throw new ConflictException("The database did not capture the hourly rate for this session.");
        }

        await dbTransaction.CommitAsync(cancellationToken);
        await NotifyClientUnlockedAsync(customerId, computerId, session.SessionId, cancellationToken);
        await NotifyComputerStatusChangedAsync(cancellationToken);

        return new StartSessionResponse(
            session.SessionId,
            customerId,
            computerId,
            startTime,
            appliedRate.Value,
            balance);
    }

    public async Task<EndSessionResponse> EndSessionAsync(
        int sessionId,
        int callerId,
        bool isEmployee,
        int? employeeId,
        CancellationToken cancellationToken)
    {
        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var session = await dbContext.UsageSessions
            .SingleOrDefaultAsync(x => x.SessionId == sessionId && x.Status == "Active", cancellationToken);
        if (session is null)
        {
            throw new KeyNotFoundException("Active session was not found.");
        }
        if (!isEmployee && session.Customer_ID != callerId)
        {
            throw new UnauthorizedAccessException("Customers can only end their own sessions.");
        }

        var customer = await dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.CustomerId == session.Customer_ID, cancellationToken);
        if (customer is null)
        {
            throw new KeyNotFoundException("Session customer was not found.");
        }

        var hourlyRate = session.Applied_Hourly_Rate;
        if (hourlyRate is null)
        {
            throw new ConflictException("The active session has no captured hourly rate and cannot be settled safely.");
        }

        var endTime = DateTime.UtcNow;
        var elapsedTicks = (endTime - session.Start_Time).Ticks;
        if (elapsedTicks < 0)
        {
            throw new ConflictException("Session start time is later than the current time.");
        }
        var elapsedSeconds = (decimal)elapsedTicks / TimeSpan.TicksPerSecond;

        // Bill by elapsed second, rounded to the nearest cent (midpoints away from zero).
        var calculatedAmount = decimal.Round(
            hourlyRate.Value * elapsedTicks / TimeSpan.TicksPerHour,
            2,
            MidpointRounding.AwayFromZero);
        if (calculatedAmount < 0m)
        {
            throw new ConflictException("The calculated session charge cannot be negative.");
        }

        var currentBalance = customer.Balance ?? 0m;
        var chargedAmount = Math.Min(calculatedAmount, currentBalance);
        var waivedAmount = calculatedAmount - chargedAmount;
        if (waivedAmount > 0m)
        {
            logger.LogWarning(
                "Session {SessionId} charge was capped: calculated {CalculatedAmount}, charged {ChargedAmount}, waived {WaivedAmount} due to insufficient balance.",
                session.SessionId,
                calculatedAmount,
                chargedAmount,
                waivedAmount);
        }

        if (chargedAmount > 9_999_999_999.99m)
        {
            throw new ConflictException("The session charge exceeds the database amount limit.");
        }

        session.End_Time = endTime;
        session.Status = "Completed";
        // Amount stores the amount actually charged; any capped difference is returned
        // and logged, since this schema has no separate debt/waived-amount column.
        session.Amount = chargedAmount;

        if (chargedAmount > 0m)
        {
            dbContext.Transactions.Add(new FinancialTransaction
            {
                Customer_ID = session.Customer_ID,
                Processed_By = employeeId,
                Session_ID = session.SessionId,
                Trans_Type = "Rental",
                Amount = chargedAmount
            });
        }

        // Save session and rental transaction atomically. SQL triggers synchronize the
        // computer state and deduct the customer balance within this same SQL transaction.
        await dbContext.SaveChangesAsync(cancellationToken);

        var balanceAfter = await dbContext.Customers
            .AsNoTracking()
            .Where(x => x.CustomerId == session.Customer_ID)
            .Select(x => x.Balance)
            .SingleAsync(cancellationToken) ?? 0m;

        await dbTransaction.CommitAsync(cancellationToken);

        await NotifyClientLockedAsync(session.Customer_ID, session.Computer_ID, session.SessionId, balanceAfter, cancellationToken);
        await NotifyComputerStatusChangedAsync(cancellationToken);

        var totalHours = decimal.Round(elapsedSeconds / 3600m, 4, MidpointRounding.AwayFromZero);
        return new EndSessionResponse(
            session.SessionId,
            session.Customer_ID,
            session.Computer_ID,
            session.Start_Time,
            endTime,
            totalHours,
            hourlyRate.Value,
            calculatedAmount,
            chargedAmount,
            waivedAmount,
            balanceAfter);
    }

    private async Task NotifyClientLockedAsync(
        int customerId,
        int computerId,
        int sessionId,
        decimal balanceAfter,
        CancellationToken cancellationToken)
    {
        var message = new { Computer_ID = computerId, Session_ID = sessionId };
        try
        {
            await hubContext.Clients.Group(CafeHub.ComputerGroup(computerId))
                .SendAsync("ForceLockClient", message, cancellationToken);
            await hubContext.Clients.Group(CafeHub.CustomerGroup(customerId))
                .SendAsync("BalanceUpdated", new { Balance = balanceAfter, Session_ID = sessionId }, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not send ForceLockClient for session {SessionId}.", sessionId);
        }
    }

    private async Task NotifyClientUnlockedAsync(
        int customerId,
        int computerId,
        int sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            // The customer's desktop is already authenticated in its own group; it can then
            // subscribe to the active computer group through CafeHub.JoinActiveComputer.
            await hubContext.Clients.Group(CafeHub.CustomerGroup(customerId))
                .SendAsync("UnlockComputer", new { Computer_ID = computerId, Session_ID = sessionId }, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not send UnlockComputer for session {SessionId}.", sessionId);
        }
    }

    private async Task NotifyComputerStatusChangedAsync(CancellationToken cancellationToken)
    {
        try
        {
            var computers = await dbContext.Computers
                .AsNoTracking()
                .OrderBy(x => x.Computer_Code)
                .Select(x => new ComputerResponse(
                    x.ComputerId,
                    x.Computer_Code,
                    x.Status ?? "Unknown",
                    x.Hourly_Rate))
                .ToListAsync(cancellationToken);

            await hubContext.Clients.Group(CafeHub.WebAdminGroup)
                .SendAsync("ComputerStatusChanged", computers, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not broadcast the updated computer statuses.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 2601 or 2627 };
}
