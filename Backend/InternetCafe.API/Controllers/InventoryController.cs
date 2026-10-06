using System.Security.Claims;
using InternetCafe.API.DTOs.Inventory;
using InternetCafe.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Controllers;

[ApiController]
[Authorize(Roles = "Employee,Admin")]
[Route("api/inventory")]
public sealed class InventoryController(
    IInventoryService inventoryService,
    ILogger<InventoryController> logger) : ControllerBase
{
    [HttpPost("restock")]
    [ProducesResponseType<RestockResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RestockResponse>> Restock(
        [FromBody] RestockRequest request,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var employeeId))
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await inventoryService.RestockAsync(request, employeeId, cancellationToken));
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Database rejected restock for product {ProductId}.", request.Product_ID);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Restock could not be saved",
                Detail = "Check product, quantity and inventory constraints."
            });
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Restock failed for product {ProductId}.", request.Product_ID);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid restock request",
                Detail = exception.Message
            });
        }
    }
}
