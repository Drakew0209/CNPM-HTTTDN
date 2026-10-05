using InternetCafe.API.DTOs.Computers;

namespace InternetCafe.API.Services.Interfaces;

public interface IComputerService
{
    Task<IReadOnlyList<ComputerResponse>> GetAllAsync(CancellationToken cancellationToken);
}
