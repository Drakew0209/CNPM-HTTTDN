namespace InternetCafe.API.DTOs.Computers;

public sealed record ComputerResponse(
    int Computer_ID,
    string Computer_Name,
    string Status,
    decimal Hourly_Rate);
