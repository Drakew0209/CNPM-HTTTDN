using InternetCafe.API.DTOs.AdminData;
using InternetCafe.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternetCafe.API.Controllers;

[ApiController]
[Authorize(Roles = "Employee,Admin")]
[Route("api/admin-data")]
public sealed class AdminDataController(IAdminDataService adminDataService) : ControllerBase
{
    [HttpGet("transactions")]
    public Task<IReadOnlyList<TransactionListResponse>> GetTransactions(CancellationToken cancellationToken) =>
        adminDataService.GetTransactionsAsync(cancellationToken);

    [HttpGet("sessions")]
    public Task<IReadOnlyList<UsageSessionListResponse>> GetSessions(CancellationToken cancellationToken) =>
        adminDataService.GetSessionsAsync(cancellationToken);

    [HttpGet("feedback")]
    public Task<IReadOnlyList<FeedbackListResponse>> GetFeedback(CancellationToken cancellationToken) =>
        adminDataService.GetFeedbackAsync(cancellationToken);

    [HttpGet("employees")]
    public Task<IReadOnlyList<EmployeeListResponse>> GetEmployees(CancellationToken cancellationToken) =>
        adminDataService.GetEmployeesAsync(cancellationToken);

    [HttpGet("work-schedules")]
    public Task<IReadOnlyList<WorkScheduleListResponse>> GetWorkSchedules(CancellationToken cancellationToken) =>
        adminDataService.GetWorkSchedulesAsync(cancellationToken);

    [HttpGet("attendance")]
    public Task<IReadOnlyList<AttendanceListResponse>> GetAttendance(CancellationToken cancellationToken) =>
        adminDataService.GetAttendanceAsync(cancellationToken);

    [HttpGet("payroll")]
    public Task<IReadOnlyList<PayrollListResponse>> GetPayroll(CancellationToken cancellationToken) =>
        adminDataService.GetPayrollAsync(cancellationToken);

    [HttpGet("leave-requests")]
    public Task<IReadOnlyList<LeaveRequestListResponse>> GetLeaveRequests(CancellationToken cancellationToken) =>
        adminDataService.GetLeaveRequestsAsync(cancellationToken);

    [HttpGet("inventory-transactions")]
    public Task<IReadOnlyList<InventoryTransactionListResponse>> GetInventoryTransactions(
        CancellationToken cancellationToken) =>
        adminDataService.GetInventoryTransactionsAsync(cancellationToken);

    [HttpGet("products")]
    public Task<IReadOnlyList<InventoryProductListResponse>> GetInventoryProducts(
        CancellationToken cancellationToken) =>
        adminDataService.GetInventoryProductsAsync(cancellationToken);
}
