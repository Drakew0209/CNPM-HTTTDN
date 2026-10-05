using InternetCafe.API.DTOs.Products;

namespace InternetCafe.API.Services.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetActiveProductsAsync(CancellationToken cancellationToken);
}
