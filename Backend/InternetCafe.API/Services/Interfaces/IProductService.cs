using InternetCafe.API.DTOs.Products;

namespace InternetCafe.API.Services.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetActiveProductsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken);
    Task<ProductResponse> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken);
    Task DeleteProductAsync(int id, CancellationToken cancellationToken);
}
