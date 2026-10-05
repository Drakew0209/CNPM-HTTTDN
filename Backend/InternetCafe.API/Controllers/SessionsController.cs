using System.Security.Claims;
using InternetCafe.API.DTOs.Sessions;
using InternetCafe.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternetCafe.API.Controllers;

[ApiController]
[Authorize(Roles = "Customer,Employee,Admin")]
[Route("api/sessions")]
public sealed class SessionsController(ISessionService sessionService) : ControllerBase
{
    [HttpPost("start")]
    [ProducesResponseType<StartSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StartSessionResponse>> Start(
        [FromBody] StartSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out var callerId, out var role))
        {
            return Unauthorized();
        }

        int customerId;
        int? employeeId;
        if (role == "Customer")
        {
            if (request.Customer_ID is not null && request.Customer_ID != callerId)
            {
                return Forbid();
            }

            customerId = callerId;
            employeeId = null;
        }
        else if (role is "Employee" or "Admin")
        {
            if (request.Customer_ID is null)
            {
                return BadRequest("Customer_ID is required when an employee starts a session for a customer.");
            }

            customerId = request.Customer_ID.Value;
            employeeId = callerId;
        }
        else
        {
            return Forbid();
        }

        var response = await sessionService.StartSessionAsync(
            customerId, request.Computer_ID, employeeId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{sessionId:int}/end")]
    [ProducesResponseType<EndSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EndSessionResponse>> End(
        int sessionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCaller(out var callerId, out var role))
        {
            return Unauthorized();
        }

        if (sessionId <= 0)
        {
            return BadRequest("sessionId must be greater than zero.");
        }

        var isEmployee = role is "Employee" or "Admin";
        if (!isEmployee && role != "Customer")
        {
            return Forbid();
        }

        var response = await sessionService.EndSessionAsync(
            sessionId,
            callerId,
            isEmployee,
            isEmployee ? callerId : null,
            cancellationToken);
        return Ok(response);
    }

    private bool TryGetCaller(out int callerId, out string role)
    {
        role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out callerId);
    }
}
