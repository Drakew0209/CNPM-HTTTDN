using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Orders;

public sealed record OrderRequest(
    [Range(1, int.MaxValue)] int Customer_ID,
    [Required, MinLength(1), MaxLength(100)] IReadOnlyList<OrderItemRequest> Items);
