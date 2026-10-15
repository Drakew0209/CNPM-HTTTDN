using InternetCafe.Backend.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace InternetCafe.Backend.Services;

// Writes for the administrative/CRM/HR surfaces.  The original database owns
// balance and stock side effects through triggers, so this class appends their
// source records instead of duplicating those calculations in the API.
public sealed class ManagementService(SqlConnectionFactory connections)
{
    private readonly PasswordHasher<object> passwords = new();

    public async Task<OperationResult<object>> CreateAsync(IdentityContext actor, string resource, JsonElement body, CancellationToken ct) =>
        await ExecuteAsync(async () => resource.ToLowerInvariant() switch
        {
            "computers" => await CreateComputerAsync(actor, body, ct),
            "customers" => await CreateCustomerAsync(actor, body, ct),
            "games" => await CreateGameAsync(actor, body, ct),
            "categories" => await CreateCategoryAsync(actor, body, ct),
            "products" => await CreateProductAsync(actor, body, ct),
            "suppliers" => await CreateSupplierAsync(actor, body, ct),
            "inventory" => await AppendInventoryAsync(actor, body, ct),
            "feedback" => await CreateFeedbackAsync(actor, body, ct),
            "surveys" => await CreateSurveyAsync(actor, body, ct),
            "employees" => await CreateEmployeeAsync(actor, body, ct),
            "shifts" => await CreateShiftAsync(actor, body, ct),
            "schedules" => await CreateScheduleAsync(actor, body, ct),
            "leaves" => await CreateLeaveAsync(actor, body, ct),
            "payroll" => await CreatePayrollAsync(actor, body, ct),
            "accounts" => await CreateAccountAsync(actor, body, ct),
            "materials" => await CreateMaterialAsync(actor, body, ct),
            "bom" => await CreateBomAsync(actor, body, ct),
            "material-requests" => await CreateMaterialRequestAsync(actor, body, ct),
            _ => OperationResult<object>.Fail("NOT_FOUND", "Không hỗ trợ loại dữ liệu này.", StatusCodes.Status404NotFound)
        });

    public async Task<OperationResult<object>> UpdateAsync(IdentityContext actor, string resource, string id, JsonElement body, CancellationToken ct) =>
        await ExecuteAsync(async () => resource.ToLowerInvariant() switch
        {
            "computers" => await UpdateComputerAsync(actor, id, body, ct),
            "customers" => await UpdateCustomerAsync(actor, id, body, ct),
            "games" => await UpdateGameAsync(actor, id, body, ct),
            "categories" => await UpdateCategoryAsync(actor, id, body, ct),
            "products" => await UpdateProductAsync(actor, id, body, ct),
            "suppliers" => await UpdateSupplierAsync(actor, id, body, ct),
            "feedback" => await UpdateFeedbackAsync(actor, id, body, ct),
            "surveys" => await UpdateSurveyAsync(actor, id, body, ct),
            "employees" => await UpdateEmployeeAsync(actor, id, body, ct),
            "shifts" => await UpdateShiftAsync(actor, id, body, ct),
            "schedules" => await UpdateScheduleAsync(actor, id, body, ct),
            "leaves" => await UpdateLeaveAsync(actor, id, body, ct),
            "payroll" => await UpdatePayrollAsync(actor, id, body, ct),
            "accounts" => await UpdateAccountAsync(actor, id, body, ct),
            "materials" => await UpdateMaterialAsync(actor, id, body, ct),
            _ => OperationResult<object>.Fail("NOT_FOUND", "Không hỗ trợ loại dữ liệu này.", StatusCodes.Status404NotFound)
        });

    public async Task<OperationResult<object>> DeleteAsync(IdentityContext actor, string resource, string id, CancellationToken ct) =>
        await ExecuteAsync(async () => resource.ToLowerInvariant() switch
        {
            "customers" => await SetStatusAsync(actor, "customers", id, "Inactive", ct),
            "games" => await SetStatusAsync(actor, "games", id, "Inactive", ct),
            "products" => await SetStatusAsync(actor, "products", id, "Discontinued", ct),
            "suppliers" => await SetStatusAsync(actor, "suppliers", id, "Inactive", ct),
            "employees" => await SetStatusAsync(actor, "employees", id, "Resigned", ct),
            "accounts" => await SetStatusAsync(actor, "accounts", id, "Banned", ct),
            "materials" => await SetStatusAsync(actor, "materials", id, "Inactive", ct),
            "bom" => await DeleteBomAsync(actor, id, ct),
            "categories" => await DeleteCategoryAsync(actor, id, ct),
            "computers" => await DeleteComputerAsync(actor, id, ct),
            "shifts" => await DeleteRowAsync(actor, "shifts", id, ct),
            "schedules" => await DeleteRowAsync(actor, "schedules", id, ct),
            "surveys" => await DeleteSurveyAsync(actor, id, ct),
            _ => OperationResult<object>.Fail("NOT_FOUND", "Không hỗ trợ xóa dữ liệu này.", StatusCodes.Status404NotFound)
        });

    public Task<OperationResult<object>> DecideLeaveAsync(IdentityContext actor, string id, DecisionRequest request, CancellationToken ct) =>
        ExecuteAsync(() => DecideLeaveCoreAsync(actor, id, request, ct));

    public Task<OperationResult<object>> DecideMaterialRequestAsync(IdentityContext actor, string id, DecisionRequest request, CancellationToken ct) =>
        ExecuteAsync(() => DecideMaterialRequestCoreAsync(actor, id, request, ct));

    public Task<OperationResult<object>> CheckInAsync(IdentityContext actor, JsonElement body, CancellationToken ct) =>
        ExecuteAsync(() => CheckInCoreAsync(actor, body, ct));

