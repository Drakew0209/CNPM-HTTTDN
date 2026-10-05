namespace InternetCafe.API.DTOs.AdminData;

public sealed record TransactionListResponse(
    int Transaction_ID,
    int Customer_ID,
    string Customer_Username,
    string Customer_Name,
    decimal Amount,
    string Trans_Type,
    DateTime? Trans_Date);

public sealed record UsageSessionListResponse(
    int Session_ID,
    int Customer_ID,
    string Customer_Name,
    int Computer_ID,
    string Computer_Name,
    DateTime Start_Time,
    DateTime? End_Time,
    decimal? Total_Hours,
    decimal? Applied_Hourly_Rate,
    decimal? Amount,
    string Status);

public sealed record FeedbackListResponse(
    int Feedback_ID,
    int Customer_ID,
    string Customer_Name,
    string? Handled_By_Name,
    string Subject,
    string Content,
    DateTime? Submitted_Date,
    string? Status,
    string? Manager_Notes);

public sealed record EmployeeListResponse(
    int Employee_ID,
    string Username,
    string Full_Name,
    string Position_Name,
    string Access_Level,
    DateTime? Hire_Date,
    decimal? Base_Salary,
    string? Status);

public sealed record WorkScheduleListResponse(
    int Schedule_ID,
    int Employee_ID,
    string Employee_Name,
    string Shift_Name,
    DateTime Work_Date,
    TimeSpan Start_Time,
    TimeSpan End_Time,
    string? Status);

public sealed record AttendanceListResponse(
    int Attendance_ID,
    int Employee_ID,
    string Employee_Name,
    DateTime Work_Date,
    string Shift_Name,
    DateTime? Check_In_Time,
    DateTime? Check_Out_Time,
    string? Note,
    string? Schedule_Status);

public sealed record PayrollListResponse(
    int Payroll_ID,
    int Employee_ID,
    string Employee_Name,
    int Pay_Month,
    int Pay_Year,
    decimal Base_Salary,
    decimal? Bonus,
    decimal? Deduction,
    decimal? Net_Salary,
    DateTime? Payment_Date,
    string? Status);

public sealed record LeaveRequestListResponse(
    int Leave_ID,
    int Employee_ID,
    string Employee_Name,
    string Leave_Type,
    DateTime Start_Date,
    DateTime End_Date,
    string? Reason,
    string? Status,
    string? Approved_By_Name,
    DateTime? Request_Date);

public sealed record InventoryTransactionListResponse(
    int Inv_Trans_ID,
    int Product_ID,
    string Product_Name,
    int Employee_ID,
    string Employee_Name,
    string Trans_Type,
    int Quantity,
    string? Note,
    DateTime? Created_Date);

public sealed record InventoryProductListResponse(
    int Product_ID,
    string Product_Name,
    int Category_ID,
    string Category_Name,
    decimal Price,
    int Stock_Quantity,
    string? Status);
