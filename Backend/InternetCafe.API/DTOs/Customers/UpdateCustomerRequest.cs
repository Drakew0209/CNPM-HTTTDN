using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Customers;

public sealed record UpdateCustomerRequest(
    [property: Required, StringLength(100, MinimumLength = 1)] string Full_Name,
    [property: Range(1, int.MaxValue)] int Tier_ID,
    [property: StringLength(72, MinimumLength = 8)] string? Password = null);
