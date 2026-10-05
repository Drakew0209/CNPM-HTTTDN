namespace InternetCafe.API.DTOs.Customers;

public sealed record CustomerListResponse(
    int Customer_ID,
    string Username,
    string Full_Name,
    decimal Balance,
    string Tier_Name,
    string Status);
