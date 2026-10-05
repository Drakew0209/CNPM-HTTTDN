using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Orders;

public sealed record OrderRequest(
    [property: Range(1, int.MaxValue)] int Customer_ID,
    [property: Required, MinLength(1), MaxLength(100)] IReadOnlyList<OrderItemRequest> Items);
