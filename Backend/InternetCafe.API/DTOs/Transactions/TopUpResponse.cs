namespace InternetCafe.API.DTOs.Transactions;

public sealed record TopUpResponse(
    int Transaction_ID,
    int Customer_ID,
    decimal Paid_Amount,
    decimal Bonus_Amount,
    decimal Balance,
    string? Receipt_Code,
    DateTime? Trans_Date);
