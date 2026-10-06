using System.Data;
using InternetCafe.API.Data;
using InternetCafe.API.Data.Entities;
using InternetCafe.API.DTOs.Inventory;
using InternetCafe.API.ExceptionHandling;
using InternetCafe.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Services;

public sealed class InventoryService(InternetCafeDbContext dbContext) : IInventoryService
{
    public async Task<RestockResponse> RestockAsync(
        RestockRequest request,
        int employeeId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var product = await dbContext.Products
            .FromSqlInterpolated($"SELECT * FROM dbo.Products WITH (UPDLOCK, HOLDLOCK) WHERE Product_ID = {request.Product_ID}")
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null)
        {
            throw new KeyNotFoundException("Product was not found.");
        }
        if (product.Status != "Active")
        {
            throw new ConflictException("Inactive products cannot be restocked.");
        }

        if ((long)product.Stock_Quantity + request.Quantity > int.MaxValue)
        {
            throw new ArgumentException("The resulting stock quantity exceeds the supported limit.");
        }

        var inventoryTransaction = new InventoryTransaction
        {
            Product_ID = product.ProductId,
            Employee_ID = employeeId,
            // SQL trigger expects Import/Export. 'In' is not a valid value in this schema.
            Trans_Type = "Import",
            Quantity = request.Quantity,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            Created_Date = DateTime.UtcNow
        };

        dbContext.InventoryTransactions.Add(inventoryTransaction);
        // TR_Sync_Inventory applies the quantity increase within this DB transaction.
        await dbContext.SaveChangesAsync(cancellationToken);

        var stockAfter = await dbContext.Products
            .AsNoTracking()
            .Where(x => x.ProductId == product.ProductId)
            .Select(x => x.Stock_Quantity)
            .SingleAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new RestockResponse(
            inventoryTransaction.InventoryTransactionId,
            product.ProductId,
            product.Product_Name,
            request.Quantity,
            stockAfter,
            inventoryTransaction.Created_Date);
    }
}
