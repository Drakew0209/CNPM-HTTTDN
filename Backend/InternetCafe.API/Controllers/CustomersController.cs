using InternetCafe.API.DTOs.Customers;
using InternetCafe.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Controllers;

[ApiController]
[Authorize(Roles = "Employee,Admin")]
[Route("api/customers")]
public sealed class CustomersController(
    ICustomerService customerService,
    ILogger<CustomersController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CustomerListResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerListResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await customerService.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<CustomerListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerListResponse>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await customerService.CreateAsync(request, cancellationToken));
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Database rejected customer creation.");
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Customer could not be created",
                Detail = "Check that the username and membership tier are valid."
            });
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Customer creation failed due to invalid data.");
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid customer data",
                Detail = exception.Message
            });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<CustomerListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerListResponse>> Update(
        int id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await customerService.UpdateAsync(id, request, cancellationToken));
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Database rejected update for customer {CustomerId}.", id);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Customer could not be updated",
                Detail = "Check the customer data and selected membership tier."
            });
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Customer update failed for customer {CustomerId}.", id);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid customer data",
                Detail = exception.Message
            });
        }
    }

    [HttpPut("{id:int}/status")]
    [ProducesResponseType<CustomerListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerListResponse>> UpdateStatus(
        int id,
        [FromBody] UpdateCustomerStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await customerService.UpdateStatusAsync(id, request, cancellationToken));
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Database rejected status update for customer {CustomerId}.", id);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Customer status could not be updated",
                Detail = "The requested status is not permitted by the database."
            });
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Customer status update failed for customer {CustomerId}.", id);
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid customer status",
                Detail = exception.Message
            });
        }
    }
}
