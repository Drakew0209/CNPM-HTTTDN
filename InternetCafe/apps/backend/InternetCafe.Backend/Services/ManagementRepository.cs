using InternetCafe.Backend.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace InternetCafe.Backend.Services;

/// <summary>
/// Read models for the management, CRM, HR, and system-account areas.
/// This class deliberately contains no mutations: the write services own validation and state changes.
/// </summary>
public sealed class ManagementRepository(SqlConnectionFactory connections)
{
    public async Task<ManagementWorkspaceDto> WorkspaceAsync(IdentityContext identity, CancellationToken ct) => new(
        await SuppliersAsync(identity, ct),
        await InventoryAsync(identity, ct),
        await FeedbackAsync(identity, ct),
        await SurveysAsync(identity, ct),
        await EmployeesAsync(identity, ct),
        await ShiftsAsync(identity, ct),
        await SchedulesAsync(identity, ct),
        await LeavesAsync(identity, ct),
        await AttendanceAsync(identity, ct),
        await PayrollAsync(identity, ct),
        await AccountsAsync(identity, ct),
        await MaterialsAsync(identity, ct),
        await BomAsync(identity, ct),
        await MaterialRequestsAsync(identity, ct));

    public Task<IReadOnlyList<SupplierReadDto>> SuppliersAsync(IdentityContext identity, CancellationToken ct) =>
        CanManage(identity)
            ? QueryAsync("""
                SELECT Supplier_ID, Name, Phone, Email, Address, Status
                FROM dbo.Suppliers
                ORDER BY Name, Supplier_ID
                """, r => new SupplierReadDto(
                    Id("supplier", r.GetInt32(0)), r.GetString(1), Text(r, 2), Text(r, 3), Text(r, 4),
                    string.Equals(Text(r, 5), "Active", StringComparison.OrdinalIgnoreCase)), ct)
            : EmptyAsync<SupplierReadDto>();

    public Task<IReadOnlyList<InventoryReadDto>> InventoryAsync(IdentityContext identity, CancellationToken ct) =>
        CanManage(identity)
            ? QueryAsync("""
                SELECT Inv_Trans_ID, Product_ID, Supplier_ID, Trans_Type, Quantity, Note, Created_Date
                FROM dbo.Inventory_Transactions
                ORDER BY Created_Date DESC, Inv_Trans_ID DESC
                """, r => new InventoryReadDto(
                    Id("inv", r.GetInt32(0)), Id("p", r.GetInt32(1)), NullableId("supplier", r, 2), r.GetString(3), r.GetInt32(4), Text(r, 5), DateTimeValue(r, 6)), ct)
            : EmptyAsync<InventoryReadDto>();

    public Task<IReadOnlyList<FeedbackReadDto>> FeedbackAsync(IdentityContext identity, CancellationToken ct)
    {
        if (!CanManage(identity) && identity.CustomerId is null) return EmptyAsync<FeedbackReadDto>();
        return QueryAsync("""
            SELECT Feedback_ID, Customer_ID, Subject, Content, Status, Manager_Notes, Submitted_Date
            FROM dbo.Feedback
            WHERE @ownOnly = 0 OR Customer_ID = @customerId
            ORDER BY Submitted_Date DESC, Feedback_ID DESC
            """, r => new FeedbackReadDto(
                Id("feedback", r.GetInt32(0)), Id("c", r.GetInt32(1)), r.GetString(2), r.GetString(3),
                FeedbackStatus(Text(r, 4)), Text(r, 5), DateTimeValue(r, 6)), ct,
            ("@ownOnly", CanManage(identity) ? 0 : 1), ("@customerId", (object?)identity.CustomerId ?? DBNull.Value));
    }

