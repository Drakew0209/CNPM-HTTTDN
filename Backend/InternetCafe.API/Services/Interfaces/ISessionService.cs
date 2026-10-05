using InternetCafe.API.DTOs.Sessions;

namespace InternetCafe.API.Services.Interfaces;

public interface ISessionService
{
    Task<StartSessionResponse> StartSessionAsync(
        int customerId,
        int computerId,
        int? employeeId,
        CancellationToken cancellationToken);

    Task<EndSessionResponse> EndSessionAsync(
        int sessionId,
        int callerId,
        bool isEmployee,
        int? employeeId,
        CancellationToken cancellationToken);
}
