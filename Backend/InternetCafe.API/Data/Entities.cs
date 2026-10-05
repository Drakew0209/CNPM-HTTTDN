using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Data.Entities;

[Table("Departments", Schema = "dbo")]
public class Department
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Department_ID")]
    public int DepartmentId { get; set; }
    [Required, MaxLength(100), Unicode]
    public string Department_Name { get; set; } = null!;
}

[Table("Positions", Schema = "dbo")]
public class Position
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Position_ID")]
    public int PositionId { get; set; }
    [Required, MaxLength(100), Unicode]
    public string Position_Name { get; set; } = null!;
    public int Department_ID { get; set; }
    [MaxLength(20), Unicode(false)]
    public string? Access_Level { get; set; }
}

[Table("Employees", Schema = "dbo")]
public class Employee
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Employee_ID")]
    public int EmployeeId { get; set; }
    [Required, MaxLength(50), Unicode]
    public string Username { get; set; } = null!;
    [Required, MaxLength(255), Unicode]
    public string Password_Hash { get; set; } = null!;
    [Required, MaxLength(100), Unicode]
    public string Full_Name { get; set; } = null!;
    public DateTime? Date_Of_Birth { get; set; }
    [MaxLength(10), Unicode(false)] public string? Gender { get; set; }
    [MaxLength(15), Unicode(false)] public string? Phone_Number { get; set; }
    [MaxLength(100), Unicode(false)] public string? Email { get; set; }
    [MaxLength(255), Unicode] public string? Address { get; set; }
    public int Position_ID { get; set; }
    public DateTime? Hire_Date { get; set; }
    [Precision(12, 2)] public decimal? Base_Salary { get; set; }
    [MaxLength(20), Unicode(false)] public string? Status { get; set; }
    public DateTime? Created_Date { get; set; }
}

[Table("Computers", Schema = "dbo")]
public class Computer
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Computer_ID")]
    public int ComputerId { get; set; }
    [Required, MaxLength(20), Unicode(false)] public string Computer_Code { get; set; } = null!;
    [MaxLength(20), Unicode(false)] public string? Zone_Type { get; set; }
    [Precision(10, 2)] public decimal Hourly_Rate { get; set; }
    [MaxLength(20), Unicode(false)] public string? Status { get; set; }
}

[Table("Membership_Tier", Schema = "dbo")]
public class MembershipTier
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Tier_ID")]
    public int TierId { get; set; }
    [Required, MaxLength(50), Unicode] public string Tier_Name { get; set; } = null!;
    [Precision(5, 2)] public decimal? Discount_Rate { get; set; }
}

[Table("Surveys", Schema = "dbo")]
public class Survey
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Survey_ID")]
    public int SurveyId { get; set; }
    [Required, MaxLength(200), Unicode] public string Title { get; set; } = null!;
    [Unicode] public string? Description { get; set; }
    public int? Created_By { get; set; }
    public DateTime? Created_Date { get; set; }
    public bool? Is_Active { get; set; }
}

[Table("Product_Categories", Schema = "dbo")]
public class ProductCategory
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Category_ID")]
    public int CategoryId { get; set; }
    [Required, MaxLength(100), Unicode] public string Category_Name { get; set; } = null!;
}

[Table("Work_Shifts", Schema = "dbo")]
public class WorkShift
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Shift_ID")]
    public int ShiftId { get; set; }
    [Required, MaxLength(50), Unicode] public string Shift_Name { get; set; } = null!;
    public TimeSpan Start_Time { get; set; }
    public TimeSpan End_Time { get; set; }
}

[Table("Customers", Schema = "dbo")]
public class Customer
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Customer_ID")]
    public int CustomerId { get; set; }
    [Required, MaxLength(50), Unicode] public string Username { get; set; } = null!;
    [Required, MaxLength(255), Unicode] public string Password_Hash { get; set; } = null!;
    [Required, MaxLength(100), Unicode] public string Full_Name { get; set; } = null!;
    public DateTime? Date_Of_Birth { get; set; }
    [MaxLength(255), Unicode] public string? Hobbies { get; set; }
    [MaxLength(15), Unicode(false)] public string? Phone_Number { get; set; }
    [Precision(12, 2)] public decimal? Balance { get; set; }
    public int Tier_ID { get; set; }
    [MaxLength(20), Unicode(false)] public string? Status { get; set; }
    public DateTime? Created_Date { get; set; }
}

