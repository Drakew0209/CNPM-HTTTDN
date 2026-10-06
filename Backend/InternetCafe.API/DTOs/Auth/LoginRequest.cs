using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Auth;

public sealed class LoginRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Identifier { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}
