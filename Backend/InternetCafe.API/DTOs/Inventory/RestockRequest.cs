using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Inventory;

public sealed record RestockRequest(
    [property: Range(1, int.MaxValue)] int Product_ID,
    [property: Range(1, int.MaxValue)] int Quantity,
    [property: StringLength(255)] string? Note = null);