[Table("Combos", Schema = "dbo")]
public class Combo
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Combo_ID")]
    public int ComboId { get; set; }
    [Required, MaxLength(100), Unicode] public string Combo_Name { get; set; } = null!;
    [Precision(12, 2)] public decimal Price { get; set; }
    [Precision(12, 2)] public decimal Bonus_Balance { get; set; }
    [Precision(13, 2), DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal? Total_Value { get; private set; }
    [Unicode] public string? Description { get; set; }
    public bool? Is_Active { get; set; }
}

[Table("Survey_Questions", Schema = "dbo")]
public class SurveyQuestion
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Question_ID")]
    public int QuestionId { get; set; }
    public int Survey_ID { get; set; }
    [Required, Unicode] public string Question_Text { get; set; } = null!;
    [MaxLength(20), Unicode(false)] public string? Question_Type { get; set; }
    [Unicode] public string? Options { get; set; }
}

[Table("Usage_Sessions", Schema = "dbo")]
public class UsageSession
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Session_ID")]
    public int SessionId { get; set; }
    public int Customer_ID { get; set; }
    public int Computer_ID { get; set; }
    public int? Employee_ID { get; set; }
    public DateTime Start_Time { get; set; }
    public DateTime? End_Time { get; set; }
    [Precision(12, 2)] public decimal? Start_Balance { get; set; }
    [Precision(10, 2)] public decimal? Applied_Hourly_Rate { get; set; }
    [Precision(10, 2), DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal? Total_Hours { get; private set; }
    [Precision(12, 2)] public decimal? Amount { get; set; }
    [MaxLength(20), Unicode(false)] public string? Status { get; set; }
}

[Table("Orders", Schema = "dbo")]
public class Order
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Order_ID")]
    public int OrderId { get; set; }
    public int Customer_ID { get; set; }
    public int? Computer_ID { get; set; }
    public int? Employee_ID { get; set; }
    public DateTime? Order_Date { get; set; }
    [Precision(12, 2)] public decimal? Total_Amount { get; set; }
    [MaxLength(20), Unicode(false)] public string? Status { get; set; }
}

[Table("Products", Schema = "dbo")]
public class Product
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Product_ID")]
    public int ProductId { get; set; }
    public int Category_ID { get; set; }
    [Required, MaxLength(100), Unicode] public string Product_Name { get; set; } = null!;
    [Precision(12, 2)] public decimal Price { get; set; }
    public int Stock_Quantity { get; set; }
    [MaxLength(20), Unicode(false)] public string? Status { get; set; }
}

[Table("Inventory_Transactions", Schema = "dbo")]
public class InventoryTransaction
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Inv_Trans_ID")]
    public int InventoryTransactionId { get; set; }
    public int Product_ID { get; set; }
    public int Employee_ID { get; set; }
    [Required, MaxLength(20), Unicode(false)] public string Trans_Type { get; set; } = null!;
    public int Quantity { get; set; }
    [MaxLength(255), Unicode] public string? Note { get; set; }
    public DateTime? Created_Date { get; set; }
}

[Table("Work_Schedules", Schema = "dbo")]
public class WorkSchedule
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Schedule_ID")]
    public int ScheduleId { get; set; }
    public int Employee_ID { get; set; }
    public int Shift_ID { get; set; }
    public DateTime Work_Date { get; set; }
    [MaxLength(20), Unicode(false)] public string? Status { get; set; }
}

[Table("Survey_Responses", Schema = "dbo")]
public class SurveyResponse
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Response_ID")]
    public int ResponseId { get; set; }
    public int Survey_ID { get; set; }
    public int Customer_ID { get; set; }
    public int Question_ID { get; set; }
    [Required, Unicode] public string Answer_Text { get; set; } = null!;
    public DateTime? Submitted_Date { get; set; }
}

