using InternetCafe.API.DTOs.Computers;
using InternetCafe.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternetCafe.API.Controllers;

[ApiController]
[Authorize]
[Route("api/computers")]
public sealed class ComputersController(IComputerService computerService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ComputerResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ComputerResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var computers = await computerService.GetAllAsync(cancellationToken);
        return Ok(computers);
    }
}
