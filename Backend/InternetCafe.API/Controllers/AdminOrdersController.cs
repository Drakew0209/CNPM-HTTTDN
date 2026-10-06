using System.Security.Claims;
using InternetCafe.API.DTOs.Orders;
using InternetCafe.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternetCafe.API.Controllers;

[ApiController]
[Authorize(Roles = "Employee,Admin")]
[Route("api/orders")]
public sealed class AdminOrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PendingOrderResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PendingOrderResponse>>> GetPending(
        CancellationToken cancellationToken)
    {
        return Ok(await orderService.GetPendingAsync(cancellationToken));
    }

    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(int id, CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId)) return Unauthorized();
        await orderService.CompleteAsync(id, employeeId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId)) return Unauthorized();
        await orderService.UpdateStatusAsync(id, request.Status, employeeId, cancellationToken);
        return NoContent();
    }

    private bool TryGetEmployeeId(out int employeeId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out employeeId);
}
