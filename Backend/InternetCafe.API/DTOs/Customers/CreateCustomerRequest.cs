using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Customers;

public sealed record CreateCustomerRequest(
    [Required, StringLength(50, MinimumLength = 3)] string Username,
    [Required, StringLength(72, MinimumLength = 8)] string Password,
    [Required, StringLength(100, MinimumLength = 1)] string Full_Name,
    [Range(1, int.MaxValue)] int Tier_ID);
