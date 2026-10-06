using InternetCafe.API.Data;
using InternetCafe.API.Data.Entities;
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

    public async Task<IReadOnlyList<ProductCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ProductCategories
            .AsNoTracking()
            .OrderBy(x => x.Category_Name)
            .Select(x => new ProductCategoryResponse(x.CategoryId, x.Category_Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductResponse> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var categoryExists = await dbContext.ProductCategories
            .AnyAsync(x => x.CategoryId == request.Category_ID, cancellationToken);
        if (!categoryExists)
        {
            throw new ArgumentException("The selected product category does not exist.");
        }

        var product = new Product
        {
            Product_Name = request.Product_Name.Trim(),
            Category_ID = request.Category_ID,
            Price = request.Price,
            Stock_Quantity = request.Stock_Quantity,
            Status = "Active"
        };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetProductResponseAsync(product.ProductId, cancellationToken);
    }

    public async Task<ProductResponse> UpdateProductAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(x => x.ProductId == id, cancellationToken);
        if (product is null)
        {
            throw new KeyNotFoundException("Product was not found.");
        }

        var categoryExists = await dbContext.ProductCategories
            .AnyAsync(x => x.CategoryId == request.Category_ID, cancellationToken);
        if (!categoryExists)
        {
            throw new ArgumentException("The selected product category does not exist.");
        }

        product.Product_Name = request.Product_Name.Trim();
        product.Category_ID = request.Category_ID;
        product.Price = request.Price;
        product.Stock_Quantity = request.Stock_Quantity;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetProductResponseAsync(product.ProductId, cancellationToken);
    }

    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(x => x.ProductId == id, cancellationToken);
        if (product is null)
        {
            throw new KeyNotFoundException("Product was not found.");
        }

        // Keep historical Order_Details and inventory references intact.
        product.Status = "Inactive";
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ProductResponse> GetProductResponseAsync(int id, CancellationToken cancellationToken)
    {
        return await (
            from product in dbContext.Products.AsNoTracking()
            join category in dbContext.ProductCategories.AsNoTracking()
                on product.Category_ID equals category.CategoryId
            where product.ProductId == id
            select new ProductResponse(
                product.ProductId,
                product.Product_Name,
                product.Price,
                product.Stock_Quantity,
                null,
                product.Category_ID,
                category.Category_Name))
            .SingleAsync(cancellationToken);
    }
}
