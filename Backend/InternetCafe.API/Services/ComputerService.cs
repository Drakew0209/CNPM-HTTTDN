using InternetCafe.API.Data;
using InternetCafe.API.DTOs.Computers;
using InternetCafe.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Services;

public sealed class ComputerService(InternetCafeDbContext dbContext) : IComputerService
{
    public async Task<IReadOnlyList<ComputerResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Computers
            .AsNoTracking()
            .OrderBy(x => x.Computer_Code)
            .Select(x => new ComputerResponse(
                x.ComputerId,
                x.Computer_Code,
                x.Status ?? "Unknown",
                x.Hourly_Rate))
            .ToListAsync(cancellationToken);
    }
}
