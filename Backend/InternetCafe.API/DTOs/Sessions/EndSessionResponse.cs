namespace InternetCafe.API.DTOs.Sessions;

public sealed record EndSessionResponse(
    int Session_ID,
    int Customer_ID,
    int Computer_ID,
    DateTime Start_Time,
    DateTime End_Time,
    decimal Total_Hours,
    decimal Applied_Hourly_Rate,
    decimal Calculated_Amount,
    decimal Charged_Amount,
    decimal Waived_Amount,
    decimal Balance_After);