    public Task<OperationResult<object>> CheckOutAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct) =>
        ExecuteAsync(() => CheckOutCoreAsync(actor, id, body, ct));

    public Task<OperationResult<object>> AdminCheckOutAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct) =>
        ExecuteAsync(() => AdminCheckOutCoreAsync(actor, id, body, ct));

    public Task<OperationResult<object>> CalculateMonthlyPayrollAsync(IdentityContext actor, JsonElement body, CancellationToken ct) =>
        ExecuteAsync(() => CalculateMonthlyPayrollCoreAsync(actor, body, ct));

    public Task<OperationResult<object>> PublishSurveyAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct) =>
        ExecuteAsync(() => PublishSurveyCoreAsync(actor, id, body, ct));

    public Task<OperationResult<object>> SubmitSurveyAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct) =>
        ExecuteAsync(() => SubmitSurveyCoreAsync(actor, id, body, ct));

    private async Task<OperationResult<object>> CreateComputerAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden();
        var name = Required(body, "name"); var zone = Value(body, "zone") ?? "Standard"; var rate = Decimal(body, "hourlyRate");
        if (name is null || rate is null || rate < 0 || zone is not ("Standard" or "VIP")) return Validation("Tên máy, khu vực hoặc giá giờ không hợp lệ.");
        await using var c = await connections.OpenAsync(ct);
        var id = await ScalarIntAsync(c, "INSERT INTO dbo.Computers (Computer_Code, Zone_Type, Hourly_Rate, Status) VALUES (@name,@zone,@rate,'Available'); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, ("@name", name), ("@zone", zone), ("@rate", rate.Value));
        return OperationResult<object>.Ok(new { id = Id("pc", id), name, zone, status = "Available", online = false, hourlyRate = rate.Value });
    }

    private async Task<OperationResult<object>> CreateGameAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden();
        var name = Required(body, "name"); var category = Required(body, "category");
        var executablePath = Required(body, "executablePath"); var bannerUrl = Required(body, "bannerUrl");
        var description = Value(body, "description"); var active = Bool(body, "active") is not false;
        if (name is null || category is null || executablePath is null || bannerUrl is null)
            return Validation("Tên, thể loại, đường dẫn thực thi và URL banner là bắt buộc.");
        var code = "game-" + Guid.NewGuid().ToString("N")[..8];
        await using var c = await connections.OpenAsync(ct);
        var id = await ScalarIntAsync(c, "INSERT INTO dbo.Games (Code,Name,Category,Executable_Path,Banner_Url,Description,Is_Active) VALUES (@code,@name,@category,@path,@banner,@description,@active); SELECT CAST(SCOPE_IDENTITY() AS int);", ct,
            ("@code", code), ("@name", name), ("@category", category), ("@path", executablePath), ("@banner", bannerUrl), ("@description", Db(description)), ("@active", active));
        return OperationResult<object>.Ok(new GameDto(Id("g", id), code, name, category, executablePath, bannerUrl, description ?? "", active));
    }

    private async Task<OperationResult<object>> CreateCustomerAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsOperator) return Forbidden();
        var username = Username(Value(body, "username")); var fullName = Required(body, "fullName"); var phone = Required(body, "phone");
        var email = Value(body, "email"); var birth = Date(body, "dateOfBirth"); var tier = "Member"; var password = Value(body, "password") ?? "Demo@123"; var citizenId = Value(body, "citizenId");
        if (username is null || fullName is null || phone is null || birth is null || password.Length < 8) return Validation("Thông tin khách hàng hoặc mật khẩu không hợp lệ.");
        await using var c = await connections.OpenAsync(ct); await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct);
        try
        {
            if (await UsernameTakenAsync(c, tx, username, ct)) { await tx.RollbackAsync(ct); return OperationResult<object>.Fail("USERNAME_EXISTS", "Tên đăng nhập đã được sử dụng.", StatusCodes.Status409Conflict); }
            var tierId = await TierIdAsync(c, tx, tier, ct); if (tierId is null) { await tx.RollbackAsync(ct); return Validation("Hạng thành viên không tồn tại."); }
            var hash = passwords.HashPassword(new object(), password);
            var customerId = await ScalarIntAsync(c, "INSERT INTO dbo.Customers (Username,Password_Hash,Full_Name,Phone_Number,Email,Date_Of_Birth,Hobbies,Citizen_ID,Tier_ID,Status) VALUES (@u,@h,@n,@p,@e,@d,@hb,@cid,@tier,'Active'); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, tx, ("@u", username), ("@h", hash), ("@n", fullName), ("@p", phone), ("@e", Db(email)), ("@d", birth.Value), ("@hb", Db(Value(body, "hobbies"))), ("@cid", Db(citizenId)), ("@tier", tierId.Value));
            await ExecuteNonQueryAsync(c, "INSERT INTO dbo.App_Accounts (Username,Password_Hash,Role,Customer_ID,Full_Name,Status) VALUES (@u,@h,'Customer',@id,@n,'Active')", ct, tx, ("@u", username), ("@h", hash), ("@id", customerId), ("@n", fullName));
            await tx.CommitAsync(ct);
            return OperationResult<object>.Ok(new { id = Id("c", customerId), username, fullName, phone, email = email ?? "", dateOfBirth = birth.Value.ToString("yyyy-MM-dd"), hobbies = Value(body, "hobbies") ?? "", citizenId = citizenId ?? "", tier, balance = 0m, status = "Active" });
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private async Task<OperationResult<object>> CreateCategoryAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var name = Required(body, "name"); if (name is null) return Validation("Tên danh mục là bắt buộc.");
        await using var c = await connections.OpenAsync(ct); var id = await ScalarIntAsync(c, "INSERT INTO dbo.Product_Categories (Category_Name) VALUES (@name); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, ("@name", name));
        return OperationResult<object>.Ok(new { id = Id("cat", id), name });
    }

    private async Task<OperationResult<object>> CreateProductAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var name = Required(body, "name"); var categoryId = ParseId(Value(body, "categoryId"), "cat"); var price = Decimal(body, "price"); var stock = Int(body, "stock") ?? 0;
        if (name is null || categoryId is null || price is null || price < 0 || stock is < 0 or > 100000) return Validation("Thông tin món không hợp lệ.");
        await using var c = await connections.OpenAsync(ct); var id = await ScalarIntAsync(c, "INSERT INTO dbo.Products (Category_ID,Product_Name,Price,Stock_Quantity,Status,Image_Url) VALUES (@cat,@name,@price,@stock,@status,@image); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, ("@cat", categoryId.Value), ("@name", name), ("@price", price.Value), ("@stock", stock), ("@status", Bool(body, "active") is false ? "Discontinued" : "Active"), ("@image", Db(Value(body, "imageUrl"))));
        return OperationResult<object>.Ok(new { id = Id("p", id), name, categoryId = Id("cat", categoryId.Value), price = price.Value, stock, active = Bool(body, "active") is not false, imageUrl = Value(body, "imageUrl") ?? "" });
    }

    private async Task<OperationResult<object>> CreateSupplierAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var name = Required(body, "name"); if (name is null) return Validation("Tên nhà cung cấp là bắt buộc.");
        await using var c = await connections.OpenAsync(ct); var id = await ScalarIntAsync(c, "INSERT INTO dbo.Suppliers (Name,Phone,Email,Address,Status) VALUES (@name,@phone,@email,@address,@status); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, ("@name", name), ("@phone", Db(Value(body, "phone"))), ("@email", Db(Value(body, "email"))), ("@address", Db(Value(body, "address"))), ("@status", Bool(body, "active") is false ? "Inactive" : "Active"));
        return OperationResult<object>.Ok(new { id = Id("supplier", id), name, phone = Value(body, "phone") ?? "", email = Value(body, "email") ?? "", address = Value(body, "address") ?? "", active = Bool(body, "active") is not false });
    }

    private async Task<OperationResult<object>> AppendInventoryAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement || actor.EmployeeId is null) return actor.IsManagement ? OperationResult<object>.Fail("EMPLOYEE_REQUIRED", "Tài khoản quản lý cần liên kết hồ sơ nhân viên.", StatusCodes.Status409Conflict) : Forbidden();
        var productId = ParseId(Value(body, "productId"), "p"); var supplierId = Has(body, "supplierId") ? ParseId(Value(body, "supplierId"), "supplier") : null; var type = Value(body, "type"); var quantity = Int(body, "quantity");
        if (productId is null || quantity is null || quantity <= 0 || type is not ("Import" or "Export")) return Validation("Phiếu kho không hợp lệ.");
        if (type == "Import" && supplierId is null) return Validation("Phiếu nhập kho bắt buộc phải chọn nhà cung cấp.");
        if (type == "Export" && string.IsNullOrWhiteSpace(Value(body, "note"))) return Validation("Phiếu xuất kho bắt buộc phải có ghi chú lý do xuất hàng (hao hụt, hết hạn hoặc xuất dùng).");
        await using var c = await connections.OpenAsync(ct); await using var tx = (SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var stock = await ScalarIntOrMissingAsync(c, "SELECT Stock_Quantity FROM dbo.Products WITH (UPDLOCK,HOLDLOCK) WHERE Product_ID=@id", ct, tx, ("@id", productId.Value));
            if (stock < 0) { await tx.RollbackAsync(ct); return NotFound("Không tìm thấy sản phẩm."); }
            if (type == "Export" && stock < quantity) { await tx.RollbackAsync(ct); return OperationResult<object>.Fail("OUT_OF_STOCK", "Tồn kho không đủ để xuất.", StatusCodes.Status409Conflict); }
            if (supplierId is not null && await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Suppliers WHERE Supplier_ID=@id AND Status='Active'", ct, tx, ("@id", supplierId.Value)) == 0) { await tx.RollbackAsync(ct); return Validation("Nhà cung cấp không tồn tại hoặc đã ngừng hoạt động."); }
            var id = await ScalarIntAsync(c, "INSERT INTO dbo.Inventory_Transactions (Product_ID,Employee_ID,Supplier_ID,Trans_Type,Quantity,Note) VALUES (@product,@employee,@supplier,@type,@quantity,@note); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, tx, ("@product", productId.Value), ("@employee", actor.EmployeeId.Value), ("@supplier", Db(supplierId)), ("@type", type), ("@quantity", quantity.Value), ("@note", Db(Value(body, "note"))));
            await tx.CommitAsync(ct); return OperationResult<object>.Ok(new { id = Id("inv", id), productId = Id("p", productId.Value), supplierId = supplierId is null ? null : Id("supplier", supplierId.Value), type, quantity = quantity.Value, note = Value(body, "note") ?? "", createdAt = DateTime.UtcNow });
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private async Task<OperationResult<object>> CreateFeedbackAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (actor.CustomerId is null) return OperationResult<object>.Fail("FORBIDDEN", "Chỉ khách hàng có thể gửi phản hồi.", StatusCodes.Status403Forbidden);
        var subject = Required(body, "subject"); var content = Required(body, "content"); if (subject is null || content is null) return Validation("Chủ đề và nội dung là bắt buộc.");
        await using var c = await connections.OpenAsync(ct); var id = await ScalarIntAsync(c, "INSERT INTO dbo.Feedback (Customer_ID,Subject,Content,Status) VALUES (@customer,@subject,@content,'Pending'); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, ("@customer", actor.CustomerId.Value), ("@subject", subject), ("@content", content));
        return OperationResult<object>.Ok(new { id = Id("feedback", id), customerId = Id("c", actor.CustomerId.Value), subject, content, status = "Pending", response = "", createdAt = DateTime.UtcNow });
    }

    private async Task<OperationResult<object>> CreateSurveyAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var title = Required(body, "title"); if (title is null || !Array(body, "questions").Any()) return Validation("Khảo sát cần tiêu đề và ít nhất một câu hỏi.");
        await using var c = await connections.OpenAsync(ct); await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct);
        try
        {
            var id = await ScalarIntAsync(c, "INSERT INTO dbo.Surveys (Title,Description,Created_By,Is_Active,Lifecycle_Status) VALUES (@title,@description,@by,0,'Draft'); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, tx, ("@title", title), ("@description", Db(Value(body, "description"))), ("@by", Db(actor.EmployeeId)));
            await ReplaceQuestionsAsync(c, tx, id, Array(body, "questions"), ct); await tx.CommitAsync(ct);
            return OperationResult<object>.Ok(new { id = Id("survey", id), title, description = Value(body, "description") ?? "", status = "Draft", questions = Array(body, "questions").Select((q, i) => new { id = $"q-{i + 1:000}", text = Value(q, "text") ?? "", options = StringArray(q, "options") }).ToArray(), customerIds = System.Array.Empty<string>(), responses = System.Array.Empty<object>() });
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private async Task<OperationResult<object>> CreateEmployeeAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var fullName = Required(body, "fullName"); var department = Required(body, "department"); var position = Required(body, "position"); var joinedAt = Date(body, "joinedAt"); var salary = Decimal(body, "baseSalary");
        var username = Username(Value(body, "username")); var password = Value(body, "password") ?? "Demo@123";
        if (fullName is null || department is null || position is null || joinedAt is null || salary is null || salary < 0) return Validation("Thông tin nhân viên không hợp lệ.");
        if (username is null || password.Length < 8) return Validation("Tên đăng nhập hoặc mật khẩu không hợp lệ (tối thiểu 8 ký tự).");
        await using var c = await connections.OpenAsync(ct); await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct);
        try
        {
            if (await UsernameTakenAsync(c, tx, username, ct)) { await tx.RollbackAsync(ct); return OperationResult<object>.Fail("USERNAME_EXISTS", "Tên đăng nhập đã được sử dụng.", StatusCodes.Status409Conflict); }
            var departmentId = await EnsureDepartmentAsync(c, tx, department, ct); var positionId = await EnsurePositionAsync(c, tx, position, departmentId, ct);
            var role = position.Contains("Quản lý") || position.Contains("Trưởng nhóm") ? Roles.Manager : position.Contains("Thu ngân") ? Roles.Cashier : Roles.Staff;
            var employeeStatus = Value(body, "status") == "Inactive" ? "Suspended" : "Active";
            var hash = passwords.HashPassword(new object(), password);
            var birth = Date(body, "dateOfBirth");
            var id = await ScalarIntAsync(c, "INSERT INTO dbo.Employees (Username,Password_Hash,Full_Name,Phone_Number,Email,Position_ID,Hire_Date,Base_Salary,Status,Qualification,Date_Of_Birth) VALUES (@username,@hash,@name,@phone,@email,@position,@joined,@salary,@status,@qualification,@birth); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, tx, ("@username", username), ("@hash", hash), ("@name", fullName), ("@phone", Db(Value(body, "phone"))), ("@email", Db(Value(body, "email"))), ("@position", positionId), ("@joined", joinedAt.Value), ("@salary", salary.Value), ("@status", employeeStatus), ("@qualification", Db(Value(body, "qualification"))), ("@birth", (object?)birth ?? DBNull.Value));
            await ExecuteNonQueryAsync(c, "INSERT INTO dbo.App_Accounts (Username,Password_Hash,Role,Employee_ID,Full_Name,Status) VALUES (@u,@h,@role,@empId,@name,'Active')", ct, tx, ("@u", username), ("@h", hash), ("@role", role), ("@empId", id), ("@name", fullName));
            await tx.CommitAsync(ct); return OperationResult<object>.Ok(EmployeeObject(id, fullName, department, position, body, salary.Value, joinedAt.Value));
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private async Task<OperationResult<object>> CreateShiftAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var name = Required(body, "name"); var start = Time(body, "startTime"); var end = Time(body, "endTime"); if (name is null || start is null || end is null || start == end) return Validation("Ca làm không hợp lệ.");
        await using var c = await connections.OpenAsync(ct); var id = await ScalarIntAsync(c, "INSERT INTO dbo.Work_Shifts (Shift_Name,Start_Time,End_Time) VALUES (@name,@start,@end); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, ("@name", name), ("@start", start.Value), ("@end", end.Value));
        return OperationResult<object>.Ok(new { id = Id("shift", id), name, startTime = start.Value.ToString("hh\\:mm"), endTime = end.Value.ToString("hh\\:mm") });
    }

    private async Task<OperationResult<object>> CreateScheduleAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var employee = ParseId(Value(body, "employeeId"), "e"); var shift = ParseId(Value(body, "shiftId"), "shift"); var date = Date(body, "date"); var status = Value(body, "status") ?? "Scheduled";
        if (employee is null || shift is null || date is null || !ScheduleStatuses.Contains(status)) return Validation("Lịch làm không hợp lệ.");
        await using var c = await connections.OpenAsync(ct);
        var conflict = await CheckScheduleConflictsAsync(c, null, employee.Value, date.Value, shift.Value, null, ct);
        if (conflict is not null) return conflict;
        var id = await ScalarIntAsync(c, "INSERT INTO dbo.Work_Schedules (Employee_ID,Shift_ID,Work_Date,Status) VALUES (@employee,@shift,@date,@status); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, ("@employee", employee.Value), ("@shift", shift.Value), ("@date", date.Value), ("@status", status));
        return OperationResult<object>.Ok(new { id = Id("schedule", id), employeeId = Id("e", employee.Value), shiftId = Id("shift", shift.Value), date = date.Value.ToString("yyyy-MM-dd"), status });
    }

    private async Task<OperationResult<object>> CreateLeaveAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        var employee = ParseId(Value(body, "employeeId"), "e") ?? actor.EmployeeId; var type = Value(body, "type"); var start = Date(body, "startDate"); var end = Date(body, "endDate"); var reason = Required(body, "reason");
        if (employee is null || type is not ("Annual" or "Sick" or "Maternity" or "Resignation") || start is null || end is null || end < start || reason is null) return Validation("Đơn nghỉ không hợp lệ.");
        if (!actor.IsManagement && actor.EmployeeId != employee) return Forbidden();
        await using var c = await connections.OpenAsync(ct);
        if (await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Leave_Requests WHERE Employee_ID=@e AND Status IN ('Pending','Approved') AND NOT (End_Date < @start OR Start_Date > @end)", ct, ("@e", employee.Value), ("@start", start.Value), ("@end", end.Value)) > 0)
            return OperationResult<object>.Fail("LEAVE_OVERLAP", "Bạn đã có đơn nghỉ phép khác đang chờ duyệt hoặc đã duyệt trong khoảng thời gian này.", StatusCodes.Status409Conflict);
        var id = await ScalarIntAsync(c, "INSERT INTO dbo.Leave_Requests (Employee_ID,Leave_Type,Start_Date,End_Date,Reason,Status) VALUES (@employee,@type,@start,@end,@reason,'Pending'); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, ("@employee", employee.Value), ("@type", type == "Resignation" ? "Unpaid" : type), ("@start", start.Value), ("@end", end.Value), ("@reason", reason));
        return OperationResult<object>.Ok(new { id = Id("leave", id), employeeId = Id("e", employee.Value), type, startDate = start.Value.ToString("yyyy-MM-dd"), endDate = end.Value.ToString("yyyy-MM-dd"), reason, status = "Pending" });
    }

    private async Task<OperationResult<object>> CreatePayrollAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var employee = ParseId(Value(body, "employeeId"), "e"); var month = Month(body, "month");
        if (employee is null || month is null) return Validation("Phiếu lương không hợp lệ.");
        await using var c = await connections.OpenAsync(ct);
        if (await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Payroll WHERE Employee_ID=@e AND Pay_Month=@m AND Pay_Year=@y", ct, ("@e", employee.Value), ("@m", month.Value.Month), ("@y", month.Value.Year)) > 0)
            return OperationResult<object>.Fail("PAYROLL_EXISTS", $"Bảng lương nhân viên trong tháng {month.Value:yyyy-MM} đã tồn tại.", StatusCodes.Status409Conflict);
        await using var salaryCommand = new SqlCommand("SELECT Base_Salary FROM dbo.Employees WHERE Employee_ID=@id", c);
        salaryCommand.Parameters.AddWithValue("@id", employee.Value);
        var salaryValue = await salaryCommand.ExecuteScalarAsync(ct);
        if (salaryValue is null || salaryValue == DBNull.Value) return NotFound("Không tìm thấy nhân viên.");
        var inputBase = Decimal(body, "baseSalary"); var baseSalary = inputBase is null or <= 0 ? Convert.ToDecimal(salaryValue) : inputBase.Value;
        var bonus = Decimal(body, "bonus") ?? 0; var manualDeduction = Decimal(body, "deduction") ?? 0;
        if (bonus < 0 || manualDeduction < 0) return Validation("Thưởng và khấu trừ không được âm.");
        var shiftRate = Math.Round(baseSalary / 26m, 0);
        var totalScheduled = await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Work_Schedules WHERE Employee_ID=@e AND YEAR(Work_Date)=@y AND MONTH(Work_Date)=@m", ct, ("@e", employee.Value), ("@y", month.Value.Year), ("@m", month.Value.Month));
        var attendedShifts = await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Attendance a JOIN dbo.Work_Schedules s ON s.Schedule_ID=a.Schedule_ID WHERE s.Employee_ID=@e AND YEAR(s.Work_Date)=@y AND MONTH(s.Work_Date)=@m AND a.Check_In_Time IS NOT NULL", ct, ("@e", employee.Value), ("@y", month.Value.Year), ("@m", month.Value.Month));
        var paidLeaveShifts = await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Work_Schedules s WHERE s.Employee_ID=@e AND YEAR(s.Work_Date)=@y AND MONTH(s.Work_Date)=@m AND s.Status='OnLeave' AND EXISTS (SELECT 1 FROM dbo.Leave_Requests l WHERE l.Employee_ID=s.Employee_ID AND l.Status='Approved' AND l.Leave_Type='Annual' AND s.Work_Date BETWEEN l.Start_Date AND l.End_Date)", ct, ("@e", employee.Value), ("@y", month.Value.Year), ("@m", month.Value.Month));
        var unpaidShifts = Math.Max(0, totalScheduled - attendedShifts - paidLeaveShifts);
        var autoDeduction = unpaidShifts * shiftRate;
        if (attendedShifts == 0 && paidLeaveShifts == 0 && totalScheduled > 0) autoDeduction = baseSalary;
        var deduction = manualDeduction > 0 ? Math.Max(manualDeduction, autoDeduction) : autoDeduction;
        var id = await ScalarIntAsync(c, "INSERT INTO dbo.Payroll (Employee_ID,Pay_Month,Pay_Year,Base_Salary,Bonus,Deduction,Status,Workflow_Status) VALUES (@employee,@month,@year,@base,@bonus,@deduction,'Unpaid','Draft'); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, ("@employee", employee.Value), ("@month", month.Value.Month), ("@year", month.Value.Year), ("@base", baseSalary), ("@bonus", bonus), ("@deduction", deduction));
        return OperationResult<object>.Ok(PayrollObject(id, employee.Value, month.Value, baseSalary, bonus, deduction, "Draft"));
    }

    private async Task<OperationResult<object>> CreateAccountAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (actor.Role != Roles.Admin) return Forbidden(); var username = Username(Value(body, "username")); var fullName = Required(body, "fullName"); var role = Value(body, "role"); var employee = ParseId(Value(body, "employeeId"), "e"); var password = Value(body, "password") ?? "Demo@123";
        if (username is null || fullName is null || role is not (Roles.Admin or Roles.Owner or Roles.Manager or Roles.Cashier or Roles.Staff) || password.Length < 8 || (role == Roles.Staff && employee is null)) return Validation("Tài khoản không hợp lệ.");
        await using var c = await connections.OpenAsync(ct); if (await UsernameTakenAsync(c, null, username, ct)) return OperationResult<object>.Fail("USERNAME_EXISTS", "Tên đăng nhập đã được sử dụng.", StatusCodes.Status409Conflict);
        var id = await ScalarIntAsync(c, "INSERT INTO dbo.App_Accounts (Username,Password_Hash,Role,Employee_ID,Full_Name,Status) VALUES (@username,@hash,@role,@employee,@name,@status); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, ("@username", username), ("@hash", passwords.HashPassword(new object(), password)), ("@role", role), ("@employee", Db(employee)), ("@name", fullName), ("@status", Value(body, "status") == "Banned" ? "Banned" : "Active"));
        return OperationResult<object>.Ok(new { id = Id("a", id), username, fullName, role, employeeId = employee is null ? null : Id("e", employee.Value), status = Value(body, "status") == "Banned" ? "Banned" : "Active" });
    }

    private async Task<OperationResult<object>> UpdateComputerAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var key = ParseId(id, "pc"); if (key is null) return NotFound(); await using var c = await connections.OpenAsync(ct);
        var changes = new List<string>(); var parameters = new List<(string, object)> { ("@id", key.Value) }; Add(changes, parameters, "Computer_Code", "@name", Value(body, "name")); Add(changes, parameters, "Zone_Type", "@zone", Value(body, "zone")); Add(changes, parameters, "Hourly_Rate", "@rate", Decimal(body, "hourlyRate"));
        return await UpdatedAsync(c, "dbo.Computers", changes, parameters, ct, () => new { id, name = Value(body, "name"), zone = Value(body, "zone"), hourlyRate = Decimal(body, "hourlyRate") });
    }

    private async Task<OperationResult<object>> UpdateGameAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden();
        var key = ParseId(id, "g"); if (key is null) return NotFound();
        if (!new[] { "name", "category", "executablePath", "bannerUrl", "description", "active" }.Any(field => Has(body, field)))
            return Validation("Không có dữ liệu game để cập nhật.");
        await using var c = await connections.OpenAsync(ct);
        var affected = await ExecuteNonQueryAsync(c, """
            UPDATE dbo.Games
            SET Name=COALESCE(@name,Name), Category=COALESCE(@category,Category),
                Executable_Path=COALESCE(@path,Executable_Path), Banner_Url=COALESCE(@banner,Banner_Url),
                Description=CASE WHEN @hasDescription=1 THEN @description ELSE Description END,
                Is_Active=COALESCE(@active,Is_Active)
            WHERE Game_ID=@id
            """, ct,
            ("@name", Db(Has(body, "name") ? Required(body, "name") : null)),
            ("@category", Db(Has(body, "category") ? Required(body, "category") : null)),
            ("@path", Db(Has(body, "executablePath") ? Required(body, "executablePath") : null)),
            ("@banner", Db(Has(body, "bannerUrl") ? Required(body, "bannerUrl") : null)),
            ("@hasDescription", Has(body, "description") ? 1 : 0),
            ("@description", Db(Value(body, "description"))),
            ("@active", Db(Bool(body, "active"))),
            ("@id", key.Value));
        return affected == 0 ? NotFound("Không tìm thấy game.") : OperationResult<object>.Ok(new { id });
    }

    private async Task<OperationResult<object>> UpdateCustomerAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        var key = ParseId(id, "c"); if (key is null) return NotFound(); if (!actor.IsOperator && actor.CustomerId != key) return Forbidden(); if (!actor.IsManagement && (Has(body, "tier") || Has(body, "status"))) return Forbidden();
        await using var c = await connections.OpenAsync(ct); var changes = new List<string>(); var p = new List<(string, object)> { ("@id", key.Value) }; Add(changes, p, "Full_Name", "@name", Value(body, "fullName")); Add(changes, p, "Phone_Number", "@phone", Value(body, "phone")); Add(changes, p, "Email", "@email", Has(body, "email") ? Db(Value(body, "email")) : null); Add(changes, p, "Date_Of_Birth", "@birth", Date(body, "dateOfBirth")); Add(changes, p, "Hobbies", "@hobbies", Value(body, "hobbies")); Add(changes, p, "Citizen_ID", "@cid", Has(body, "citizenId") ? Db(Value(body, "citizenId")) : null); Add(changes, p, "Status", "@status", Value(body, "status"));
        if (Has(body, "tier")) { var tierId = await TierIdAsync(c, null, Value(body, "tier")!, ct); if (tierId is null) return Validation("Hạng thành viên không tồn tại."); Add(changes, p, "Tier_ID", "@tier", tierId.Value); }
        return await UpdatedAsync(c, "dbo.Customers", changes, p, ct, () => new { id });
    }

    private async Task<OperationResult<object>> UpdateCategoryAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct) => await UpdateSimpleAsync(actor, id, body, ct, "cat", "dbo.Product_Categories", "Category_Name", "name");

    private async Task<OperationResult<object>> UpdateProductAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var key = ParseId(id, "p"); if (key is null) return NotFound(); await using var c = await connections.OpenAsync(ct); var changes = new List<string>(); var p = new List<(string, object)> { ("@id", key.Value) };
        Add(changes, p, "Product_Name", "@name", Value(body, "name")); Add(changes, p, "Price", "@price", Decimal(body, "price")); Add(changes, p, "Image_Url", "@image", Has(body, "imageUrl") ? Db(Value(body, "imageUrl")) : null); if (Has(body, "active")) Add(changes, p, "Status", "@status", Bool(body, "active") is true ? "Active" : "Discontinued");
        if (Has(body, "categoryId")) { var category = ParseId(Value(body, "categoryId"), "cat"); if (category is null) return Validation("Danh mục không hợp lệ."); Add(changes, p, "Category_ID", "@category", category.Value); }
        return await UpdatedAsync(c, "dbo.Products", changes, p, ct, () => new { id });
    }

    private async Task<OperationResult<object>> UpdateSupplierAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var key = ParseId(id, "supplier"); if (key is null) return NotFound(); await using var c = await connections.OpenAsync(ct); var changes = new List<string>(); var p = new List<(string, object)> { ("@id", key.Value) };
        Add(changes, p, "Name", "@name", Value(body, "name")); Add(changes, p, "Phone", "@phone", Value(body, "phone")); Add(changes, p, "Email", "@email", Value(body, "email")); Add(changes, p, "Address", "@address", Value(body, "address")); if (Has(body, "active")) Add(changes, p, "Status", "@status", Bool(body, "active") is true ? "Active" : "Inactive");
        if (changes.Count > 0) changes.Add("Updated_At = SYSUTCDATETIME()"); return await UpdatedAsync(c, "dbo.Suppliers", changes, p, ct, () => new { id });
    }

    private async Task<OperationResult<object>> UpdateFeedbackAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var key = ParseId(id, "feedback"); if (key is null) return NotFound(); var status = Value(body, "status"); if (status is not null && status is not ("Pending" or "Processing" or "Resolved")) return Validation("Trạng thái phản hồi không hợp lệ.");
        await using var c = await connections.OpenAsync(ct); var changes = new List<string>(); var p = new List<(string, object)> { ("@id", key.Value) }; Add(changes, p, "Status", "@status", status); Add(changes, p, "Manager_Notes", "@notes", Value(body, "response")); if (changes.Count > 0) { changes.Add("Handled_By=@employee"); p.Add(("@employee", Db(actor.EmployeeId))); }
        return await UpdatedAsync(c, "dbo.Feedback", changes, p, ct, () => new { id, status, response = Value(body, "response") ?? "" });
    }

    private async Task<OperationResult<object>> UpdateSurveyAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var key = ParseId(id, "survey"); if (key is null) return NotFound(); await using var c = await connections.OpenAsync(ct); await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct);
        try
        {
            var lifecycle = await StringScalarAsync(c, "SELECT Lifecycle_Status FROM dbo.Surveys WITH (UPDLOCK,HOLDLOCK) WHERE Survey_ID=@id", ct, tx, ("@id", key.Value)); if (lifecycle is null) { await tx.RollbackAsync(ct); return NotFound(); }
            if (lifecycle != "Draft" && (Has(body, "title") || Has(body, "description") || Has(body, "questions"))) { await tx.RollbackAsync(ct); return OperationResult<object>.Fail("IMMUTABLE", "Không thể sửa nội dung khảo sát đã phát hành hoặc đã đóng.", StatusCodes.Status409Conflict); }
            var requestedStatus = Value(body, "status"); if (requestedStatus is not null && requestedStatus != "Closed") { await tx.RollbackAsync(ct); return Validation("Khảo sát chỉ có thể đóng qua cập nhật; phát hành dùng hành động publish."); }
            if (lifecycle == "Closed" && requestedStatus is not null) { await tx.RollbackAsync(ct); return OperationResult<object>.Fail("SURVEY_CLOSED", "Khảo sát đã đóng.", StatusCodes.Status409Conflict); }
            var changes = new List<string>(); var p = new List<(string, object)> { ("@id", key.Value) }; Add(changes, p, "Title", "@title", Value(body, "title")); Add(changes, p, "Description", "@description", Value(body, "description"));
            if (requestedStatus == "Closed") { changes.Add("Is_Active=0"); changes.Add("Lifecycle_Status='Closed'"); }
            if (changes.Count > 0) await ExecuteNonQueryAsync(c, $"UPDATE dbo.Surveys SET {string.Join(',', changes)} WHERE Survey_ID=@id", ct, tx, p.ToArray());
            if (Has(body, "questions")) await ReplaceQuestionsAsync(c, tx, key.Value, Array(body, "questions"), ct);
            await tx.CommitAsync(ct); return OperationResult<object>.Ok(new { id, title = Value(body, "title"), description = Value(body, "description"), status = requestedStatus ?? lifecycle });
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private async Task<OperationResult<object>> UpdateEmployeeAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        var key = ParseId(id, "e"); if (key is null) return NotFound(); if (!actor.IsManagement && actor.EmployeeId != key) return Forbidden();
        if (!actor.IsManagement && (Has(body, "department") || Has(body, "position") || Has(body, "baseSalary") || Has(body, "status") || Has(body, "joinedAt"))) return Forbidden();
        await using var c = await connections.OpenAsync(ct); await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct);
        try
        {
            var changes = new List<string>(); var p = new List<(string, object)> { ("@id", key.Value) }; Add(changes, p, "Full_Name", "@name", Value(body, "fullName")); Add(changes, p, "Phone_Number", "@phone", Value(body, "phone")); Add(changes, p, "Email", "@email", Value(body, "email")); Add(changes, p, "Qualification", "@qualification", Value(body, "qualification")); Add(changes, p, "Base_Salary", "@salary", Decimal(body, "baseSalary")); Add(changes, p, "Hire_Date", "@joined", Date(body, "joinedAt")); if (Has(body, "dateOfBirth")) Add(changes, p, "Date_Of_Birth", "@birth", Date(body, "dateOfBirth")); if (Has(body, "status")) Add(changes, p, "Status", "@status", Value(body, "status") == "Inactive" ? "Suspended" : Value(body, "status"));
            if (Has(body, "department") || Has(body, "position")) { var department = Value(body, "department") ?? await StringScalarAsync(c, "SELECT d.Department_Name FROM dbo.Employees e JOIN dbo.Positions p ON p.Position_ID=e.Position_ID JOIN dbo.Departments d ON d.Department_ID=p.Department_ID WHERE e.Employee_ID=@id", ct, tx, ("@id", key.Value)); var position = Value(body, "position") ?? await StringScalarAsync(c, "SELECT p.Position_Name FROM dbo.Employees e JOIN dbo.Positions p ON p.Position_ID=e.Position_ID WHERE e.Employee_ID=@id", ct, tx, ("@id", key.Value)); if (department is null || position is null) { await tx.RollbackAsync(ct); return NotFound(); } var departmentId = await EnsureDepartmentAsync(c, tx, department, ct); var positionId = await EnsurePositionAsync(c, tx, position, departmentId, ct); Add(changes, p, "Position_ID", "@position", positionId); var updatedRole = position.Contains("Quản lý") || position.Contains("Trưởng nhóm") ? Roles.Manager : position.Contains("Thu ngân") ? Roles.Cashier : Roles.Staff; await ExecuteNonQueryAsync(c, "UPDATE dbo.App_Accounts SET Role = @role WHERE Employee_ID = @id", ct, tx, ("@role", updatedRole), ("@id", key.Value)); }
            if (changes.Count == 0) { await tx.RollbackAsync(ct); return Validation("Không có dữ liệu để cập nhật."); }
            var n = await ExecuteNonQueryAsync(c, $"UPDATE dbo.Employees SET {string.Join(',', changes)} WHERE Employee_ID=@id", ct, tx, p.ToArray()); if (n == 0) { await tx.RollbackAsync(ct); return NotFound(); }
            if (Has(body, "fullName")) { await ExecuteNonQueryAsync(c, "UPDATE dbo.App_Accounts SET Full_Name = @name WHERE Employee_ID = @id", ct, tx, ("@name", Value(body, "fullName")!), ("@id", key.Value)); }
            await tx.CommitAsync(ct); return OperationResult<object>.Ok(new { id });
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private async Task<OperationResult<object>> UpdateShiftAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var key = ParseId(id, "shift"); if (key is null) return NotFound(); await using var c = await connections.OpenAsync(ct); var changes = new List<string>(); var p = new List<(string, object)> { ("@id", key.Value) }; Add(changes, p, "Shift_Name", "@name", Value(body, "name")); Add(changes, p, "Start_Time", "@start", Time(body, "startTime")); Add(changes, p, "End_Time", "@end", Time(body, "endTime")); return await UpdatedAsync(c, "dbo.Work_Shifts", changes, p, ct, () => new { id });
    }

    private async Task<OperationResult<object>> UpdateScheduleAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var key = ParseId(id, "schedule"); if (key is null) return NotFound();
        await using var c = await connections.OpenAsync(ct);
        await using var find = new SqlCommand("SELECT Employee_ID, Shift_ID, Work_Date FROM dbo.Work_Schedules WHERE Schedule_ID=@id", c);
        find.Parameters.AddWithValue("@id", key.Value);
        await using var reader = await find.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return NotFound();
        var (currentEmployee, currentShift, currentDate) = (reader.GetInt32(0), reader.GetInt32(1), DateOnly.FromDateTime(reader.GetDateTime(2)));
        await reader.CloseAsync();
        int employee = currentEmployee, shift = currentShift; var date = currentDate;
        if (Has(body, "employeeId")) { employee = ParseId(Value(body, "employeeId"), "e") ?? -1; if (employee < 0) return Validation("Nhân viên không hợp lệ."); }
        if (Has(body, "shiftId")) { shift = ParseId(Value(body, "shiftId"), "shift") ?? -1; if (shift < 0) return Validation("Ca làm không hợp lệ."); }
        if (Has(body, "date")) { var parsed = Date(body, "date"); if (parsed is null) return Validation("Ngày làm không hợp lệ."); date = parsed.Value; }
        var status = Value(body, "status"); if (status is not null && !ScheduleStatuses.Contains(status)) return Validation("Trạng thái lịch làm không hợp lệ.");
        if (Has(body, "employeeId") || Has(body, "shiftId") || Has(body, "date"))
        {
            await using var history = new SqlCommand("SELECT COUNT(*) FROM dbo.Attendance WHERE Schedule_ID=@id", c);
            history.Parameters.AddWithValue("@id", key.Value);
            if (Convert.ToInt32(await history.ExecuteScalarAsync(ct)) > 0)
                return OperationResult<object>.Fail("HAS_HISTORY", "Lịch làm việc đã có dữ liệu chấm công, không được đổi nhân viên, ca làm hoặc ngày làm.", StatusCodes.Status409Conflict);
            var conflict = await CheckScheduleConflictsAsync(c, null, employee, date, shift, key.Value, ct);
            if (conflict is not null) return conflict;
        }
        var changes = new List<string>(); var p = new List<(string, object)> { ("@id", key.Value) };
        if (Has(body, "employeeId")) Add(changes, p, "Employee_ID", "@employee", employee);
        if (Has(body, "shiftId")) Add(changes, p, "Shift_ID", "@shift", shift);
        Add(changes, p, "Work_Date", "@date", Date(body, "date")); Add(changes, p, "Status", "@status", status);
        return await UpdatedAsync(c, "dbo.Work_Schedules", changes, p, ct, () => new { id });
    }

    private async Task<OperationResult<object>> UpdateLeaveAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        var key = ParseId(id, "leave"); if (key is null) return NotFound(); await using var c = await connections.OpenAsync(ct); var employee = await ScalarIntOrMissingAsync(c, "SELECT Employee_ID FROM dbo.Leave_Requests WHERE Leave_ID=@id", ct, null, ("@id", key.Value)); if (employee < 0) return NotFound(); if (!actor.IsManagement && actor.EmployeeId != employee) return Forbidden();
        var changes = new List<string>(); var p = new List<(string, object)> { ("@id", key.Value) }; if (Has(body, "type")) Add(changes, p, "Leave_Type", "@type", Value(body, "type") == "Resignation" ? "Unpaid" : Value(body, "type")); Add(changes, p, "Start_Date", "@start", Date(body, "startDate")); Add(changes, p, "End_Date", "@end", Date(body, "endDate")); Add(changes, p, "Reason", "@reason", Value(body, "reason")); return await UpdatedAsync(c, "dbo.Leave_Requests", changes, p, ct, () => new { id });
    }

    private async Task<OperationResult<object>> UpdatePayrollAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var key = ParseId(id, "payroll"); if (key is null) return NotFound(); await using var c = await connections.OpenAsync(ct); await using var tx = (SqlTransaction)await c.BeginTransactionAsync(ct);
        try
        {
            await using var state = new SqlCommand("SELECT Workflow_Status FROM dbo.Payroll WITH (UPDLOCK,HOLDLOCK) WHERE Payroll_ID=@id", c, (SqlTransaction)tx); state.Parameters.AddWithValue("@id", key.Value); var dbStatus = (string?)await state.ExecuteScalarAsync(ct); if (dbStatus is null) { await tx.RollbackAsync(ct); return NotFound(); } if (dbStatus == "Paid") { await tx.RollbackAsync(ct); return OperationResult<object>.Fail("IMMUTABLE", "Phiếu lương đã thanh toán không thể sửa.", StatusCodes.Status409Conflict); }
            var requestedStatus = Value(body, "status"); if (requestedStatus is not null && requestedStatus is not ("Draft" or "Approved" or "Paid")) { await tx.RollbackAsync(ct); return Validation("Trạng thái lương không hợp lệ."); }
            var changes = new List<string>(); var p = new List<(string, object)> { ("@id", key.Value) }; if (Has(body, "employeeId")) Add(changes, p, "Employee_ID", "@employee", ParseId(Value(body, "employeeId"), "e")); if (Has(body, "month")) { var month = Month(body, "month"); if (month is null) { await tx.RollbackAsync(ct); return Validation("Tháng lương không hợp lệ."); } Add(changes, p, "Pay_Month", "@month", month.Value.Month); Add(changes, p, "Pay_Year", "@year", month.Value.Year); } Add(changes, p, "Base_Salary", "@base", Decimal(body, "baseSalary")); Add(changes, p, "Bonus", "@bonus", Decimal(body, "bonus")); Add(changes, p, "Deduction", "@deduction", Decimal(body, "deduction")); if (requestedStatus is not null) { Add(changes, p, "Workflow_Status", "@workflow", requestedStatus); Add(changes, p, "Status", "@status", requestedStatus == "Paid" ? "Paid" : "Unpaid"); if (requestedStatus == "Paid") changes.Add("Payment_Date=COALESCE(Payment_Date,SYSUTCDATETIME())"); }
            if (changes.Count == 0) { await tx.RollbackAsync(ct); return Validation("Không có dữ liệu để cập nhật."); } var n = await ExecuteNonQueryAsync(c, $"UPDATE dbo.Payroll SET {string.Join(',', changes)} WHERE Payroll_ID=@id", ct, tx, p.ToArray()); if (n == 0) { await tx.RollbackAsync(ct); return NotFound(); } await tx.CommitAsync(ct); return OperationResult<object>.Ok(new { id });
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private async Task<OperationResult<object>> UpdateAccountAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        if (actor.Role != Roles.Admin) return Forbidden(); var key = ParseId(id, "a"); if (key is null) return NotFound(); if (key == actor.AccountId && ((Has(body, "role") && Value(body, "role") != Roles.Admin) || Value(body, "status") == "Banned")) return OperationResult<object>.Fail("SELF_LOCKOUT", "Không thể tự hạ quyền hoặc khóa tài khoản hiện tại.", StatusCodes.Status409Conflict);
        var role = Value(body, "role"); if (role is not null && role is not (Roles.Admin or Roles.Owner or Roles.Manager or Roles.Cashier or Roles.Staff)) return Validation("Vai trò không hợp lệ."); var employee = Has(body, "employeeId") ? ParseId(Value(body, "employeeId"), "e") : null; if (role == Roles.Staff && employee is null && !Has(body, "employeeId")) return Validation("Staff phải liên kết hồ sơ nhân viên.");
        await using var c = await connections.OpenAsync(ct); var changes = new List<string>(); var p = new List<(string, object)> { ("@id", key.Value) }; Add(changes, p, "Full_Name", "@name", Value(body, "fullName")); Add(changes, p, "Role", "@role", role); if (Has(body, "employeeId")) Add(changes, p, "Employee_ID", "@employee", Db(employee)); Add(changes, p, "Status", "@status", Value(body, "status")); if (changes.Count > 0) changes.Add("Updated_At=SYSUTCDATETIME()"); return await UpdatedAsync(c, "dbo.App_Accounts", changes, p, ct, () => new { id });
    }

    private async Task<OperationResult<object>> DecideLeaveCoreAsync(IdentityContext actor, string id, DecisionRequest request, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var key = ParseId(id, "leave"); if (key is null) return NotFound(); if (request.Status is not ("Approved" or "Rejected")) return Validation("Trạng thái phải là Approved hoặc Rejected.");
        await using var c = await connections.OpenAsync(ct); await using var tx = (SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await using var find = new SqlCommand("SELECT Employee_ID, Start_Date, End_Date, Status FROM dbo.Leave_Requests WITH (UPDLOCK,HOLDLOCK) WHERE Leave_ID=@id", c, tx);
            find.Parameters.AddWithValue("@id", key.Value);
            await using var reader = await find.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) { await tx.RollbackAsync(ct); return NotFound(); }
            var (employee, start, end, status) = (reader.GetInt32(0), DateOnly.FromDateTime(reader.GetDateTime(1)), DateOnly.FromDateTime(reader.GetDateTime(2)), reader.GetString(3));
            await reader.CloseAsync();
            if (status != "Pending") { await tx.RollbackAsync(ct); return OperationResult<object>.Fail("ALREADY_PROCESSED", "Đơn nghỉ không tồn tại hoặc đã xử lý.", StatusCodes.Status409Conflict); }
            await ExecuteNonQueryAsync(c, "UPDATE dbo.Leave_Requests SET Status=@status, Approved_By=@by WHERE Leave_ID=@id", ct, tx, ("@status", request.Status), ("@by", Db(actor.EmployeeId)), ("@id", key.Value));
            if (request.Status == "Approved")
                await ExecuteNonQueryAsync(c, "UPDATE dbo.Work_Schedules SET Status='OnLeave' WHERE Employee_ID=@e AND Work_Date>=@start AND Work_Date<=@end AND Status='Scheduled'", ct, tx, ("@e", employee), ("@start", start), ("@end", end));
            await tx.CommitAsync(ct); return OperationResult<object>.Ok(new { id, status = request.Status });
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private async Task<OperationResult<object>> CheckInCoreAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        var scheduleId = ParseId(Value(body, "scheduleId"), "schedule"); if (scheduleId is null) return Validation("Lịch làm không hợp lệ.");
        await using var c = await connections.OpenAsync(ct);
        await using var find = new SqlCommand("""
            SELECT s.Employee_ID, s.Work_Date, s.Status, ws.Start_Time, ws.End_Time
            FROM dbo.Work_Schedules s
            JOIN dbo.Work_Shifts ws ON ws.Shift_ID = s.Shift_ID
            WHERE s.Schedule_ID = @id
        """, c);
        find.Parameters.AddWithValue("@id", scheduleId.Value);
        await using var reader = await find.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return NotFound("Không tìm thấy lịch làm.");
        var (owner, workDate, scheduleStatus, shiftStart, shiftEnd) = (
            reader.GetInt32(0),
            DateOnly.FromDateTime(reader.GetDateTime(1)),
            reader.GetString(2),
            reader.GetTimeSpan(3),
            reader.GetTimeSpan(4));
        await reader.CloseAsync();
        if (!actor.IsManagement && actor.EmployeeId != owner) return Forbidden();
        if (scheduleStatus == "OnLeave") return OperationResult<object>.Fail("OUTSIDE_SCHEDULE", "Ca làm việc này đã được duyệt nghỉ phép (OnLeave), không được phép chấm công.", StatusCodes.Status409Conflict);
        if (scheduleStatus != "Scheduled") return OperationResult<object>.Fail("OUTSIDE_SCHEDULE", "Lịch làm việc không ở trạng thái sẵn sàng để chấm công.", StatusCodes.Status409Conflict);
        if (workDate != DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7))) return OperationResult<object>.Fail("OUTSIDE_SCHEDULE", "Chỉ được chấm công đúng ngày của ca làm việc.", StatusCodes.Status409Conflict);
        await using var leave = new SqlCommand("SELECT COUNT(*) FROM dbo.Leave_Requests WHERE Employee_ID=@e AND Status='Approved' AND @date BETWEEN Start_Date AND End_Date", c);
        leave.Parameters.AddWithValue("@e", owner); leave.Parameters.AddWithValue("@date", workDate);
        if (Convert.ToInt32(await leave.ExecuteScalarAsync(ct)) > 0) return OperationResult<object>.Fail("OUTSIDE_SCHEDULE", "Bạn đang trong thời gian nghỉ phép đã được phê duyệt, không thể chấm công.", StatusCodes.Status409Conflict);
        await using var dup = new SqlCommand("SELECT COUNT(*) FROM dbo.Attendance WHERE Schedule_ID=@id", c);
        dup.Parameters.AddWithValue("@id", scheduleId.Value);
        if (Convert.ToInt32(await dup.ExecuteScalarAsync(ct)) > 0) return OperationResult<object>.Fail("DUPLICATE_CHECK_IN", "Ca này đã chấm công vào.", StatusCodes.Status409Conflict);
        await using var open = new SqlCommand("SELECT COUNT(*) FROM dbo.Attendance a JOIN dbo.Work_Schedules s ON s.Schedule_ID=a.Schedule_ID WHERE s.Employee_ID=@e AND a.Check_Out_Time IS NULL", c);
        open.Parameters.AddWithValue("@e", owner);
        if (Convert.ToInt32(await open.ExecuteScalarAsync(ct)) > 0) return OperationResult<object>.Fail("OPEN_SHIFT_EXISTS", "Bạn đang có một ca làm việc khác chưa chấm công ra. Vui lòng kết thúc ca trước.", StatusCodes.Status409Conflict);

        var nowVn = DateTime.UtcNow.AddHours(7);
        var checkInTimeOfDay = nowVn.TimeOfDay;
        string punctualityNote;
        if (checkInTimeOfDay > shiftStart.Add(TimeSpan.FromMinutes(5)))
        {
            var lateMins = (int)(checkInTimeOfDay - shiftStart).TotalMinutes;
            punctualityNote = $"Đi trễ {lateMins} phút";
        }
        else if (checkInTimeOfDay < shiftStart.Subtract(TimeSpan.FromMinutes(30)))
        {
            var earlyMins = (int)(shiftStart - checkInTimeOfDay).TotalMinutes;
            punctualityNote = $"Vào sớm {earlyMins} phút";
        }
        else
        {
            punctualityNote = "Đúng giờ";
        }
        var userNote = Value(body, "note");
        var finalNote = string.IsNullOrWhiteSpace(userNote) ? punctualityNote : $"{punctualityNote} | {userNote}";

        var id = await ScalarIntAsync(c, "INSERT INTO dbo.Attendance (Schedule_ID,Check_In_Time,Note) VALUES (@schedule,SYSUTCDATETIME(),@note); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, ("@schedule", scheduleId.Value), ("@note", (object?)finalNote ?? DBNull.Value));
        return OperationResult<object>.Ok(new { id = Id("attendance", id), scheduleId = Id("schedule", scheduleId.Value), employeeId = Id("e", owner), checkIn = DateTime.UtcNow, checkOut = (DateTime?)null, note = finalNote });
    }

    private async Task<OperationResult<object>> CheckOutCoreAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        var key = ParseId(id, "attendance"); if (key is null) return NotFound(); await using var c = await connections.OpenAsync(ct); var owner = await ScalarIntOrMissingAsync(c, "SELECT s.Employee_ID FROM dbo.Attendance a JOIN dbo.Work_Schedules s ON s.Schedule_ID=a.Schedule_ID WHERE a.Attendance_ID=@id", ct, null, ("@id", key.Value)); if (owner < 0) return NotFound(); if (!actor.IsManagement && actor.EmployeeId != owner) return Forbidden();
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await using var shiftCmd = new SqlCommand("""
                SELECT ws.End_Time, a.Note
                FROM dbo.Attendance a
                JOIN dbo.Work_Schedules s ON s.Schedule_ID = a.Schedule_ID
                JOIN dbo.Work_Shifts ws ON ws.Shift_ID = s.Shift_ID
                WHERE a.Attendance_ID = @id
            """, c, tx);
            shiftCmd.Parameters.AddWithValue("@id", key.Value);
            await using var shiftReader = await shiftCmd.ExecuteReaderAsync(ct);
            TimeSpan? shiftEndTime = null;
            string? currentNote = null;
            if (await shiftReader.ReadAsync(ct))
            {
                shiftEndTime = shiftReader.GetTimeSpan(0);
                currentNote = shiftReader.IsDBNull(1) ? null : shiftReader.GetString(1);
            }
            await shiftReader.CloseAsync();

            string? earlyNote = null;
            if (shiftEndTime.HasValue)
            {
                var nowTimeOut = DateTime.UtcNow.AddHours(7).TimeOfDay;
                if (nowTimeOut < shiftEndTime.Value.Subtract(TimeSpan.FromMinutes(5)))
                {
                    var earlyMins = (int)(shiftEndTime.Value - nowTimeOut).TotalMinutes;
                    earlyNote = $"Về sớm {earlyMins} phút";
                }
            }
            var userOutNote = Value(body, "note");
            var outPieces = new[] { earlyNote, userOutNote }.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            var appendText = outPieces.Count > 0 ? string.Join(" | ", outPieces) : null;
            var finalUpdatedNote = string.IsNullOrWhiteSpace(currentNote)
                ? appendText
                : (string.IsNullOrWhiteSpace(appendText) ? currentNote : $"{currentNote} | {appendText}");

            var n = await ExecuteNonQueryAsync(c, "UPDATE dbo.Attendance SET Check_Out_Time=SYSUTCDATETIME(), Note=@note WHERE Attendance_ID=@id AND Check_Out_Time IS NULL", ct, tx, ("@note", (object?)finalUpdatedNote ?? DBNull.Value), ("@id", key.Value));
            if (n == 0) { await tx.RollbackAsync(ct); return OperationResult<object>.Fail("ALREADY_PROCESSED", "Đã chấm công ra hoặc không tồn tại.", StatusCodes.Status409Conflict); }
            await ExecuteNonQueryAsync(c, "UPDATE dbo.Work_Schedules SET Status='Completed' WHERE Schedule_ID=(SELECT Schedule_ID FROM dbo.Attendance WHERE Attendance_ID=@id)", ct, tx, ("@id", key.Value));
            await tx.CommitAsync(ct); return OperationResult<object>.Ok(new { id, checkOut = DateTime.UtcNow, note = finalUpdatedNote });
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private async Task<OperationResult<object>> AdminCheckOutCoreAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden();
        var key = ParseId(id, "attendance"); if (key is null) return NotFound();
        var note = Value(body, "note");
        if (string.IsNullOrWhiteSpace(note)) return Validation("Vui lòng nhập ghi chú / lý do quản lý can thiệp chốt công ra.");

        await using var c = await connections.OpenAsync(ct);
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await using var find = new SqlCommand("SELECT a.Attendance_ID, a.Schedule_ID, a.Check_In_Time, a.Check_Out_Time FROM dbo.Attendance a WITH (UPDLOCK, HOLDLOCK) WHERE a.Attendance_ID = @id", c, tx);
            find.Parameters.AddWithValue("@id", key.Value);
            await using var reader = await find.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) { await tx.RollbackAsync(ct); return NotFound(); }
            var scheduleId = reader.GetInt32(1);
            var checkInTime = reader.GetDateTime(2);
            var isAlreadyCheckedOut = !reader.IsDBNull(3);
            await reader.CloseAsync();

            if (isAlreadyCheckedOut) { await tx.RollbackAsync(ct); return OperationResult<object>.Fail("ALREADY_PROCESSED", "Ca làm việc này đã được chấm công ra trước đó.", StatusCodes.Status409Conflict); }

            var checkOutTime = DateTime.UtcNow;
            if (checkOutTime < checkInTime) checkOutTime = checkInTime.AddHours(4);

            await ExecuteNonQueryAsync(c, "UPDATE dbo.Attendance SET Check_Out_Time = @out, Note = CASE WHEN Note IS NULL OR Note = '' THEN @note ELSE Note + N' | Quản lý chốt: ' + @note END WHERE Attendance_ID = @id", ct, tx, ("@out", checkOutTime), ("@note", note), ("@id", key.Value));
            await ExecuteNonQueryAsync(c, "UPDATE dbo.Work_Schedules SET Status = 'Completed' WHERE Schedule_ID = @sid", ct, tx, ("@sid", scheduleId));

            await tx.CommitAsync(ct);
            return OperationResult<object>.Ok(new { id, scheduleId = Id("schedule", scheduleId), checkOut = checkOutTime, note });
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private async Task<OperationResult<object>> CalculateMonthlyPayrollCoreAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden();
        var month = Month(body, "month");
        if (month is null) return Validation("Tháng tính lương không hợp lệ (định dạng YYYY-MM).");

        await using var c = await connections.OpenAsync(ct);
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var employees = new List<(int Id, decimal BaseSalary)>();
            await using (var empCmd = new SqlCommand("SELECT Employee_ID, Base_Salary FROM dbo.Employees WHERE Status = 'Active'", c, tx))
            await using (var reader = await empCmd.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                    employees.Add((reader.GetInt32(0), reader.IsDBNull(1) ? 0m : reader.GetDecimal(1)));
            }

            var createdList = new List<object>();
            foreach (var emp in employees)
            {
                var existsCount = await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Payroll WHERE Employee_ID=@e AND Pay_Month=@m AND Pay_Year=@y", ct, tx, ("@e", emp.Id), ("@m", month.Value.Month), ("@y", month.Value.Year));
                if (existsCount > 0) continue;

                var baseSalary = emp.BaseSalary;
                var shiftRate = Math.Round(baseSalary / 26m, 0);
                var totalScheduled = await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Work_Schedules WHERE Employee_ID=@e AND YEAR(Work_Date)=@y AND MONTH(Work_Date)=@m", ct, tx, ("@e", emp.Id), ("@y", month.Value.Year), ("@m", month.Value.Month));
                var attendedShifts = await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Attendance a JOIN dbo.Work_Schedules s ON s.Schedule_ID=a.Schedule_ID WHERE s.Employee_ID=@e AND YEAR(s.Work_Date)=@y AND MONTH(s.Work_Date)=@m AND a.Check_In_Time IS NOT NULL", ct, tx, ("@e", emp.Id), ("@y", month.Value.Year), ("@m", month.Value.Month));
                var paidLeaveShifts = await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Work_Schedules s WHERE s.Employee_ID=@e AND YEAR(s.Work_Date)=@y AND MONTH(s.Work_Date)=@m AND s.Status='OnLeave' AND EXISTS (SELECT 1 FROM dbo.Leave_Requests l WHERE l.Employee_ID=s.Employee_ID AND l.Status='Approved' AND l.Leave_Type='Annual' AND s.Work_Date BETWEEN l.Start_Date AND l.End_Date)", ct, tx, ("@e", emp.Id), ("@y", month.Value.Year), ("@m", month.Value.Month));
                var unpaidShifts = Math.Max(0, totalScheduled - attendedShifts - paidLeaveShifts);
                var autoDeduction = unpaidShifts * shiftRate;
                if (attendedShifts == 0 && paidLeaveShifts == 0 && totalScheduled > 0) autoDeduction = baseSalary;

                var id = await ScalarIntAsync(c, "INSERT INTO dbo.Payroll (Employee_ID,Pay_Month,Pay_Year,Base_Salary,Bonus,Deduction,Status,Workflow_Status) VALUES (@employee,@month,@year,@base,0,@deduction,'Unpaid','Draft'); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, tx, ("@employee", emp.Id), ("@month", month.Value.Month), ("@year", month.Value.Year), ("@base", baseSalary), ("@deduction", autoDeduction));
                createdList.Add(PayrollObject(id, emp.Id, month.Value, baseSalary, 0, autoDeduction, "Draft"));
            }

            await tx.CommitAsync(ct);
            return OperationResult<object>.Ok(new
            {
                month = month.Value.ToString("yyyy-MM"),
                totalActiveEmployees = employees.Count,
                createdCount = createdList.Count,
                items = createdList
            });
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private async Task<OperationResult<object>> PublishSurveyCoreAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var key = ParseId(id, "survey"); if (key is null) return NotFound(); var requestedCustomers = StringArray(body, "customerIds"); var customers = requestedCustomers.Select(x => ParseId(x, "c")).Where(x => x is not null).Select(x => x!.Value).Distinct().ToArray(); if (customers.Length == 0 || customers.Length != requestedCustomers.Count()) return Validation("Cần chọn các khách nhận khảo sát hợp lệ, không trùng lặp.");
        await using var c = await connections.OpenAsync(ct); await using var tx = (SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var lifecycle = await StringScalarAsync(c, "SELECT Lifecycle_Status FROM dbo.Surveys WITH (UPDLOCK,HOLDLOCK) WHERE Survey_ID=@id", ct, tx, ("@id", key.Value));
            if (lifecycle is null) { await tx.RollbackAsync(ct); return NotFound(); }
            if (lifecycle != "Draft") { await tx.RollbackAsync(ct); return OperationResult<object>.Fail(lifecycle == "Closed" ? "SURVEY_CLOSED" : "SURVEY_PUBLISHED", "Khảo sát không còn ở trạng thái nháp.", StatusCodes.Status409Conflict); }
            var count = await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Customers WHERE Customer_ID IN (SELECT value FROM STRING_SPLIT(@ids,',')) AND Status='Active'", ct, tx, ("@ids", string.Join(',', customers)));
            if (count != customers.Length) { await tx.RollbackAsync(ct); return Validation("Có khách hàng không tồn tại hoặc không còn hoạt động."); }
            foreach (var customerId in customers)
                await ExecuteNonQueryAsync(c, "INSERT INTO dbo.Survey_Targets (Survey_ID,Customer_ID) VALUES (@survey,@customer)", ct, tx, ("@survey", key.Value), ("@customer", customerId));
            await ExecuteNonQueryAsync(c, "UPDATE dbo.Surveys SET Is_Active=1, Lifecycle_Status='Published' WHERE Survey_ID=@id", ct, tx, ("@id", key.Value));
            await tx.CommitAsync(ct); return OperationResult<object>.Ok(new { id, status = "Published", customerIds = customers.Select(x => Id("c", x)).ToArray() });
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private async Task<OperationResult<object>> SubmitSurveyCoreAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        if (actor.CustomerId is null) return OperationResult<object>.Fail("FORBIDDEN", "Chỉ khách hàng có thể trả lời khảo sát.", StatusCodes.Status403Forbidden); var survey = ParseId(id, "survey"); if (survey is null) return NotFound(); if (!body.TryGetProperty("answers", out var answers) || answers.ValueKind != JsonValueKind.Object || !answers.EnumerateObject().Any()) return Validation("Cần có câu trả lời.");
        await using var c = await connections.OpenAsync(ct); await using var tx = (SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            if (await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Surveys s JOIN dbo.Survey_Targets st ON st.Survey_ID=s.Survey_ID WHERE s.Survey_ID=@id AND s.Lifecycle_Status='Published' AND st.Customer_ID=@customer", ct, tx, ("@id", survey.Value), ("@customer", actor.CustomerId.Value)) == 0) { await tx.RollbackAsync(ct); return OperationResult<object>.Fail("NOT_AVAILABLE", "Khảo sát chưa phát hành, đã đóng hoặc không được gửi cho bạn.", StatusCodes.Status409Conflict); }
            if (await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Survey_Responses WITH (UPDLOCK,HOLDLOCK) WHERE Survey_ID=@s AND Customer_ID=@c", ct, tx, ("@s", survey.Value), ("@c", actor.CustomerId.Value)) > 0) { await tx.RollbackAsync(ct); return OperationResult<object>.Fail("DUPLICATE_RESPONSE", "Bạn đã trả lời khảo sát này.", StatusCodes.Status409Conflict); }
            foreach (var answer in answers.EnumerateObject()) { var question = ParseId(answer.Name, "q"); if (question is null || answer.Value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(answer.Value.GetString())) { await tx.RollbackAsync(ct); return Validation("Câu trả lời không hợp lệ."); } var valid = await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Survey_Questions WHERE Question_ID=@q AND Survey_ID=@s", ct, tx, ("@q", question.Value), ("@s", survey.Value)); if (valid == 0) { await tx.RollbackAsync(ct); return Validation("Câu hỏi không thuộc khảo sát."); } await ExecuteNonQueryAsync(c, "INSERT INTO dbo.Survey_Responses (Survey_ID,Customer_ID,Question_ID,Answer_Text) VALUES (@s,@c,@q,@answer)", ct, tx, ("@s", survey.Value), ("@c", actor.CustomerId.Value), ("@q", question.Value), ("@answer", answer.Value.GetString()!)); }
            await tx.CommitAsync(ct); return OperationResult<object>.Ok(new { id, submitted = true });
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    private async Task<OperationResult<object>> SetStatusAsync(IdentityContext actor, string resource, string id, string status, CancellationToken ct)
    {
        if (resource == "accounts" ? actor.Role != Roles.Admin : !actor.IsManagement) return Forbidden();
        var (table, column, prefix) = resource switch
        {
            "customers" => ("dbo.Customers", "Customer_ID", "c"), "products" => ("dbo.Products", "Product_ID", "p"),
            "suppliers" => ("dbo.Suppliers", "Supplier_ID", "supplier"), "employees" => ("dbo.Employees", "Employee_ID", "e"),
            "accounts" => ("dbo.App_Accounts", "Account_ID", "a"), "games" => ("dbo.Games", "Game_ID", "g"),
            "materials" => ("dbo.Materials", "Material_ID", "mat"),
            _ => throw new InvalidOperationException()
        };
        var key = ParseId(id, prefix); if (key is null) return NotFound();
        if (resource == "accounts" && key == actor.AccountId) return OperationResult<object>.Fail("SELF_LOCKOUT", "Không thể khóa chính tài khoản hiện tại.", StatusCodes.Status409Conflict);
        await using var c = await connections.OpenAsync(ct);
        var n = resource == "games"
            ? await ExecuteNonQueryAsync(c, "UPDATE dbo.Games SET Is_Active=0 WHERE Game_ID=@id", ct, ("@id", key.Value))
            : await ExecuteNonQueryAsync(c, $"UPDATE {table} SET Status=@status{(resource is "accounts" or "materials" ? ", Updated_At=SYSUTCDATETIME()" : "")} WHERE {column}=@id", ct, ("@status", status), ("@id", key.Value));
        return n == 0 ? NotFound() : OperationResult<object>.Ok(new { id, deleted = true, status = resource is "employees" or "games" ? "Inactive" : status });
    }

    private async Task<OperationResult<object>> DeleteCategoryAsync(IdentityContext actor, string id, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var key = ParseId(id, "cat"); if (key is null) return NotFound(); await using var c = await connections.OpenAsync(ct); if (await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Products WHERE Category_ID=@id", ct, ("@id", key.Value)) > 0) return OperationResult<object>.Fail("IN_USE", "Danh mục đang có món sử dụng.", StatusCodes.Status409Conflict); var n = await ExecuteNonQueryAsync(c, "DELETE FROM dbo.Product_Categories WHERE Category_ID=@id", ct, ("@id", key.Value)); return n == 0 ? NotFound() : OperationResult<object>.Ok(new { id, deleted = true });
    }

    private async Task<OperationResult<object>> DeleteComputerAsync(IdentityContext actor, string id, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var key = ParseId(id, "pc"); if (key is null) return NotFound(); await using var c = await connections.OpenAsync(ct); if (await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Usage_Sessions WHERE Computer_ID=@id", ct, ("@id", key.Value)) > 0) return OperationResult<object>.Fail("IN_USE", "Không thể xóa máy đã có lịch sử phiên.", StatusCodes.Status409Conflict); var n = await ExecuteNonQueryAsync(c, "DELETE FROM dbo.Computers WHERE Computer_ID=@id", ct, ("@id", key.Value)); return n == 0 ? NotFound() : OperationResult<object>.Ok(new { id, deleted = true });
    }

    private async Task<OperationResult<object>> DeleteRowAsync(IdentityContext actor, string resource, string id, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var (table, column, prefix) = resource == "shifts" ? ("dbo.Work_Shifts", "Shift_ID", "shift") : ("dbo.Work_Schedules", "Schedule_ID", "schedule"); var key = ParseId(id, prefix); if (key is null) return NotFound(); await using var c = await connections.OpenAsync(ct);
        if (resource == "schedules" && await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.Attendance WHERE Schedule_ID=@id", ct, ("@id", key.Value)) > 0)
            return OperationResult<object>.Fail("HAS_HISTORY", "Lịch làm việc đã có dữ liệu chấm công, không thể xóa.", StatusCodes.Status409Conflict);
        var n = await ExecuteNonQueryAsync(c, $"DELETE FROM {table} WHERE {column}=@id", ct, ("@id", key.Value)); return n == 0 ? NotFound() : OperationResult<object>.Ok(new { id, deleted = true });
    }

    private async Task<OperationResult<object>> DeleteSurveyAsync(IdentityContext actor, string id, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden(); var key = ParseId(id, "survey"); if (key is null) return NotFound(); await using var c = await connections.OpenAsync(ct); var n = await ExecuteNonQueryAsync(c, "DELETE FROM dbo.Surveys WHERE Survey_ID=@id AND Is_Active=0", ct, ("@id", key.Value)); return n == 0 ? OperationResult<object>.Fail("INVALID_TRANSITION", "Chỉ có thể xóa khảo sát nháp.", StatusCodes.Status409Conflict) : OperationResult<object>.Ok(new { id, deleted = true });
    }

    private async Task<OperationResult<object>> UpdateSimpleAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct, string prefix, string table, string column, string field)
    {
        if (!actor.IsManagement) return Forbidden(); var key = ParseId(id, prefix); var value = Required(body, field); if (key is null) return NotFound(); if (value is null) return Validation("Giá trị là bắt buộc."); await using var c = await connections.OpenAsync(ct); var n = await ExecuteNonQueryAsync(c, $"UPDATE {table} SET {column}=@value WHERE {column.Replace("_Name", "_ID") }=@id", ct, ("@value", value), ("@id", key.Value)); return n == 0 ? NotFound() : OperationResult<object>.Ok(new { id, name = value });
    }

    private async Task<OperationResult<object>> UpdatedAsync(SqlConnection connection, string table, List<string> changes, List<(string, object)> parameters, CancellationToken ct, Func<object> result)
    {
        if (changes.Count == 0) return Validation("Không có dữ liệu để cập nhật."); var n = await ExecuteNonQueryAsync(connection, $"UPDATE {table} SET {string.Join(',', changes)} WHERE {KeyColumn(table)}=@id", ct, parameters.ToArray()); return n == 0 ? NotFound() : OperationResult<object>.Ok(result());
    }

    private async Task ReplaceQuestionsAsync(SqlConnection c, SqlTransaction tx, int surveyId, IEnumerable<JsonElement> questions, CancellationToken ct)
    {
        var rows = questions.ToArray(); if (rows.Length is 0 or > 20) throw new InvalidOperationException("Khảo sát phải có từ 1 đến 20 câu hỏi."); await ExecuteNonQueryAsync(c, "DELETE FROM dbo.Survey_Questions WHERE Survey_ID=@id", ct, tx, ("@id", surveyId)); foreach (var row in rows) { var text = Required(row, "text") ?? throw new InvalidOperationException("Câu hỏi không được để trống."); var options = StringArray(row, "options"); await ExecuteNonQueryAsync(c, "INSERT INTO dbo.Survey_Questions (Survey_ID,Question_Text,Question_Type,Options) VALUES (@survey,@text,@type,@options)", ct, tx, ("@survey", surveyId), ("@text", text), ("@type", options.Length == 0 ? "Text" : "Choice"), ("@options", options.Length == 0 ? DBNull.Value : string.Join('|', options))); }
    }

    private async Task<int> EnsureDepartmentAsync(SqlConnection c, SqlTransaction tx, string name, CancellationToken ct)
    {
        var id = await ScalarIntOrMissingAsync(c, "SELECT Department_ID FROM dbo.Departments WHERE Department_Name=@name", ct, tx, ("@name", name)); return id >= 0 ? id : await ScalarIntAsync(c, "INSERT INTO dbo.Departments (Department_Name) VALUES (@name); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, tx, ("@name", name));
    }

    private async Task<int> EnsurePositionAsync(SqlConnection c, SqlTransaction tx, string name, int departmentId, CancellationToken ct)
    {
        var id = await ScalarIntOrMissingAsync(c, "SELECT Position_ID FROM dbo.Positions WHERE Position_Name=@name AND Department_ID=@department", ct, tx, ("@name", name), ("@department", departmentId)); return id >= 0 ? id : await ScalarIntAsync(c, "INSERT INTO dbo.Positions (Position_Name,Department_ID,Access_Level) VALUES (@name,@department,'Staff'); SELECT CAST(SCOPE_IDENTITY() AS int);", ct, tx, ("@name", name), ("@department", departmentId));
    }

    private async Task<int?> TierIdAsync(SqlConnection c, SqlTransaction? tx, string tier, CancellationToken ct)
    {
        await using var cmd = new SqlCommand("SELECT TOP 1 Tier_ID FROM dbo.Membership_Tier WHERE Tier_Name=@tier", c, tx); cmd.Parameters.AddWithValue("@tier", tier); var value = await cmd.ExecuteScalarAsync(ct); return value is null ? null : Convert.ToInt32(value);
    }

    private async Task<bool> UsernameTakenAsync(SqlConnection c, SqlTransaction? tx, string username, CancellationToken ct) => await ScalarIntAsync(c, "SELECT COUNT(*) FROM dbo.App_Accounts WHERE Username=@u UNION ALL SELECT COUNT(*) FROM dbo.Customers WHERE Username=@u", ct, tx, ("@u", username)) > 0;
    private static object EmployeeObject(int id, string fullName, string department, string position, JsonElement body, decimal salary, DateOnly joinedAt) => new { id = Id("e", id), fullName, phone = Value(body, "phone") ?? "", email = Value(body, "email") ?? "", department, position, qualification = Value(body, "qualification") ?? "", baseSalary = salary, joinedAt = joinedAt.ToString("yyyy-MM-dd"), dateOfBirth = Date(body, "dateOfBirth")?.ToString("yyyy-MM-dd"), status = Value(body, "status") ?? "Active" };
    private static object PayrollObject(int id, int employee, DateOnly month, decimal salary, decimal bonus, decimal deduction, string status) => new { id = Id("payroll", id), employeeId = Id("e", employee), month = month.ToString("yyyy-MM"), baseSalary = salary, bonus, deduction, total = Math.Max(0, salary + bonus - deduction), status };
    private static async Task<OperationResult<object>> ExecuteAsync(Func<Task<OperationResult<object>>> action) { try { return await action(); } catch (SqlException ex) when (ex.Number is 2601 or 2627) { return OperationResult<object>.Fail("CONFLICT", "Dữ liệu đã tồn tại hoặc đang được tham chiếu.", StatusCodes.Status409Conflict); } catch (SqlException) { return OperationResult<object>.Fail("DATABASE_ERROR", "Không thể ghi dữ liệu do ràng buộc cơ sở dữ liệu.", StatusCodes.Status409Conflict); } catch (InvalidOperationException ex) { return Validation(ex.Message); } }
    private static Task<int> ScalarIntAsync(SqlConnection c, string sql, CancellationToken ct, params (string Name, object Value)[] p) => ScalarIntCoreAsync(c, sql, ct, null, p, false);
    private static Task<int> ScalarIntAsync(SqlConnection c, string sql, CancellationToken ct, SqlTransaction? tx, params (string Name, object Value)[] p) => ScalarIntCoreAsync(c, sql, ct, tx, p, false);
    private static Task<int> ScalarIntOrMissingAsync(SqlConnection c, string sql, CancellationToken ct, SqlTransaction? tx, params (string Name, object Value)[] p) => ScalarIntCoreAsync(c, sql, ct, tx, p, true);
    private static async Task<int> ScalarIntCoreAsync(SqlConnection c, string sql, CancellationToken ct, SqlTransaction? tx, (string Name, object Value)[] p, bool missingIsMinusOne) { await using var cmd = new SqlCommand(sql, c, tx); foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, Db(v)); var value = await cmd.ExecuteScalarAsync(ct); return value is null || value == DBNull.Value ? (missingIsMinusOne ? -1 : throw new InvalidOperationException("Không tìm thấy dữ liệu.")) : Convert.ToInt32(value); }
    private static async Task<string?> StringScalarAsync(SqlConnection c, string sql, CancellationToken ct, SqlTransaction? tx, params (string Name, object Value)[] p) { await using var cmd = new SqlCommand(sql, c, tx); foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, Db(v)); return (string?)await cmd.ExecuteScalarAsync(ct); }
    private static Task<int> ExecuteNonQueryAsync(SqlConnection c, string sql, CancellationToken ct, params (string Name, object Value)[] p) => ExecuteNonQueryAsync(c, sql, ct, null, p);
    private static async Task<int> ExecuteNonQueryAsync(SqlConnection c, string sql, CancellationToken ct, SqlTransaction? tx, params (string Name, object Value)[] p) { await using var cmd = new SqlCommand(sql, c, tx); foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, Db(v)); return await cmd.ExecuteNonQueryAsync(ct); }
    private static void Add(List<string> fields, List<(string, object)> parameters, string column, string parameter, object? value) { if (value is not null) { fields.Add($"{column}={parameter}"); parameters.Add((parameter, value)); } }
    private static string KeyColumn(string table) => table switch { "dbo.Computers" => "Computer_ID", "dbo.Customers" => "Customer_ID", "dbo.Product_Categories" => "Category_ID", "dbo.Products" => "Product_ID", "dbo.Suppliers" => "Supplier_ID", "dbo.Feedback" => "Feedback_ID", "dbo.Work_Shifts" => "Shift_ID", "dbo.Work_Schedules" => "Schedule_ID", "dbo.App_Accounts" => "Account_ID", _ => throw new InvalidOperationException("Bảng không hỗ trợ.") };
    private static readonly HashSet<string> ScheduleStatuses = ["Scheduled", "Completed", "Absent", "OnLeave"];
    private static bool IsShiftOverlapping(TimeSpan s1, TimeSpan e1, TimeSpan s2, TimeSpan e2)
    {
        static IEnumerable<(TimeSpan Start, TimeSpan End)> Segments(TimeSpan s, TimeSpan e)
        {
            if (e > s) yield return (s, e);
            else { yield return (s, TimeSpan.FromHours(24)); if (e > TimeSpan.Zero) yield return (TimeSpan.Zero, e); }
        }
        foreach (var a in Segments(s1, e1))
            foreach (var b in Segments(s2, e2))
                if (a.Start < b.End && b.Start < a.End) return true;
        return false;
    }
    private static async Task<OperationResult<object>?> CheckScheduleConflictsAsync(SqlConnection c, SqlTransaction? tx, int employeeId, DateOnly date, int shiftId, int? excludeScheduleId, CancellationToken ct)
    {
        await using var leave = new SqlCommand("SELECT COUNT(*) FROM dbo.Leave_Requests WHERE Employee_ID=@e AND Status='Approved' AND @date BETWEEN Start_Date AND End_Date", c, tx);
        leave.Parameters.AddWithValue("@e", employeeId); leave.Parameters.AddWithValue("@date", date);
        if (Convert.ToInt32(await leave.ExecuteScalarAsync(ct)) > 0)
            return OperationResult<object>.Fail("EMPLOYEE_ON_LEAVE", "Nhân viên đang trong kỳ nghỉ phép đã được duyệt vào ngày này.", StatusCodes.Status409Conflict);
        await using var dup = new SqlCommand("SELECT COUNT(*) FROM dbo.Work_Schedules WHERE Employee_ID=@e AND Work_Date=@date AND Shift_ID=@shift AND Schedule_ID<>@exclude", c, tx);
        dup.Parameters.AddWithValue("@e", employeeId); dup.Parameters.AddWithValue("@date", date); dup.Parameters.AddWithValue("@shift", shiftId); dup.Parameters.AddWithValue("@exclude", excludeScheduleId ?? -1);
        if (Convert.ToInt32(await dup.ExecuteScalarAsync(ct)) > 0)
            return OperationResult<object>.Fail("SCHEDULE_EXISTS", "Nhân viên đã được phân ca làm việc này trong ngày.", StatusCodes.Status409Conflict);
        await using var current = new SqlCommand("SELECT Shift_Name, Start_Time, End_Time FROM dbo.Work_Shifts WHERE Shift_ID=@shift", c, tx);
        current.Parameters.AddWithValue("@shift", shiftId);
        await using var currentReader = await current.ExecuteReaderAsync(ct);
        if (!await currentReader.ReadAsync(ct)) return NotFound("Không tìm thấy ca làm.");
        var (start, end) = (currentReader.GetTimeSpan(1), currentReader.GetTimeSpan(2));
        await currentReader.CloseAsync();
        await using var others = new SqlCommand("SELECT sh.Shift_Name, sh.Start_Time, sh.End_Time FROM dbo.Work_Schedules s JOIN dbo.Work_Shifts sh ON sh.Shift_ID=s.Shift_ID WHERE s.Employee_ID=@e AND s.Work_Date=@date AND s.Schedule_ID<>@exclude", c, tx);
        others.Parameters.AddWithValue("@e", employeeId); others.Parameters.AddWithValue("@date", date); others.Parameters.AddWithValue("@exclude", excludeScheduleId ?? -1);
        await using var otherReader = await others.ExecuteReaderAsync(ct);
        while (await otherReader.ReadAsync(ct))
        {
            var (otherName, otherStart, otherEnd) = (otherReader.GetString(0), otherReader.GetTimeSpan(1), otherReader.GetTimeSpan(2));
            if (IsShiftOverlapping(start, end, otherStart, otherEnd))
                return OperationResult<object>.Fail("SCHEDULE_CONFLICT", $"Khung giờ ca làm bị trùng hoặc giao thoa với ca '{otherName}' ({otherStart:hh\\:mm}-{otherEnd:hh\\:mm}) trong ngày.", StatusCodes.Status409Conflict);
        }
        return null;
    }

    private async Task<OperationResult<object>> CreateMaterialAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden();
        var code = Value(body, "code") ?? $"MAT-{Random.Shared.Next(1000, 9999)}";
        var name = Required(body, "name");
        var unit = Value(body, "unit") ?? "g";
        var stock = Decimal(body, "stockQuantity") ?? 0m;
        var reorder = Decimal(body, "reorderLevel") ?? 0m;
        var cost = Decimal(body, "unitCost") ?? 0m;
        var supplierId = Has(body, "supplierId") ? ParseId(Value(body, "supplierId"), "supplier") : null;
        var status = Value(body, "status") ?? "Active";

        if (name is null) return Validation("Tên nguyên vật liệu là bắt buộc.");

        await using var c = await connections.OpenAsync(ct);
        var id = await ScalarIntAsync(c, """
            INSERT INTO dbo.Materials (Material_Code, Material_Name, Unit, Stock_Quantity, Reorder_Level, Unit_Cost, Supplier_ID, Status)
            VALUES (@code, @name, @unit, @stock, @reorder, @cost, @supplier, @status);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """, ct,
            ("@code", code), ("@name", name), ("@unit", unit),
            ("@stock", stock), ("@reorder", reorder), ("@cost", cost),
            ("@supplier", Db(supplierId)), ("@status", status));

        return OperationResult<object>.Ok(new { id = Id("mat", id), code, name, unit, stockQuantity = stock, reorderLevel = reorder, unitCost = cost, supplierId = supplierId is null ? null : Id("supplier", supplierId.Value), status });
    }

    private async Task<OperationResult<object>> UpdateMaterialAsync(IdentityContext actor, string id, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden();
        var key = ParseId(id, "mat");
        if (key is null) return NotFound();
        await using var c = await connections.OpenAsync(ct);
        var changes = new List<string>();
        var p = new List<(string, object)> { ("@id", key.Value) };
        Add(changes, p, "Material_Code", "@code", Value(body, "code"));
        Add(changes, p, "Material_Name", "@name", Value(body, "name"));
        Add(changes, p, "Unit", "@unit", Value(body, "unit"));
        Add(changes, p, "Stock_Quantity", "@stock", Decimal(body, "stockQuantity"));
        Add(changes, p, "Reorder_Level", "@reorder", Decimal(body, "reorderLevel"));
        Add(changes, p, "Unit_Cost", "@cost", Decimal(body, "unitCost"));
        if (Has(body, "supplierId")) Add(changes, p, "Supplier_ID", "@supplier", Db(ParseId(Value(body, "supplierId"), "supplier")));
        Add(changes, p, "Status", "@status", Value(body, "status"));
        if (changes.Count > 0) changes.Add("Updated_At = SYSUTCDATETIME()");
        return await UpdatedAsync(c, "dbo.Materials", changes, p, ct, () => new { id });
    }

    private async Task<OperationResult<object>> CreateBomAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden();
        var productId = ParseId(Value(body, "productId"), "p");
        var materialId = ParseId(Value(body, "materialId"), "mat");
        var quantity = Decimal(body, "quantity");

        if (productId is null || materialId is null || quantity is null || quantity <= 0)
            return Validation("Thông tin định mức BOM không hợp lệ.");

        await using var c = await connections.OpenAsync(ct);
        await ExecuteNonQueryAsync(c, """
            IF EXISTS (SELECT 1 FROM dbo.Product_Materials WHERE Product_ID=@pid AND Material_ID=@mid)
                UPDATE dbo.Product_Materials SET Quantity=@qty WHERE Product_ID=@pid AND Material_ID=@mid
            ELSE
                INSERT INTO dbo.Product_Materials (Product_ID, Material_ID, Quantity) VALUES (@pid, @mid, @qty)
            """, ct, ("@pid", productId.Value), ("@mid", materialId.Value), ("@qty", quantity.Value));

        return OperationResult<object>.Ok(new { id = $"{productId.Value}-{materialId.Value}", productId = Id("p", productId.Value), materialId = Id("mat", materialId.Value), quantity = quantity.Value });
    }

    private async Task<OperationResult<object>> DeleteBomAsync(IdentityContext actor, string id, CancellationToken ct)
    {
        if (!actor.IsManagement) return Forbidden();
        int? pid = null; int? mid = null;
        var parts = id.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[0], out var p1) && int.TryParse(parts[1], out var m1))
        {
            pid = p1; mid = m1;
        }
        else if (parts.Length >= 4 && parts[0] == "p" && int.TryParse(parts[1], out var p2) && parts[2] == "mat" && int.TryParse(parts[3], out var m2))
        {
            pid = p2; mid = m2;
        }

        if (pid is null || mid is null) return NotFound("Không xác định được định mức BOM cần xóa.");

        await using var c = await connections.OpenAsync(ct);
        var n = await ExecuteNonQueryAsync(c, "DELETE FROM dbo.Product_Materials WHERE Product_ID=@pid AND Material_ID=@mid", ct, ("@pid", pid.Value), ("@mid", mid.Value));
        return n == 0 ? NotFound() : OperationResult<object>.Ok(new { id, deleted = true });
    }

    private async Task<OperationResult<object>> CreateMaterialRequestAsync(IdentityContext actor, JsonElement body, CancellationToken ct)
    {
        var itemType = Value(body, "itemType") ?? (Has(body, "productId") ? "Product" : "Material");
        if (itemType is not ("Material" or "Product")) return Validation("Loại yêu cầu phải là Material hoặc Product.");
        var materialId = Has(body, "materialId") ? ParseId(Value(body, "materialId"), "mat") : null;
        var productId = Has(body, "productId") ? ParseId(Value(body, "productId"), "p") : null;
        var quantity = Decimal(body, "quantity");
        var note = Value(body, "note") ?? "";

        if (quantity is null or <= 0) return Validation("Số lượng yêu cầu phải lớn hơn 0.");
        if (itemType == "Material" && materialId is null) return Validation("Vui lòng chọn nguyên vật liệu cần xuất.");
        if (itemType == "Product" && productId is null) return Validation("Vui lòng chọn sản phẩm cần pha chế/chế biến.");

        await using var c = await connections.OpenAsync(ct);
        var id = await ScalarIntAsync(c, """
            INSERT INTO dbo.Material_Requests (Item_Type, Material_ID, Product_ID, Quantity, Status, Note, Requested_By_Account_ID)
            VALUES (@type, @mid, @pid, @qty, 'Pending', @note, @reqBy);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """, ct,
            ("@type", itemType),
            ("@mid", Db(materialId)),
            ("@pid", Db(productId)),
            ("@qty", quantity.Value),
            ("@note", note),
            ("@reqBy", actor.AccountId));

        return OperationResult<object>.Ok(new { id = Id("matreq", id), itemType, materialId = materialId is null ? null : Id("mat", materialId.Value), productId = productId is null ? null : Id("p", productId.Value), quantity = quantity.Value, note, status = "Pending" });
    }

    private async Task<OperationResult<object>> DecideMaterialRequestCoreAsync(IdentityContext actor, string id, DecisionRequest request, CancellationToken ct)
    {
        if (!actor.IsManagement && actor.Role != Roles.Cashier) return Forbidden();
        var key = ParseId(id, "matreq");
        if (key is null) return NotFound();
        if (request.Status is not ("Approved" or "Rejected")) return Validation("Trạng thái phải là Approved hoặc Rejected.");
        await using var c = await connections.OpenAsync(ct);
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            await using var find = new SqlCommand("SELECT Item_Type, Material_ID, Product_ID, Quantity, Status FROM dbo.Material_Requests WITH (UPDLOCK,HOLDLOCK) WHERE Request_ID=@id", c, tx);
            find.Parameters.AddWithValue("@id", key.Value);
            await using var reader = await find.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) { await tx.RollbackAsync(ct); return NotFound(); }
            var itemType = reader.GetString(0);
            var materialId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
            var productId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
            var quantity = reader.GetDecimal(3);
            var status = reader.GetString(4);
            await reader.CloseAsync();

            if (status != "Pending") { await tx.RollbackAsync(ct); return OperationResult<object>.Fail("ALREADY_PROCESSED", "Yêu cầu xuất kho không tồn tại hoặc đã được xử lý.", StatusCodes.Status409Conflict); }

            if (request.Status == "Approved")
            {
                if (itemType == "Material")
                {
                    if (materialId is null) { await tx.RollbackAsync(ct); return Validation("Yêu cầu xuất vật tư thiếu mã nguyên vật liệu."); }
                    await using var matCmd = new SqlCommand("SELECT Material_Name, Stock_Quantity FROM dbo.Materials WITH (UPDLOCK,HOLDLOCK) WHERE Material_ID=@id", c, tx);
                    matCmd.Parameters.AddWithValue("@id", materialId.Value);
                    await using var matReader = await matCmd.ExecuteReaderAsync(ct);
                    if (!await matReader.ReadAsync(ct)) { await tx.RollbackAsync(ct); return NotFound("Không tìm thấy nguyên vật liệu."); }
                    var matName = matReader.GetString(0);
                    var currentStock = matReader.GetDecimal(1);
                    await matReader.CloseAsync();

                    if (currentStock < quantity)
                    {
                        await tx.RollbackAsync(ct);
                        return OperationResult<object>.Fail("OUT_OF_STOCK", $"Tồn kho nguyên vật liệu '{matName}' không đủ để xuất ({currentStock:N2} < {quantity:N2}).", StatusCodes.Status409Conflict);
                    }

                    await ExecuteNonQueryAsync(c, "UPDATE dbo.Materials SET Stock_Quantity = Stock_Quantity - @qty, Updated_At=SYSUTCDATETIME() WHERE Material_ID=@id", ct, tx, ("@qty", quantity), ("@id", materialId.Value));
                }
                else if (itemType == "Product")
                {
                    if (productId is null) { await tx.RollbackAsync(ct); return Validation("Yêu cầu pha chế/sản xuất thiếu mã sản phẩm."); }
                    var bomItems = new List<(int MaterialId, string MaterialName, decimal BomQty)>();
                    await using var bomCmd = new SqlCommand("""
                        SELECT pm.Material_ID, m.Material_Name, pm.Quantity
                        FROM dbo.Product_Materials pm
                        JOIN dbo.Materials m ON m.Material_ID = pm.Material_ID
                        WHERE pm.Product_ID = @pid
                        """, c, tx);
                    bomCmd.Parameters.AddWithValue("@pid", productId.Value);
                    await using var bomReader = await bomCmd.ExecuteReaderAsync(ct);
                    while (await bomReader.ReadAsync(ct))
                    {
                        bomItems.Add((bomReader.GetInt32(0), bomReader.GetString(1), bomReader.GetDecimal(2)));
                    }
                    await bomReader.CloseAsync();

                    if (bomItems.Count == 0)
                    {
                        await tx.RollbackAsync(ct);
                        return OperationResult<object>.Fail("NO_BOM", "Sản phẩm chưa được định nghĩa định mức nguyên vật liệu (BOM).", StatusCodes.Status400BadRequest);
                    }

                    foreach (var item in bomItems)
                    {
                        var needed = item.BomQty * quantity;
                        await using var chkCmd = new SqlCommand("SELECT Stock_Quantity FROM dbo.Materials WITH (UPDLOCK,HOLDLOCK) WHERE Material_ID=@id", c, tx);
                        chkCmd.Parameters.AddWithValue("@id", item.MaterialId);
                        var curStock = (decimal?)await chkCmd.ExecuteScalarAsync(ct) ?? 0m;
                        if (curStock < needed)
                        {
                            await tx.RollbackAsync(ct);
                            return OperationResult<object>.Fail("OUT_OF_STOCK", $"Nguyên vật liệu '{item.MaterialName}' không đủ để sản xuất {quantity} món (cần {needed:N2}, hiện có {curStock:N2}).", StatusCodes.Status409Conflict);
                        }

                        await ExecuteNonQueryAsync(c, "UPDATE dbo.Materials SET Stock_Quantity = Stock_Quantity - @needed, Updated_At=SYSUTCDATETIME() WHERE Material_ID=@id", ct, tx, ("@needed", needed), ("@id", item.MaterialId));
                    }

                    await ExecuteNonQueryAsync(c, "UPDATE dbo.Products SET Stock_Quantity = Stock_Quantity + @qty WHERE Product_ID=@pid", ct, tx, ("@qty", (int)Math.Round(quantity)), ("@pid", productId.Value));
                }
            }

            await ExecuteNonQueryAsync(c, "UPDATE dbo.Material_Requests SET Status=@status, Decided_By_Account_ID=@decidedBy, Decided_At=SYSUTCDATETIME() WHERE Request_ID=@id", ct, tx, ("@status", request.Status), ("@decidedBy", actor.AccountId), ("@id", key.Value));

            await tx.CommitAsync(ct);
            return OperationResult<object>.Ok(new { id, status = request.Status });
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }
    private static string? Value(JsonElement body, string name) => body.ValueKind == JsonValueKind.Object && body.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() : null;
    private static string? Required(JsonElement body, string name) => string.IsNullOrWhiteSpace(Value(body, name)) ? null : Value(body, name);
    private static bool Has(JsonElement body, string name) => body.ValueKind == JsonValueKind.Object && body.TryGetProperty(name, out _);
    private static bool? Bool(JsonElement body, string name) => body.ValueKind == JsonValueKind.Object && body.TryGetProperty(name, out var v) && (v.ValueKind is JsonValueKind.True or JsonValueKind.False) ? v.GetBoolean() : null;
    private static int? Int(JsonElement body, string name) => body.ValueKind == JsonValueKind.Object && body.TryGetProperty(name, out var v) && v.TryGetInt32(out var n) ? n : null;
    private static decimal? Decimal(JsonElement body, string name) => body.ValueKind == JsonValueKind.Object && body.TryGetProperty(name, out var v) && v.TryGetDecimal(out var n) ? n : null;
    private static DateOnly? Date(JsonElement body, string name) => DateOnly.TryParse(Value(body, name), out var date) ? date : null;
    private static DateOnly? Month(JsonElement body, string name) => DateOnly.TryParseExact((Value(body, name) ?? "") + "-01", "yyyy-MM-dd", out var month) ? month : null;
    private static TimeOnly? Time(JsonElement body, string name) => TimeOnly.TryParse(Value(body, name), out var time) ? time : null;
    private static IEnumerable<JsonElement> Array(JsonElement body, string name) => body.ValueKind == JsonValueKind.Object && body.TryGetProperty(name, out var array) && array.ValueKind == JsonValueKind.Array ? array.EnumerateArray() : Enumerable.Empty<JsonElement>();
    private static string[] StringArray(JsonElement body, string name) => Array(body, name).Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToArray();
    private static int? ParseId(string? id, string prefix) => id is not null && id.StartsWith(prefix + "-", StringComparison.OrdinalIgnoreCase) && int.TryParse(id[(prefix.Length + 1)..], out var value) ? value : null;
    private static string? Username(string? value) { var name = value?.Trim().ToLowerInvariant(); return name is { Length: >= 3 and <= 50 } && System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-z0-9_.-]+$") ? name : null; }
    private static object Db(object? value) => value ?? DBNull.Value;
    private static string Id(string prefix, int value) => $"{prefix}-{value:000}";
    private static OperationResult<object> Forbidden() => OperationResult<object>.Fail("FORBIDDEN", "Bạn không có quyền thực hiện thao tác này.", StatusCodes.Status403Forbidden);
    private static OperationResult<object> Validation(string message) => OperationResult<object>.Fail("VALIDATION_ERROR", message);
    private static OperationResult<object> NotFound(string message = "Không tìm thấy dữ liệu.") => OperationResult<object>.Fail("NOT_FOUND", message, StatusCodes.Status404NotFound);
}
