using InternetCafe.API.DTOs.AdminData;

namespace InternetCafe.API.Services.Interfaces;

public interface IAdminDataService
{
    Task<IReadOnlyList<TransactionListResponse>> GetTransactionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<UsageSessionListResponse>> GetSessionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<FeedbackListResponse>> GetFeedbackAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<EmployeeListResponse>> GetEmployeesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkScheduleListResponse>> GetWorkSchedulesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AttendanceListResponse>> GetAttendanceAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PayrollListResponse>> GetPayrollAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<LeaveRequestListResponse>> GetLeaveRequestsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryProductListResponse>> GetInventoryProductsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryTransactionListResponse>> GetInventoryTransactionsAsync(CancellationToken cancellationToken);
}
