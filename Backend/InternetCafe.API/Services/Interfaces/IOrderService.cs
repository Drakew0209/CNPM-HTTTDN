using InternetCafe.API.DTOs.Orders;

namespace InternetCafe.API.Services.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(OrderRequest request, CancellationToken cancellationToken);
}
