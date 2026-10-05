using System.Security.Claims;
using InternetCafe.API.DTOs.Transactions;
using InternetCafe.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternetCafe.API.Controllers;

[ApiController]
[Authorize(Roles = "Employee,Admin")]
[Route("api/transactions")]
public sealed class TransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpPost("topup")]
    [ProducesResponseType<TopUpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TopUpResponse>> TopUp(
        [FromBody] TopUpRequest request,
        CancellationToken cancellationToken)
    {
        var employeeClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(employeeClaim, out var employeeId))
        {
            return Unauthorized();
        }

        var response = await transactionService.TopUpAsync(request, employeeId, cancellationToken);
        return Ok(response);
    }
}
