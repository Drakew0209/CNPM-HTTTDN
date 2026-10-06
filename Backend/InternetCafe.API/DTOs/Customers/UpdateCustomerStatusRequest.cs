using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Customers;

public sealed record UpdateCustomerStatusRequest(
    [property: Required, RegularExpression("^(Active|Banned|Inactive)$")] string Status);
