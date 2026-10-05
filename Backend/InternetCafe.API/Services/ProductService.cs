using InternetCafe.API.Data;
using InternetCafe.API.DTOs.Products;
using InternetCafe.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Services;

public sealed class ProductService(InternetCafeDbContext dbContext) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> GetActiveProductsAsync(CancellationToken cancellationToken)
    {
        return await (
            from product in dbContext.Products.AsNoTracking()
            join category in dbContext.ProductCategories.AsNoTracking()
                on product.Category_ID equals category.CategoryId
            where product.Status == "Active"
            orderby category.Category_Name, product.Product_Name
            select new ProductResponse(
                product.ProductId,
                product.Product_Name,
                product.Price,
                product.Stock_Quantity,
                null, // The current schema has no product image column.
                product.Category_ID,
                category.Category_Name))
            .ToListAsync(cancellationToken);
    }
}
