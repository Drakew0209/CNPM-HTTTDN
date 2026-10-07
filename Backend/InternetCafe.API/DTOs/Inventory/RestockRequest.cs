using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Inventory;

public sealed record RestockRequest(
    [Range(1, int.MaxValue)] int Product_ID,
    [Range(1, int.MaxValue)] int Quantity,
    [StringLength(255)] string? Note = null);
