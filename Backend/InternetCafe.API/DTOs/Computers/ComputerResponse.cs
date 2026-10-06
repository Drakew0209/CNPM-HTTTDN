namespace InternetCafe.API.DTOs.Computers;

public sealed record ComputerResponse(
    int Computer_ID,
    string Computer_Name,
    string Status,
    string Zone_Type,
    decimal Hourly_Rate,
    int? Active_Session_ID = null,
    int? Active_Customer_ID = null);
