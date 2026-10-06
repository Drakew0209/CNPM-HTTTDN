using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Hrm;

public sealed record CreateEmployeeRequest(
    [property: Required, StringLength(50, MinimumLength = 3)] string Username,
    [property: Required, StringLength(72, MinimumLength = 8)] string Password,
    [property: Required, StringLength(100, MinimumLength = 1)] string Full_Name,
    [property: Range(1, int.MaxValue)] int Position_ID,
    [property: Range(typeof(decimal), "0", "9999999999.99")] decimal Base_Salary);

public sealed record RunPayrollRequest(
    [property: Range(1, 12)] int Pay_Month,
    [property: Range(2000, 9999)] int Pay_Year,
    [property: Range(1, int.MaxValue)] int? Employee_ID = null,
    [property: Range(typeof(decimal), "0", "9999999999.99")] decimal Bonus = 0m,
    [property: Range(typeof(decimal), "0", "9999999999.99")] decimal Deduction = 0m);
