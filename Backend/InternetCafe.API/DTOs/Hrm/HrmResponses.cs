using InternetCafe.API.DTOs.AdminData;

namespace InternetCafe.API.DTOs.Hrm;

public sealed record PositionOptionResponse(
    int Position_ID,
    string Position_Name,
    int Department_ID,
    string Department_Name,
    string Access_Level);

public sealed record PayrollRunResponse(
    int Created_Count,
    IReadOnlyList<PayrollListResponse> Payrolls);
