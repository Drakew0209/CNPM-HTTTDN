using System.Data;
using System.Text;
using InternetCafe.API.Data;
using InternetCafe.API.Data.Entities;
using InternetCafe.API.DTOs.AdminData;
using InternetCafe.API.DTOs.Hrm;
using InternetCafe.API.ExceptionHandling;
using InternetCafe.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InternetCafe.API.Services;

public sealed class HrmService(InternetCafeDbContext dbContext) : IHrmService
{
    public async Task<IReadOnlyList<PositionOptionResponse>> GetPositionsAsync(CancellationToken cancellationToken)
    {
        return await (
            from position in dbContext.Positions.AsNoTracking()
            join department in dbContext.Departments.AsNoTracking()
                on position.Department_ID equals department.DepartmentId
            orderby department.Department_Name, position.Position_Name
            select new PositionOptionResponse(
                position.PositionId,
                position.Position_Name,
                department.DepartmentId,
                department.Department_Name,
                position.Access_Level ?? "Staff"))
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeListResponse> CreateEmployeeAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        if (await dbContext.Employees.AnyAsync(x => x.Username == username, cancellationToken))
        {
            throw new ConflictException("That employee username is already in use.");
        }

        if (Encoding.UTF8.GetByteCount(request.Password) > 72)
        {
            throw new ArgumentException("Password must not exceed 72 UTF-8 bytes for BCrypt hashing.");
        }

        var position = await dbContext.Positions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.PositionId == request.Position_ID, cancellationToken);
        if (position is null)
        {
            throw new ArgumentException("The selected employee position does not exist.");
        }

        var employee = new Employee
        {
            Username = username,
            Password_Hash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Full_Name = request.Full_Name.Trim(),
            Position_ID = request.Position_ID,
            Hire_Date = DateTime.UtcNow.Date,
            Base_Salary = request.Base_Salary,
            Status = "Active",
            Created_Date = DateTime.UtcNow
        };

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new EmployeeListResponse(
            employee.EmployeeId,
            employee.Username,
            employee.Full_Name,
            position.Position_Name,
            position.Access_Level ?? "Staff",
            employee.Hire_Date,
            employee.Base_Salary,
            employee.Status);
    }

    public async Task<PayrollRunResponse> RunPayrollAsync(
        RunPayrollRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var employeesQuery = dbContext.Employees
            .AsNoTracking()
            .Where(x => x.Status == "Active");
        if (request.Employee_ID is int employeeId)
        {
            employeesQuery = employeesQuery.Where(x => x.EmployeeId == employeeId);
        }

        var employees = await employeesQuery
            .OrderBy(x => x.EmployeeId)
            .ToListAsync(cancellationToken);
        if (employees.Count == 0)
        {
            throw new KeyNotFoundException(request.Employee_ID is null
                ? "No active employees were found for payroll."
                : "Active employee was not found.");
        }

        var employeeIds = employees.Select(x => x.EmployeeId).ToArray();
        var existingPayrollEmployeeIds = await dbContext.Payrolls
            .AsNoTracking()
            .Where(x => x.Pay_Month == request.Pay_Month &&
                        x.Pay_Year == request.Pay_Year &&
                        employeeIds.Contains(x.Employee_ID))
            .Select(x => x.Employee_ID)
            .ToListAsync(cancellationToken);

        if (request.Employee_ID is not null && existingPayrollEmployeeIds.Contains(request.Employee_ID.Value))
        {
            throw new ConflictException("Payroll already exists for this employee and month.");
        }

        var employeesToPay = employees
            .Where(x => !existingPayrollEmployeeIds.Contains(x.EmployeeId))
            .ToList();
        if (employeesToPay.Count == 0)
        {
            throw new ConflictException("Payroll already exists for every active employee in this month.");
        }

        foreach (var employee in employeesToPay)
        {
            var baseSalary = employee.Base_Salary ?? 0m;
            if (request.Deduction > baseSalary + request.Bonus)
            {
                throw new ArgumentException($"Deduction exceeds net available salary for employee #{employee.EmployeeId}.");
            }
        }

        var payrollRows = employeesToPay.Select(employee => new Payroll
        {
            Employee_ID = employee.EmployeeId,
            Pay_Month = request.Pay_Month,
            Pay_Year = request.Pay_Year,
            Base_Salary = employee.Base_Salary ?? 0m,
            Bonus = request.Bonus,
            Deduction = request.Deduction,
            Status = "Unpaid"
        }).ToList();

        dbContext.Payrolls.AddRange(payrollRows);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var createdIds = payrollRows.Select(x => x.PayrollId).ToArray();
        var result = await (
            from payroll in dbContext.Payrolls.AsNoTracking()
            join employee in dbContext.Employees.AsNoTracking()
                on payroll.Employee_ID equals employee.EmployeeId
            where createdIds.Contains(payroll.PayrollId)
            orderby employee.Full_Name
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
            .ToListAsync(cancellationToken);

        return new PayrollRunResponse(result.Count, result);
    }
}
