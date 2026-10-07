using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Orders;

public sealed record OrderItemRequest(
    [Range(1, int.MaxValue)] int Product_ID,
    [Range(1, 1000)] int Quantity);
