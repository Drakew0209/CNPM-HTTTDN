using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Orders;

public sealed record UpdateOrderStatusRequest(
    [Required, RegularExpression("^(Pending|Preparing|Served|Cancelled)$")] string Status);