    public async Task<IReadOnlyList<SurveyReadDto>> SurveysAsync(IdentityContext identity, CancellationToken ct)
    {
        if (!CanManage(identity) && identity.Role != Roles.Customer) return [];

        var surveys = await QueryAsync("""
            SELECT s.Survey_ID, s.Title, s.Description, s.Lifecycle_Status
            FROM dbo.Surveys s
            WHERE @management = 1
               OR (s.Lifecycle_Status = 'Published' AND EXISTS (
                   SELECT 1 FROM dbo.Survey_Targets st
                   WHERE st.Survey_ID = s.Survey_ID AND st.Customer_ID = @customerId))
            ORDER BY s.Survey_ID DESC
            """, r => new SurveyShell(r.GetInt32(0), r.GetString(1), Text(r, 2), Text(r, 3)), ct,
            ("@management", CanManage(identity) ? 1 : 0), ("@customerId", (object?)identity.CustomerId ?? DBNull.Value));
        if (surveys.Count == 0) return [];

        var questions = await QueryAsync("""
            SELECT Question_ID, Survey_ID, Question_Text, Options
            FROM dbo.Survey_Questions
            ORDER BY Survey_ID, Question_ID
            """, r => new SurveyQuestionShell(r.GetInt32(0), r.GetInt32(1), r.GetString(2), Text(r, 3)), ct);
        var responses = await QueryAsync("""
            SELECT Survey_ID, Customer_ID, Question_ID, Answer_Text, Submitted_Date
            FROM dbo.Survey_Responses
            ORDER BY Survey_ID, Customer_ID, Submitted_Date, Response_ID
            """, r => new SurveyAnswerShell(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetString(3), DateTimeValue(r, 4)), ct);

        var allowedSurveyIds = surveys.Select(x => x.SurveyId).ToHashSet();
        var targets = await QueryAsync("""
            SELECT Survey_ID, Customer_ID
            FROM dbo.Survey_Targets
            ORDER BY Survey_ID, Customer_ID
            """, r => new SurveyTargetShell(r.GetInt32(0), r.GetInt32(1)), ct);
        var targetLookup = targets.Where(x => allowedSurveyIds.Contains(x.SurveyId)).GroupBy(x => x.SurveyId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Select(target => Id("c", target.CustomerId)).ToList());
        var questionLookup = questions.Where(x => allowedSurveyIds.Contains(x.SurveyId)).GroupBy(x => x.SurveyId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<SurveyQuestionReadDto>)x.Select(question => new SurveyQuestionReadDto(
                Id("q", question.QuestionId), question.Text,
                question.Options.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))).ToList());

        var responseLookup = responses.Where(x => allowedSurveyIds.Contains(x.SurveyId))
            .GroupBy(x => new { x.SurveyId, x.CustomerId })
            .ToDictionary(group => group.Key, group => (IReadOnlyList<SurveyResponseReadDto>)[new SurveyResponseReadDto(
                Id("c", group.Key.CustomerId),
                group.GroupBy(x => x.QuestionId).ToDictionary(x => Id("q", x.Key), x => x.Last().Answer),
                group.Max(x => x.CreatedAt))]);

        return surveys.Select(survey => new SurveyReadDto(
            Id("survey", survey.SurveyId), survey.Title, survey.Description,
            questionLookup.GetValueOrDefault(survey.SurveyId, []),
            targetLookup.GetValueOrDefault(survey.SurveyId, []),
            survey.LifecycleStatus,
            responseLookup.Where(pair => pair.Key.SurveyId == survey.SurveyId).SelectMany(pair => pair.Value).ToList())).ToList();
    }

    public Task<IReadOnlyList<EmployeeReadDto>> EmployeesAsync(IdentityContext identity, CancellationToken ct)
    {
        if (!CanManage(identity) && identity.EmployeeId is null) return EmptyAsync<EmployeeReadDto>();
        return QueryAsync("""
            SELECT e.Employee_ID, e.Full_Name, e.Phone_Number, e.Email, d.Department_Name, p.Position_Name,
                   e.Status, e.Base_Salary, e.Hire_Date, e.Qualification, e.Date_Of_Birth
            FROM dbo.Employees e
            JOIN dbo.Positions p ON p.Position_ID = e.Position_ID
            JOIN dbo.Departments d ON d.Department_ID = p.Department_ID
            WHERE @ownOnly = 0 OR e.Employee_ID = @employeeId
            ORDER BY e.Full_Name, e.Employee_ID
            """, r => new EmployeeReadDto(
                Id("e", r.GetInt32(0)), r.GetString(1), Text(r, 2), Text(r, 3), r.GetString(4), r.GetString(5),
                EmployeeStatus(Text(r, 6)), DecimalValue(r, 7), DateOnlyValue(r, 8), Text(r, 9), DateOnlyValue(r, 10)), ct,
            ("@ownOnly", CanManage(identity) ? 0 : 1), ("@employeeId", (object?)identity.EmployeeId ?? DBNull.Value));
    }

    public Task<IReadOnlyList<ShiftReadDto>> ShiftsAsync(IdentityContext identity, CancellationToken ct)
    {
        // A staff member needs the shift labels to interpret their own schedules.
        if (!CanManage(identity) && identity.EmployeeId is null) return EmptyAsync<ShiftReadDto>();
        return QueryAsync("SELECT Shift_ID, Shift_Name, Start_Time, End_Time FROM dbo.Work_Shifts ORDER BY Start_Time, Shift_ID",
            r => new ShiftReadDto(Id("shift", r.GetInt32(0)), r.GetString(1), TimeValue(r, 2), TimeValue(r, 3)), ct);
    }

    public Task<IReadOnlyList<ScheduleReadDto>> SchedulesAsync(IdentityContext identity, CancellationToken ct)
    {
        if (!CanManage(identity) && identity.EmployeeId is null) return EmptyAsync<ScheduleReadDto>();
        return QueryAsync("""
            SELECT Schedule_ID, Employee_ID, Shift_ID, Work_Date, Status
            FROM dbo.Work_Schedules
            WHERE @ownOnly = 0 OR Employee_ID = @employeeId
            ORDER BY Work_Date DESC, Schedule_ID DESC
            """, r => new ScheduleReadDto(Id("schedule", r.GetInt32(0)), Id("e", r.GetInt32(1)), Id("shift", r.GetInt32(2)), DateOnlyValue(r, 3), Text(r, 4)), ct,
            ("@ownOnly", CanManage(identity) ? 0 : 1), ("@employeeId", (object?)identity.EmployeeId ?? DBNull.Value));
    }

    public Task<IReadOnlyList<LeaveReadDto>> LeavesAsync(IdentityContext identity, CancellationToken ct)
    {
        if (!CanManage(identity) && identity.EmployeeId is null) return EmptyAsync<LeaveReadDto>();
        return QueryAsync("""
            SELECT Leave_ID, Employee_ID, Leave_Type, Start_Date, End_Date, Reason, Status
            FROM dbo.Leave_Requests
            WHERE @ownOnly = 0 OR Employee_ID = @employeeId
            ORDER BY Start_Date DESC, Leave_ID DESC
            """, r => new LeaveReadDto(Id("leave", r.GetInt32(0)), Id("e", r.GetInt32(1)), LeaveType(Text(r, 2)),
                DateOnlyValue(r, 3), DateOnlyValue(r, 4), Text(r, 5), Text(r, 6)), ct,
            ("@ownOnly", CanManage(identity) ? 0 : 1), ("@employeeId", (object?)identity.EmployeeId ?? DBNull.Value));
    }

    public Task<IReadOnlyList<AttendanceReadDto>> AttendanceAsync(IdentityContext identity, CancellationToken ct)
    {
        if (!CanManage(identity) && identity.EmployeeId is null) return EmptyAsync<AttendanceReadDto>();
        return QueryAsync("""
            SELECT a.Attendance_ID, s.Employee_ID, a.Schedule_ID, a.Check_In_Time, a.Check_Out_Time, a.Note
            FROM dbo.Attendance a
            JOIN dbo.Work_Schedules s ON s.Schedule_ID = a.Schedule_ID
            WHERE @ownOnly = 0 OR s.Employee_ID = @employeeId
            ORDER BY a.Check_In_Time DESC, a.Attendance_ID DESC
            """, r => new AttendanceReadDto(Id("attendance", r.GetInt32(0)), Id("e", r.GetInt32(1)), Id("schedule", r.GetInt32(2)),
                NullableDateTimeValue(r, 3), NullableDateTimeValue(r, 4), Text(r, 5)), ct,
            ("@ownOnly", CanManage(identity) ? 0 : 1), ("@employeeId", (object?)identity.EmployeeId ?? DBNull.Value));
    }

    public Task<IReadOnlyList<PayrollReadDto>> PayrollAsync(IdentityContext identity, CancellationToken ct)
    {
        if (!CanManage(identity) && identity.EmployeeId is null) return EmptyAsync<PayrollReadDto>();
        return QueryAsync("""
            SELECT Payroll_ID, Employee_ID, Pay_Year, Pay_Month, Base_Salary, Bonus, Deduction, Net_Salary, Workflow_Status
            FROM dbo.Payroll
            WHERE @ownOnly = 0 OR Employee_ID = @employeeId
            ORDER BY Pay_Year DESC, Pay_Month DESC, Payroll_ID DESC
            """, r => new PayrollReadDto(Id("payroll", r.GetInt32(0)), Id("e", r.GetInt32(1)),
                $"{r.GetInt32(2):D4}-{r.GetInt32(3):D2}", DecimalValue(r, 4), DecimalValue(r, 5), DecimalValue(r, 6), DecimalValue(r, 7),
                PayrollStatus(Text(r, 8))), ct,
            ("@ownOnly", CanManage(identity) ? 0 : 1), ("@employeeId", (object?)identity.EmployeeId ?? DBNull.Value));
    }

    public Task<IReadOnlyList<AccountReadDto>> AccountsAsync(IdentityContext identity, CancellationToken ct) =>
        identity.Role == Roles.Admin
            ? QueryAsync("""
                SELECT Account_ID, Username, Full_Name, Role, Status, Employee_ID
                FROM dbo.App_Accounts
                WHERE Role <> 'Customer'
                ORDER BY Username, Account_ID
                """, r => new AccountReadDto(Id("a", r.GetInt32(0)), r.GetString(1), r.GetString(2), r.GetString(3),
                    AccountStatus(Text(r, 4)), NullableId("e", r, 5)), ct)
            : EmptyAsync<AccountReadDto>();

    public Task<IReadOnlyList<MaterialReadDto>> MaterialsAsync(IdentityContext identity, CancellationToken ct) =>
        CanManage(identity) || identity.Role == Roles.Staff
            ? QueryAsync("""
                SELECT m.Material_ID, m.Material_Code, m.Material_Name, m.Unit, m.Stock_Quantity, m.Reorder_Level, m.Unit_Cost, m.Status, COALESCE(s.Name, '') AS SupplierName
                FROM dbo.Materials m
                LEFT JOIN dbo.Suppliers s ON s.Supplier_ID = m.Supplier_ID
                ORDER BY m.Material_Name, m.Material_ID
                """, r => new MaterialReadDto(
                    Id("mat", r.GetInt32(0)), r.GetString(1), r.GetString(2), r.GetString(3),
                    DecimalValue(r, 4), DecimalValue(r, 5), DecimalValue(r, 6),
                    r.GetString(7), Text(r, 8)), ct)
            : EmptyAsync<MaterialReadDto>();

    public Task<IReadOnlyList<BomReadDto>> BomAsync(IdentityContext identity, CancellationToken ct) =>
        CanManage(identity) || identity.Role == Roles.Staff
            ? QueryAsync("""
                SELECT pm.Product_ID, p.Product_Name, pm.Material_ID, m.Material_Name, pm.Quantity, m.Unit
                FROM dbo.Product_Materials pm
                JOIN dbo.Products p ON p.Product_ID = pm.Product_ID
                JOIN dbo.Materials m ON m.Material_ID = pm.Material_ID
                ORDER BY p.Product_Name, m.Material_Name
                """, r => new BomReadDto(
                    $"{r.GetInt32(0)}-{r.GetInt32(2)}",
                    Id("p", r.GetInt32(0)), r.GetString(1),
                    Id("mat", r.GetInt32(2)), r.GetString(3),
                    DecimalValue(r, 4), r.GetString(5)), ct)
            : EmptyAsync<BomReadDto>();

    public Task<IReadOnlyList<MaterialRequestReadDto>> MaterialRequestsAsync(IdentityContext identity, CancellationToken ct) =>
        CanManage(identity) || identity.Role == Roles.Staff
            ? QueryAsync("""
                SELECT r.Request_ID, r.Item_Type, r.Material_ID, m.Material_Name, r.Product_ID, p.Product_Name,
                       r.Quantity, r.Status, r.Note,
                       COALESCE(eReq.Full_Name, aReq.Full_Name, 'Nhân viên') AS RequesterName,
                       COALESCE(eDec.Full_Name, aDec.Full_Name, '') AS DeciderName,
                       r.Decided_At, r.Created_At
                FROM dbo.Material_Requests r
                LEFT JOIN dbo.Materials m ON m.Material_ID = r.Material_ID
                LEFT JOIN dbo.Products p ON p.Product_ID = r.Product_ID
                LEFT JOIN dbo.App_Accounts aReq ON aReq.Account_ID = r.Requested_By_Account_ID
                LEFT JOIN dbo.Employees eReq ON eReq.Employee_ID = aReq.Employee_ID OR eReq.Employee_ID = r.Requested_By_Account_ID
                LEFT JOIN dbo.App_Accounts aDec ON aDec.Account_ID = r.Decided_By_Account_ID
                LEFT JOIN dbo.Employees eDec ON eDec.Employee_ID = aDec.Employee_ID OR eDec.Employee_ID = r.Decided_By_Account_ID
                ORDER BY r.Created_At DESC, r.Request_ID DESC
                """, r => new MaterialRequestReadDto(
                    Id("matreq", r.GetInt32(0)),
                    r.GetString(1),
                    NullableId("mat", r, 2), Text(r, 3),
                    NullableId("p", r, 4), Text(r, 5),
                    DecimalValue(r, 6),
                    r.GetString(7),
                    Text(r, 8),
                    Text(r, 9),
                    r.IsDBNull(10) ? null : r.GetString(10),
                    NullableDateTimeValue(r, 11),
                    DateTimeValue(r, 12)), ct)
            : EmptyAsync<MaterialRequestReadDto>();

    private async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, Func<SqlDataReader, T> map, CancellationToken ct, params (string Name, object Value)[] parameters)
    {
        await using var connection = await connections.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<T>();
        while (await reader.ReadAsync(ct)) rows.Add(map(reader));
        return rows;
    }

    private static Task<IReadOnlyList<T>> EmptyAsync<T>() => Task.FromResult<IReadOnlyList<T>>([]);
    private static bool CanManage(IdentityContext identity) => identity.Role is Roles.Owner or Roles.Manager;
    private static string Id(string prefix, int value) => $"{prefix}-{value:000}";
    private static string? NullableId(string prefix, SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Id(prefix, reader.GetInt32(ordinal));
    private static string Text(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal);
    private static decimal DecimalValue(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
    private static DateTime DateTimeValue(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? DateTime.UnixEpoch : reader.GetDateTime(ordinal);
    private static DateTime? NullableDateTimeValue(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    private static string DateOnlyValue(SqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? "" : DateOnly.FromDateTime(reader.GetDateTime(ordinal)).ToString("yyyy-MM-dd");
    private static string TimeValue(SqlDataReader reader, int ordinal) => reader.GetFieldValue<TimeSpan>(ordinal).ToString(@"hh\:mm");
    private static string FeedbackStatus(string status) => status.Equals("Reviewed", StringComparison.OrdinalIgnoreCase) ? "Processing" : status is "Pending" or "Processing" or "Resolved" ? status : "Pending";
    private static string EmployeeStatus(string status) => status.Equals("Active", StringComparison.OrdinalIgnoreCase) ? "Active" : "Inactive";
    private static string LeaveType(string type) => 
        type.Equals("Sick", StringComparison.OrdinalIgnoreCase) ? "Sick" : 
        type.Equals("Resignation", StringComparison.OrdinalIgnoreCase) ? "Resignation" : 
        type.Equals("Maternity", StringComparison.OrdinalIgnoreCase) ? "Maternity" : 
        "Annual";
    private static string PayrollStatus(string status) => status.Equals("Paid", StringComparison.OrdinalIgnoreCase) ? "Paid" : status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ? "Approved" : "Draft";
    private static string AccountStatus(string status) => status.Equals("Banned", StringComparison.OrdinalIgnoreCase) ? "Banned" : "Active";

    private sealed record SurveyShell(int SurveyId, string Title, string Description, string LifecycleStatus);
    private sealed record SurveyTargetShell(int SurveyId, int CustomerId);
    private sealed record SurveyQuestionShell(int QuestionId, int SurveyId, string Text, string Options);
    private sealed record SurveyAnswerShell(int SurveyId, int CustomerId, int QuestionId, string Answer, DateTime CreatedAt);
}

public sealed record ManagementWorkspaceDto(
    IReadOnlyList<SupplierReadDto> Suppliers,
    IReadOnlyList<InventoryReadDto> Inventory,
    IReadOnlyList<FeedbackReadDto> Feedback,
    IReadOnlyList<SurveyReadDto> Surveys,
    IReadOnlyList<EmployeeReadDto> Employees,
    IReadOnlyList<ShiftReadDto> Shifts,
    IReadOnlyList<ScheduleReadDto> Schedules,
    IReadOnlyList<LeaveReadDto> Leaves,
    IReadOnlyList<AttendanceReadDto> Attendance,
    IReadOnlyList<PayrollReadDto> Payroll,
    IReadOnlyList<AccountReadDto> Accounts,
    IReadOnlyList<MaterialReadDto> Materials,
    IReadOnlyList<BomReadDto> Bom,
    IReadOnlyList<MaterialRequestReadDto> MaterialRequests);

public sealed record SupplierReadDto(string Id, string Name, string Phone, string Email, string Address, bool Active);
public sealed record InventoryReadDto(string Id, string ProductId, string? SupplierId, string Type, int Quantity, string Note, DateTime CreatedAt);
public sealed record FeedbackReadDto(string Id, string CustomerId, string Subject, string Content, string Status, string Response, DateTime CreatedAt);
public sealed record SurveyQuestionReadDto(string Id, string Text, IReadOnlyList<string> Options);
public sealed record SurveyResponseReadDto(string CustomerId, IReadOnlyDictionary<string, string> Answers, DateTime CreatedAt);
public sealed record SurveyReadDto(string Id, string Title, string Description, IReadOnlyList<SurveyQuestionReadDto> Questions, IReadOnlyList<string> CustomerIds, string Status, IReadOnlyList<SurveyResponseReadDto> Responses);
public sealed record EmployeeReadDto(string Id, string FullName, string Phone, string Email, string Department, string Position, string Status, decimal BaseSalary, string JoinedAt, string Qualification, string? DateOfBirth);
public sealed record ShiftReadDto(string Id, string Name, string StartTime, string EndTime);
public sealed record ScheduleReadDto(string Id, string EmployeeId, string ShiftId, string Date, string Status);
public sealed record LeaveReadDto(string Id, string EmployeeId, string Type, string StartDate, string EndDate, string Reason, string Status);
public sealed record AttendanceReadDto(string Id, string EmployeeId, string ScheduleId, DateTime? CheckIn, DateTime? CheckOut, string? Note);
public sealed record PayrollReadDto(string Id, string EmployeeId, string Month, decimal BaseSalary, decimal Bonus, decimal Deduction, decimal Total, string Status);
public sealed record AccountReadDto(string Id, string Username, string FullName, string Role, string Status, string? EmployeeId);
public sealed record MaterialReadDto(string Id, string Code, string Name, string Unit, decimal StockQuantity, decimal ReorderLevel, decimal UnitCost, string Status, string SupplierName);
public sealed record BomReadDto(string Id, string ProductId, string ProductName, string MaterialId, string MaterialName, decimal Quantity, string Unit);
public sealed record MaterialRequestReadDto(string Id, string ItemType, string? MaterialId, string? MaterialName, string? ProductId, string? ProductName, decimal Quantity, string Status, string Note, string RequesterName, string? DeciderName, DateTime? DecidedAt, DateTime CreatedAt);
