using InternetCafe.API.DTOs.Products;
using InternetCafe.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InternetCafe.API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetActiveProducts(
        CancellationToken cancellationToken)
    {
        var products = await productService.GetActiveProductsAsync(cancellationToken);
        return Ok(products);
    }
}
