using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Orders;

public sealed record UpdateOrderStatusRequest(
    [property: Required, RegularExpression("^(Pending|Preparing|Served|Cancelled)$")] string Status);
