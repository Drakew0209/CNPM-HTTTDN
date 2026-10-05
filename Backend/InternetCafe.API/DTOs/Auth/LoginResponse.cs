namespace InternetCafe.API.DTOs.Auth;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    int Id,
    string Role,
    string? AccessLevel);
