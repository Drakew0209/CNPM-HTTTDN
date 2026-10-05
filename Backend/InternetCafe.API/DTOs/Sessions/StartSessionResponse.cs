namespace InternetCafe.API.DTOs.Sessions;

public sealed record StartSessionResponse(
    int Session_ID,
    int Customer_ID,
    int Computer_ID,
    DateTime Start_Time,
    decimal Applied_Hourly_Rate,
    decimal Balance);
