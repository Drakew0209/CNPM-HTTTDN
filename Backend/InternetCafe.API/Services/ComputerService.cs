using InternetCafe.API.Data;
using InternetCafe.API.DTOs.Computers;
using InternetCafe.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Services;

public sealed class ComputerService(InternetCafeDbContext dbContext) : IComputerService
{
    public async Task<IReadOnlyList<ComputerResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await (
            from computer in dbContext.Computers.AsNoTracking()
            join session in dbContext.UsageSessions.AsNoTracking().Where(x => x.Status == "Active")
                on computer.ComputerId equals session.Computer_ID into activeSessions
            from session in activeSessions.DefaultIfEmpty()
            orderby computer.Computer_Code
            select new ComputerResponse(
                computer.ComputerId,
                computer.Computer_Code,
                computer.Status ?? "Unknown",
                computer.Zone_Type ?? "Standard",
                computer.Hourly_Rate,
                session == null ? null : (int?)session.SessionId,
                session == null ? null : (int?)session.Customer_ID))
            .ToListAsync(cancellationToken);
    }
}
