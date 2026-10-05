using InternetCafe.API.Data;
using InternetCafe.API.DTOs.AdminData;
using InternetCafe.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Services;

public sealed class AdminDataService(InternetCafeDbContext dbContext) : IAdminDataService
{
    public async Task<IReadOnlyList<TransactionListResponse>> GetTransactionsAsync(CancellationToken cancellationToken)
    {
        return await (
            from transaction in dbContext.Transactions.AsNoTracking()
            join customer in dbContext.Customers.AsNoTracking()
                on transaction.Customer_ID equals customer.CustomerId
            orderby transaction.Trans_Date descending, transaction.TransactionId descending
            select new TransactionListResponse(
                transaction.TransactionId,
                customer.CustomerId,
                customer.Username,
                customer.Full_Name,
                transaction.Amount,
                transaction.Trans_Type,
                transaction.Trans_Date))
            .Take(1000)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UsageSessionListResponse>> GetSessionsAsync(CancellationToken cancellationToken)
    {
        return await (
            from session in dbContext.UsageSessions.AsNoTracking()
            join customer in dbContext.Customers.AsNoTracking()
                on session.Customer_ID equals customer.CustomerId
            join computer in dbContext.Computers.AsNoTracking()
                on session.Computer_ID equals computer.ComputerId
            orderby session.Start_Time descending, session.SessionId descending
            select new UsageSessionListResponse(
                session.SessionId,
                customer.CustomerId,
                customer.Full_Name,
                computer.ComputerId,
                computer.Computer_Code,
                session.Start_Time,
                session.End_Time,
                session.Total_Hours,
                session.Applied_Hourly_Rate,
                session.Amount,
                session.Status ?? "Unknown"))
            .Take(1000)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FeedbackListResponse>> GetFeedbackAsync(CancellationToken cancellationToken)
    {
        return await (
            from feedback in dbContext.Feedback.AsNoTracking()
            join customer in dbContext.Customers.AsNoTracking()
                on feedback.Customer_ID equals customer.CustomerId
            join handler in dbContext.Employees.AsNoTracking()
                on feedback.Handled_By equals (int?)handler.EmployeeId into handlers
            from handler in handlers.DefaultIfEmpty()
            orderby feedback.Submitted_Date descending, feedback.FeedbackId descending
            select new FeedbackListResponse(
                feedback.FeedbackId,
                customer.CustomerId,
                customer.Full_Name,
                handler == null ? null : handler.Full_Name,
                feedback.Subject,
                feedback.Content,
                feedback.Submitted_Date,
                feedback.Status,
                feedback.Manager_Notes))
            .Take(1000)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeListResponse>> GetEmployeesAsync(CancellationToken cancellationToken)
    {
        return await (
            from employee in dbContext.Employees.AsNoTracking()
            join position in dbContext.Positions.AsNoTracking()
                on employee.Position_ID equals position.PositionId
            orderby employee.EmployeeId
            select new EmployeeListResponse(
                employee.EmployeeId,
                employee.Username,
                employee.Full_Name,
                position.Position_Name,
                position.Access_Level ?? "Staff",
                employee.Hire_Date,
                employee.Base_Salary,
                employee.Status))
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkScheduleListResponse>> GetWorkSchedulesAsync(CancellationToken cancellationToken)
    {
        return await (
            from schedule in dbContext.WorkSchedules.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on schedule.Employee_ID equals employee.EmployeeId
            join shift in dbContext.WorkShifts.AsNoTracking()
                on schedule.Shift_ID equals shift.ShiftId
            orderby schedule.Work_Date descending, employee.Full_Name
            select new WorkScheduleListResponse(
                schedule.ScheduleId,
                employee.EmployeeId,
                employee.Full_Name,
                shift.Shift_Name,
                schedule.Work_Date,
                shift.Start_Time,
                shift.End_Time,
                schedule.Status))
            .Take(1000)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceListResponse>> GetAttendanceAsync(CancellationToken cancellationToken)
    {
        return await (
            from attendance in dbContext.Attendances.AsNoTracking()
            join schedule in dbContext.WorkSchedules.AsNoTracking()
                on attendance.Schedule_ID equals schedule.ScheduleId
            join employee in dbContext.Employees.AsNoTracking()
                on schedule.Employee_ID equals employee.EmployeeId
            join shift in dbContext.WorkShifts.AsNoTracking()
                on schedule.Shift_ID equals shift.ShiftId
            orderby schedule.Work_Date descending, attendance.AttendanceId descending
            select new AttendanceListResponse(
                attendance.AttendanceId,
                employee.EmployeeId,
                employee.Full_Name,
                schedule.Work_Date,
                shift.Shift_Name,
                attendance.Check_In_Time,
                attendance.Check_Out_Time,
                attendance.Note,
                schedule.Status))
            .Take(1000)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollListResponse>> GetPayrollAsync(CancellationToken cancellationToken)
    {
        return await (
            from payroll in dbContext.Payrolls.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on payroll.Employee_ID equals employee.EmployeeId
            orderby payroll.Pay_Year descending, payroll.Pay_Month descending, employee.Full_Name
            select new PayrollListResponse(
                payroll.PayrollId,
                employee.EmployeeId,
                employee.Full_Name,
                payroll.Pay_Month,
                payroll.Pay_Year,
                payroll.Base_Salary,
                payroll.Bonus,
                payroll.Deduction,
                payroll.Net_Salary,
                payroll.Payment_Date,
                payroll.Status))
            .Take(1000)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequestListResponse>> GetLeaveRequestsAsync(CancellationToken cancellationToken)
    {
        return await (
            from leave in dbContext.LeaveRequests.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on leave.Employee_ID equals employee.EmployeeId
            join approver in dbContext.Employees.AsNoTracking()
                on leave.Approved_By equals (int?)approver.EmployeeId into approvers
            from approver in approvers.DefaultIfEmpty()
            orderby leave.Request_Date descending, leave.LeaveId descending
            select new LeaveRequestListResponse(
                leave.LeaveId,
                employee.EmployeeId,
                employee.Full_Name,
                leave.Leave_Type,
                leave.Start_Date,
                leave.End_Date,
                leave.Reason,
                leave.Status,
                approver == null ? null : approver.Full_Name,
                leave.Request_Date))
            .Take(1000)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryTransactionListResponse>> GetInventoryTransactionsAsync(
        CancellationToken cancellationToken)
    {
        return await (
            from inventory in dbContext.InventoryTransactions.AsNoTracking()
            join product in dbContext.Products.AsNoTracking()
                on inventory.Product_ID equals product.ProductId
            join employee in dbContext.Employees.AsNoTracking()
                on inventory.Employee_ID equals employee.EmployeeId
            orderby inventory.Created_Date descending, inventory.InventoryTransactionId descending
            select new InventoryTransactionListResponse(
                inventory.InventoryTransactionId,
                product.ProductId,
                product.Product_Name,
                employee.EmployeeId,
                employee.Full_Name,
                inventory.Trans_Type,
                inventory.Quantity,
                inventory.Note,
                inventory.Created_Date))
            .Take(1000)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryProductListResponse>> GetInventoryProductsAsync(
        CancellationToken cancellationToken)
    {
        return await (
            from product in dbContext.Products.AsNoTracking()
            join category in dbContext.ProductCategories.AsNoTracking()
                on product.Category_ID equals category.CategoryId
            orderby product.Product_Name
            select new InventoryProductListResponse(
                product.ProductId,
                product.Product_Name,
                category.CategoryId,
                category.Category_Name,
                product.Price,
                product.Stock_Quantity,
                product.Status))
            .Take(1000)
            .ToListAsync(cancellationToken);
    }
}
