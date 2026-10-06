using InternetCafe.API.DTOs.Orders;

namespace InternetCafe.API.Services.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(OrderRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PendingOrderResponse>> GetPendingAsync(CancellationToken cancellationToken);
    Task UpdateStatusAsync(int orderId, string status, int employeeId, CancellationToken cancellationToken);
    Task CompleteAsync(int orderId, int employeeId, CancellationToken cancellationToken);
}
