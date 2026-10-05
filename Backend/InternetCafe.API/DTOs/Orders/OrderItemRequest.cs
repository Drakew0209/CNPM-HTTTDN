using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Orders;

public sealed record OrderItemRequest(
    [property: Range(1, int.MaxValue)] int Product_ID,
    [property: Range(1, 1000)] int Quantity);
