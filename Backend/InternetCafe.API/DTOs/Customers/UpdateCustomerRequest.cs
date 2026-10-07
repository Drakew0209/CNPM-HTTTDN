using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Customers;

public sealed record UpdateCustomerRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Full_Name,
    [Range(1, int.MaxValue)] int Tier_ID,
    [StringLength(72, MinimumLength = 8)] string? Password = null);
