using System.ComponentModel.DataAnnotations;

namespace InternetCafe.API.DTOs.Hrm;

public sealed record CreateEmployeeRequest(
    [Required, StringLength(50, MinimumLength = 3)] string Username,
    [Required, StringLength(72, MinimumLength = 8)] string Password,
    [Required, StringLength(100, MinimumLength = 1)] string Full_Name,
    [Range(1, int.MaxValue)] int Position_ID,
    [Range(typeof(decimal), "0", "9999999999.99")] decimal Base_Salary);

public sealed record RunPayrollRequest(
    [Range(1, 12)] int Pay_Month,
    [Range(2000, 9999)] int Pay_Year,
    [Range(1, int.MaxValue)] int? Employee_ID = null,
    [Range(typeof(decimal), "0", "9999999999.99")] decimal Bonus = 0m,
    [Range(typeof(decimal), "0", "9999999999.99")] decimal Deduction = 0m);
