using InternetCafe.API.DTOs.Products;
using InternetCafe.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(
    IProductService productService,
    ILogger<ProductsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetActiveProducts(
        CancellationToken cancellationToken)
    {
        var products = await productService.GetActiveProductsAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<ProductCategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductCategoryResponse>>> GetCategories(
        CancellationToken cancellationToken)
    {
        return Ok(await productService.GetCategoriesAsync(cancellationToken));
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPost]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await productService.CreateProductAsync(request, cancellationToken));
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Database rejected product creation.");
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Product could not be created",
                Detail = "Check the product fields and selected category."
            });
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Product creation failed due to invalid data.");
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid product data",
                Detail = exception.Message
            });
        }
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Update(
        int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await productService.UpdateProductAsync(id, request, cancellationToken));
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Database rejected update for product {ProductId}.", id);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Product could not be updated",
                Detail = "Check the product fields and selected category."
            });
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Product update failed for product {ProductId}.", id);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid product data",
                Detail = exception.Message
            });
        }
    }

    [Authorize(Roles = "Employee,Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await productService.DeleteProductAsync(id, cancellationToken);
            return NoContent();
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Database rejected soft-delete for product {ProductId}.", id);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Product could not be deactivated",
                Detail = "The product status could not be saved."
            });
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Product deactivation failed for product {ProductId}.", id);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Product could not be deactivated",
                Detail = exception.Message
            });
        }
    }
}