[Table("Transactions", Schema = "dbo")]
public class FinancialTransaction
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Transaction_ID")]
    public int TransactionId { get; set; }
    public int Customer_ID { get; set; }
    public int? Processed_By { get; set; }
    public int? Order_ID { get; set; }
    public int? Session_ID { get; set; }
    public int? Combo_ID { get; set; }
    [Required, MaxLength(20), Unicode(false)] public string Trans_Type { get; set; } = null!;
    [Precision(12, 2)] public decimal Amount { get; set; }
    [Precision(12, 2)] public decimal? Balance_Before { get; set; }
    public DateTime? Trans_Date { get; set; }
}

[Table("TopUp_Receipts", Schema = "dbo")]
public class TopUpReceipt
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Receipt_ID")]
    public int ReceiptId { get; set; }
    public int Transaction_ID { get; set; }
    [MaxLength(30), Unicode(false), DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public string? Receipt_Code { get; private set; }
    [Required, MaxLength(10), Unicode(false)] public string Trans_Type_Snapshot { get; set; } = null!;
    public int Customer_ID { get; set; }
    public int? Processed_By { get; set; }
    public int? Combo_ID { get; set; }
    [Precision(12, 2)] public decimal Paid_Amount { get; set; }
    [Precision(12, 2)] public decimal Bonus_Amount { get; set; }
    [Precision(13, 2), DatabaseGenerated(DatabaseGeneratedOption.Computed)] public decimal? Total_Credited { get; private set; }
    [Precision(12, 2)] public decimal Balance_Before { get; set; }
    [Precision(13, 2), DatabaseGenerated(DatabaseGeneratedOption.Computed)] public decimal? Balance_After { get; private set; }
    public DateTime Trans_Date { get; set; }
}

[Table("Feedback", Schema = "dbo")]
public class Feedback
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Feedback_ID")]
    public int FeedbackId { get; set; }
    public int Customer_ID { get; set; }
    public int? Handled_By { get; set; }
    [Required, MaxLength(100), Unicode] public string Subject { get; set; } = null!;
    [Required, Unicode] public string Content { get; set; } = null!;
    public DateTime? Submitted_Date { get; set; }
    [MaxLength(20), Unicode(false)] public string? Status { get; set; }
    [Unicode] public string? Manager_Notes { get; set; }
}

[Table("Order_Details", Schema = "dbo")]
public class OrderDetail
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Order_Detail_ID")]
    public int OrderDetailId { get; set; }
    public int Order_ID { get; set; }
    public int Product_ID { get; set; }
    public int Quantity { get; set; }
    [Precision(12, 2)] public decimal Unit_Price { get; set; }
    [Precision(23, 2), DatabaseGenerated(DatabaseGeneratedOption.Computed)] public decimal? Line_Total { get; private set; }
}

[Table("Payroll", Schema = "dbo")]
public class Payroll
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Payroll_ID")]
    public int PayrollId { get; set; }
    public int Employee_ID { get; set; }
    public int Pay_Month { get; set; }
    public int Pay_Year { get; set; }
    [Precision(12, 2)] public decimal Base_Salary { get; set; }
    [Precision(12, 2)] public decimal? Bonus { get; set; }
    [Precision(12, 2)] public decimal? Deduction { get; set; }
    [Precision(13, 2), DatabaseGenerated(DatabaseGeneratedOption.Computed)] public decimal? Net_Salary { get; private set; }
    public DateTime? Payment_Date { get; set; }
    [MaxLength(20), Unicode(false)] public string? Status { get; set; }
}

[Table("Leave_Requests", Schema = "dbo")]
public class LeaveRequest
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Leave_ID")]
    public int LeaveId { get; set; }
    public int Employee_ID { get; set; }
    [Required, MaxLength(20), Unicode(false)] public string Leave_Type { get; set; } = null!;
    public DateTime Start_Date { get; set; }
    public DateTime End_Date { get; set; }
    [MaxLength(255), Unicode] public string? Reason { get; set; }
    [MaxLength(20), Unicode(false)] public string? Status { get; set; }
    public int? Approved_By { get; set; }
    public DateTime? Request_Date { get; set; }
}

[Table("Attendance", Schema = "dbo")]
public class Attendance
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), Column("Attendance_ID")]
    public int AttendanceId { get; set; }
    public int Schedule_ID { get; set; }
    public DateTime? Check_In_Time { get; set; }
    public DateTime? Check_Out_Time { get; set; }
    [MaxLength(255), Unicode] public string? Note { get; set; }
}
