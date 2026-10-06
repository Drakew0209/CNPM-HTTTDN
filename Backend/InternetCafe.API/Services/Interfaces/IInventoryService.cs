using InternetCafe.API.DTOs.Inventory;

namespace InternetCafe.API.Services.Interfaces;

public interface IInventoryService
{
    Task<RestockResponse> RestockAsync(
        RestockRequest request,
        int employeeId,
        CancellationToken cancellationToken);
}
