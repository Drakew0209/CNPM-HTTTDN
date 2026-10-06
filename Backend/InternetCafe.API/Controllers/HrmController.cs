using InternetCafe.API.DTOs.AdminData;
using InternetCafe.API.DTOs.Hrm;
using InternetCafe.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/hrm")]
public sealed class HrmController(
    IHrmService hrmService,
    ILogger<HrmController> logger) : ControllerBase
{
    [HttpGet("positions")]
    [ProducesResponseType<IReadOnlyList<PositionOptionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PositionOptionResponse>>> GetPositions(CancellationToken cancellationToken)
    {
        return Ok(await hrmService.GetPositionsAsync(cancellationToken));
    }

    [HttpPost("employees")]
    [ProducesResponseType<EmployeeListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeListResponse>> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hrmService.CreateEmployeeAsync(request, cancellationToken));
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Database rejected employee creation.");
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Employee could not be created",
                Detail = "Check that the username and position are valid and unique."
            });
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Employee creation failed due to invalid data.");
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid employee data",
                Detail = exception.Message
            });
        }
    }

    [HttpPost("payroll/run")]
    [ProducesResponseType<PayrollRunResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PayrollRunResponse>> RunPayroll(
        [FromBody] RunPayrollRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await hrmService.RunPayrollAsync(request, cancellationToken));
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Database rejected payroll generation.");
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Payroll could not be generated",
                Detail = "A payroll row may already exist for one or more employees in this period."
            });
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Payroll generation failed due to invalid data.");
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid payroll data",
                Detail = exception.Message
            });
        }
    }

    [HttpPost("employees/{id:int}/payroll")]
    [ProducesResponseType<PayrollRunResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<PayrollRunResponse>> RunEmployeePayroll(
        int id,
        [FromBody] RunPayrollRequest request,
        CancellationToken cancellationToken)
    {
        return RunPayroll(request with { Employee_ID = id }, cancellationToken);
    }
}
