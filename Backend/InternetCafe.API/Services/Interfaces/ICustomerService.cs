using InternetCafe.API.DTOs.Customers;

namespace InternetCafe.API.Services.Interfaces;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerListResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<CustomerListResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken);
    Task<CustomerListResponse> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken);
    Task<CustomerListResponse> UpdateStatusAsync(int id, UpdateCustomerStatusRequest request, CancellationToken cancellationToken);
}
