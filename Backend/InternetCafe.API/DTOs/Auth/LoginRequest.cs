using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Auth;

public sealed record LoginRequest(
    [property: Required, StringLength(100, MinimumLength = 1)] string Identifier,
    [property: Required, StringLength(200, MinimumLength = 1)] string Password);
