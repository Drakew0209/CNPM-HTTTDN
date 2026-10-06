using InternetCafe.API.DTOs.AdminData;
using InternetCafe.API.DTOs.Hrm;

namespace InternetCafe.API.Services.Interfaces;

public interface IHrmService
{
    Task<IReadOnlyList<PositionOptionResponse>> GetPositionsAsync(CancellationToken cancellationToken);
    Task<EmployeeListResponse> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken cancellationToken);
    Task<PayrollRunResponse> RunPayrollAsync(RunPayrollRequest request, CancellationToken cancellationToken);
}
