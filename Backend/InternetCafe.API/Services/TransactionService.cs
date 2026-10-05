using InternetCafe.API.Data;
using InternetCafe.API.Data.Entities;
using InternetCafe.API.DTOs.Transactions;
using InternetCafe.API.ExceptionHandling;
using InternetCafe.API.Hubs;
using InternetCafe.API.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Services;

public sealed class TransactionService(
    InternetCafeDbContext dbContext,
    IHubContext<CafeHub> hubContext) : ITransactionService
{
    public async Task<TopUpResponse> TopUpAsync(
        TopUpRequest request,
        int? processedBy,
        CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(x => x.CustomerId == request.Customer_ID, cancellationToken);

        if (customer is null)
        {
            throw new KeyNotFoundException("Customer was not found.");
        }
        if (customer.Status != "Active")
        {
            throw new ConflictException("Only active customers can receive a top-up.");
        }

        decimal bonusAmount = 0m;
        if (request.Combo_ID is int comboId)
        {
            var combo = await dbContext.Combos
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.ComboId == comboId && x.Is_Active == true, cancellationToken);

            if (combo is null)
            {
                throw new KeyNotFoundException("Active combo was not found.");
            }

            if (combo.Price != request.Amount)
            {
                throw new ArgumentException("Amount must match the selected combo price.");
            }

            bonusAmount = combo.Bonus_Balance;
        }

        var transaction = new FinancialTransaction
        {
            Customer_ID = request.Customer_ID,
            Processed_By = processedBy,
            Combo_ID = request.Combo_ID,
            Trans_Type = "TopUp",
            Amount = request.Amount
        };

        dbContext.Transactions.Add(transaction);
        // The SQL AFTER INSERT triggers update the customer balance and create the receipt.
        await dbContext.SaveChangesAsync(cancellationToken);

        var updatedBalance = await dbContext.Customers
            .AsNoTracking()
            .Where(x => x.CustomerId == request.Customer_ID)
            .Select(x => x.Balance)
            .SingleAsync(cancellationToken);

        var receipt = await dbContext.TopUpReceipts
            .AsNoTracking()
            .Where(x => x.Transaction_ID == transaction.TransactionId)
            .Select(x => new { x.Receipt_Code, x.Bonus_Amount })
            .SingleOrDefaultAsync(cancellationToken);

        var response = new TopUpResponse(
            transaction.TransactionId,
            request.Customer_ID,
            request.Amount,
            receipt?.Bonus_Amount ?? bonusAmount,
            updatedBalance ?? 0m,
            receipt?.Receipt_Code,
            transaction.Trans_Date);

        await hubContext.Clients.Group(CafeHub.CustomerGroup(request.Customer_ID))
            .SendAsync("BalanceUpdated", new { response.Balance, response.Transaction_ID }, cancellationToken);

        return response;
    }
}
