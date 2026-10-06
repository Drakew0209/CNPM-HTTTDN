using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Customers;

public sealed record CreateCustomerRequest(
    [property: Required, StringLength(50, MinimumLength = 3)] string Username,
    [property: Required, StringLength(72, MinimumLength = 8)] string Password,
    [property: Required, StringLength(100, MinimumLength = 1)] string Full_Name,
    [property: Range(1, int.MaxValue)] int Tier_ID);
