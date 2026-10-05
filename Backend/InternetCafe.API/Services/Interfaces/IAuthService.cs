using InternetCafe.API.DTOs.Auth;

namespace InternetCafe.API.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
