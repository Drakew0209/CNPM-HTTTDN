using System.Data;
using InternetCafe.API.Data;
using InternetCafe.API.Data.Entities;
using InternetCafe.API.DTOs.Orders;
using InternetCafe.API.ExceptionHandling;
using InternetCafe.API.Hubs;
using InternetCafe.API.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Services;

public sealed class OrderService(
    InternetCafeDbContext dbContext,
    IHubContext<CafeHub> hubContext,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<OrderResponse> CreateAsync(OrderRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count is < 1 or > 100)
        {
            throw new ArgumentException("An order must contain between 1 and 100 item rows.");
        }

        var quantities = new Dictionary<int, int>();
        foreach (var item in request.Items)
        {
            if (item.Product_ID <= 0 || item.Quantity is < 1 or > 1000)
            {
                throw new ArgumentException("Each product ID must be positive and quantity must be between 1 and 1000.");
            }

            try
            {
                quantities[item.Product_ID] = checked(quantities.GetValueOrDefault(item.Product_ID) + item.Quantity);
            }
            catch (OverflowException)
            {
                throw new ArgumentException("The combined quantity for a product is too large.");
            }
        }

        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var customer = await dbContext.Customers
            .FromSqlInterpolated($"SELECT * FROM dbo.Customers WITH (UPDLOCK, HOLDLOCK) WHERE Customer_ID = {request.Customer_ID}")
            .SingleOrDefaultAsync(cancellationToken);
        if (customer is null)
        {
            throw new KeyNotFoundException("Customer was not found.");
        }
        if (customer.Status != "Active")
        {
            throw new ConflictException("Only active customers can place an order.");
        }

        var activeSessions = await dbContext.UsageSessions
            .AsNoTracking()
            .Where(x => x.Customer_ID == request.Customer_ID && x.Status == "Active")
            .Select(x => new { x.SessionId, x.Computer_ID })
            .Take(2)
            .ToListAsync(cancellationToken);
        if (activeSessions.Count != 1)
        {
            throw new ConflictException("The customer must have exactly one active computer session to order from a workstation.");
        }

        // Lock products in a stable order until the transaction completes. Stock is not
        // updated here: TR_Sync_Order_Details_Inventory decrements it when details insert.
        var products = new Dictionary<int, Product>();
        foreach (var productId in quantities.Keys.Order())
        {
            var product = await dbContext.Products
                .FromSqlInterpolated($"SELECT * FROM dbo.Products WITH (UPDLOCK, HOLDLOCK) WHERE Product_ID = {productId}")
                .SingleOrDefaultAsync(cancellationToken);

            if (product is null || product.Status != "Active")
            {
                throw new ArgumentException($"Product {productId} is not available.");
            }
            if (product.Stock_Quantity < quantities[productId])
            {
                throw new ArgumentException($"Insufficient stock for product {productId}.");
            }

            products.Add(productId, product);
        }

        decimal totalAmount = 0m;
        foreach (var (productId, quantity) in quantities)
        {
            totalAmount += products[productId].Price * quantity;
        }
        if (totalAmount <= 0m || totalAmount > 9_999_999_999.99m)
        {
            throw new ArgumentException("The order total is outside the supported amount range.");
        }

        var currentBalance = customer.Balance ?? 0m;
        if (currentBalance < totalAmount)
        {
            throw new ArgumentException("The customer balance is insufficient for this order.");
        }

        var order = new Order
        {
            Customer_ID = request.Customer_ID,
            Computer_ID = activeSessions[0].Computer_ID,
            Order_Date = DateTime.UtcNow,
            Total_Amount = totalAmount,
            Status = "Pending"
        };
        dbContext.Orders.Add(order);

        // Save the principal first so SQL Server generates Order_ID. Both saves remain
        // inside the explicit transaction and roll back together on any later failure.
        await dbContext.SaveChangesAsync(cancellationToken);

        var orderLines = quantities
            .OrderBy(x => x.Key)
            .Select(x => new OrderDetail
            {
                Order_ID = order.OrderId,
                Product_ID = x.Key,
                Quantity = x.Value,
                Unit_Price = products[x.Key].Price
            })
            .ToList();
        dbContext.OrderDetails.AddRange(orderLines);
        dbContext.Transactions.Add(new FinancialTransaction
        {
            Customer_ID = request.Customer_ID,
            Order_ID = order.OrderId,
            Trans_Type = "FoodOrder",
            Amount = totalAmount
        });

        // The order-detail trigger reduces inventory and the transaction trigger debits
        // the customer. Do not duplicate either update in application code.
        await dbContext.SaveChangesAsync(cancellationToken);

        var balanceAfter = await dbContext.Customers
            .AsNoTracking()
            .Where(x => x.CustomerId == request.Customer_ID)
            .Select(x => x.Balance)
            .SingleAsync(cancellationToken) ?? 0m;

        await dbTransaction.CommitAsync(cancellationToken);

        var responseLines = orderLines.Select(line => new OrderLineResponse(
            line.Product_ID,
            products[line.Product_ID].Product_Name,
            line.Quantity,
            line.Unit_Price,
            line.Unit_Price * line.Quantity)).ToList();
        var response = new OrderResponse(
            order.OrderId,
            request.Customer_ID,
            order.Computer_ID,
            order.Status ?? "Pending",
            totalAmount,
            balanceAfter,
            order.Order_Date,
            responseLines);

        await NotifyNewOrderAsync(response, cancellationToken);
        await NotifyBalanceUpdatedAsync(request.Customer_ID, balanceAfter, order.OrderId, cancellationToken);
        return response;
    }

    private async Task NotifyNewOrderAsync(OrderResponse order, CancellationToken cancellationToken)
    {
        try
        {
            await hubContext.Clients.Group(CafeHub.WebAdminGroup)
                .SendAsync("NewOrderReceived", order, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not broadcast new order {OrderId}.", order.Order_ID);
        }
    }

    private async Task NotifyBalanceUpdatedAsync(
        int customerId,
        decimal balance,
        int orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            await hubContext.Clients.Group(CafeHub.CustomerGroup(customerId))
                .SendAsync("BalanceUpdated", new { Balance = balance, Order_ID = orderId }, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not send balance update after order {OrderId}.", orderId);
        }
    }
}
