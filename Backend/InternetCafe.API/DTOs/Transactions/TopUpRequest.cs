using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Transactions;

public sealed record TopUpRequest(
    [Range(1, int.MaxValue)] int Customer_ID,
    [Range(typeof(decimal), "0.01", "9999999999.99")] decimal Amount,
    [Range(1, int.MaxValue)] int? Combo_ID = null);
