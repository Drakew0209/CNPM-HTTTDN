using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace InternetCafe.Backend.Data;

public sealed class DatabaseBootstrapper(SqlConnectionFactory connections)
{
    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await EnsureGroupSchemaAsync(connection, cancellationToken);
        await using (var command = new SqlCommand(MigrationSql, connection))
            await command.ExecuteNonQueryAsync(cancellationToken);

        // The group schema deliberately owns its business data.  This seed only fills an
        // empty table, so starting the API never overwrites a classmate's existing data.
        await using (var command = new SqlCommand(SeedSql, connection))
            await command.ExecuteNonQueryAsync(cancellationToken);

        var hasher = new PasswordHasher<object>();
        foreach (var account in DemoAccounts)
        {
            await using var command = new SqlCommand("""
                IF NOT EXISTS (SELECT 1 FROM dbo.App_Accounts WHERE Username = @username)
                BEGIN
                    INSERT INTO dbo.App_Accounts (Username, Password_Hash, Role, Customer_ID, Employee_ID, Full_Name, Status)
                    VALUES (
                        @username,
                        @passwordHash,
                        @role,
                        (SELECT TOP 1 Customer_ID FROM dbo.Customers WHERE Username = @customerUsername),
                        (SELECT TOP 1 Employee_ID FROM dbo.Employees WHERE Username = @employeeUsername),
                        @fullName,
                        'Active'
                    );
                END
                """, connection);
            command.Parameters.AddWithValue("@username", account.Username);
            command.Parameters.AddWithValue("@passwordHash", hasher.HashPassword(new object(), "Demo@123"));
            command.Parameters.AddWithValue("@role", account.Role);
            command.Parameters.AddWithValue("@customerUsername", (object?)account.CustomerUsername ?? DBNull.Value);
            command.Parameters.AddWithValue("@employeeUsername", (object?)account.EmployeeUsername ?? DBNull.Value);
            command.Parameters.AddWithValue("@fullName", account.FullName);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task EnsureGroupSchemaAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            SELECT t.name
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE s.name = 'dbo'
              AND t.name IN (
                  'Departments', 'Positions', 'Employees', 'Computers', 'Membership_Tier',
                  'Product_Categories', 'Work_Shifts', 'Customers', 'Combos', 'Surveys',
                  'Survey_Questions', 'Usage_Sessions', 'Orders', 'Products',
                  'Transactions', 'Order_Details'
              )
            """, connection);
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) found.Add(reader.GetString(0));

        var missing = RequiredGroupTables.Where(table => !found.Contains(table)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException(
                $"InternetCafe group schema is incomplete. Missing: {string.Join(", ", missing)}. " +
                "Create the schema from Database/InternetCafe Final.sql in a new database before starting the API. " +
                "Do not rerun that reset script against a database that already contains data.");
    }

    private static readonly string[] RequiredGroupTables =
    [
        "Departments", "Positions", "Employees", "Computers", "Membership_Tier",
        "Product_Categories", "Work_Shifts", "Customers", "Combos", "Surveys",
        "Survey_Questions", "Usage_Sessions", "Orders", "Products", "Transactions", "Order_Details"
    ];

    private static readonly (string Username, string Role, string? CustomerUsername, string? EmployeeUsername, string FullName)[] DemoAccounts =
    [
        ("admin", Roles.Admin, null, null, "Quản trị hệ thống"),
        ("owner", Roles.Owner, null, "emp01", "Chủ quán"),
        ("manager", Roles.Manager, null, "emp01", "Quản lý quán"),
        ("cashier", Roles.Cashier, null, "emp02", "Thu ngân"),
        ("staff", Roles.Staff, null, "emp03", "Nhân viên"),
        ("gamer", Roles.Customer, "user01", null, "Khách hàng demo")
    ];

    private const string MigrationSql = """
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;

        IF COL_LENGTH('dbo.Customers', 'Email') IS NULL ALTER TABLE dbo.Customers ADD Email VARCHAR(100) NULL;
        IF COL_LENGTH('dbo.Products', 'Image_Url') IS NULL ALTER TABLE dbo.Products ADD Image_Url NVARCHAR(500) NULL;
        IF COL_LENGTH('dbo.Orders', 'Notes') IS NULL ALTER TABLE dbo.Orders ADD Notes NVARCHAR(500) NULL;
        IF COL_LENGTH('dbo.Employees', 'Qualification') IS NULL ALTER TABLE dbo.Employees ADD Qualification NVARCHAR(255) NULL;
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Employees') AND name = 'Gender' AND TYPE_NAME(user_type_id) <> 'nvarchar')
            EXEC(N'ALTER TABLE dbo.Employees ALTER COLUMN Gender NVARCHAR(10) NULL;');
        UPDATE dbo.Employees SET Gender = N'Nữ' WHERE Username = 'emp02' AND Gender IN (N'?', N'N?');

        IF OBJECT_ID('dbo.App_Accounts', 'U') IS NULL
        CREATE TABLE dbo.App_Accounts (
            Account_ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_App_Accounts PRIMARY KEY,
            Username NVARCHAR(50) NOT NULL CONSTRAINT UQ_App_Accounts_Username UNIQUE,
            Password_Hash NVARCHAR(500) NOT NULL,
            Role VARCHAR(20) NOT NULL,
            Customer_ID INT NULL,
            Employee_ID INT NULL,
            Full_Name NVARCHAR(100) NOT NULL,
            Status VARCHAR(20) NOT NULL CONSTRAINT DF_App_Accounts_Status DEFAULT 'Active',
            Created_At DATETIME2 NOT NULL CONSTRAINT DF_App_Accounts_Created_At DEFAULT SYSUTCDATETIME(),
            Updated_At DATETIME2 NOT NULL CONSTRAINT DF_App_Accounts_Updated_At DEFAULT SYSUTCDATETIME(),
            CONSTRAINT CHK_App_Accounts_Role CHECK (Role IN ('Admin','Owner','Manager','Cashier','Staff','Customer')),
            CONSTRAINT CHK_App_Accounts_Status CHECK (Status IN ('Active','Banned','Inactive')),
            CONSTRAINT FK_App_Accounts_Customer FOREIGN KEY (Customer_ID) REFERENCES dbo.Customers(Customer_ID),
            CONSTRAINT FK_App_Accounts_Employee FOREIGN KEY (Employee_ID) REFERENCES dbo.Employees(Employee_ID)
        );

        IF OBJECT_ID('dbo.App_Tokens', 'U') IS NULL
        CREATE TABLE dbo.App_Tokens (
            Token_ID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_App_Tokens PRIMARY KEY,
            Account_ID INT NOT NULL,
            Token_Hash CHAR(64) NOT NULL CONSTRAINT UQ_App_Tokens_Hash UNIQUE,
            Expires_At DATETIME2 NOT NULL,
            Revoked_At DATETIME2 NULL,
            Created_At DATETIME2 NOT NULL CONSTRAINT DF_App_Tokens_Created_At DEFAULT SYSUTCDATETIME(),
            CONSTRAINT FK_App_Tokens_Account FOREIGN KEY (Account_ID) REFERENCES dbo.App_Accounts(Account_ID)
        );
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_App_Tokens_Active')
            CREATE INDEX IX_App_Tokens_Active ON dbo.App_Tokens(Token_Hash, Expires_At) INCLUDE (Account_ID, Revoked_At);

        IF OBJECT_ID('dbo.TopUp_Requests', 'U') IS NULL
        CREATE TABLE dbo.TopUp_Requests (
            TopUp_ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TopUp_Requests PRIMARY KEY,
            Customer_ID INT NOT NULL,
            Amount DECIMAL(12,2) NOT NULL,
            Note NVARCHAR(500) NULL,
            Status VARCHAR(20) NOT NULL CONSTRAINT DF_TopUp_Requests_Status DEFAULT 'Pending',
            Requested_At DATETIME2 NOT NULL CONSTRAINT DF_TopUp_Requests_Requested_At DEFAULT SYSUTCDATETIME(),
            Decided_At DATETIME2 NULL,
            Decided_By INT NULL,
            Transaction_ID INT NULL,
            Idempotency_Key NVARCHAR(100) NULL,
            CONSTRAINT CHK_TopUp_Requests_Amount CHECK (Amount >= 1000 AND Amount <= 10000000),
            CONSTRAINT CHK_TopUp_Requests_Status CHECK (Status IN ('Pending','Approved','Rejected')),
            CONSTRAINT FK_TopUp_Requests_Customer FOREIGN KEY (Customer_ID) REFERENCES dbo.Customers(Customer_ID),
            CONSTRAINT FK_TopUp_Requests_Decided_By FOREIGN KEY (Decided_By) REFERENCES dbo.Employees(Employee_ID),
            CONSTRAINT FK_TopUp_Requests_Transaction FOREIGN KEY (Transaction_ID) REFERENCES dbo.Transactions(Transaction_ID)
        );
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_TopUp_Requests_Customer_Key')
            CREATE UNIQUE INDEX UQ_TopUp_Requests_Customer_Key ON dbo.TopUp_Requests(Customer_ID, Idempotency_Key) WHERE Idempotency_Key IS NOT NULL;

        IF OBJECT_ID('dbo.Machine_Heartbeats', 'U') IS NULL
        CREATE TABLE dbo.Machine_Heartbeats (
            Computer_ID INT NOT NULL CONSTRAINT PK_Machine_Heartbeats PRIMARY KEY,
            Last_Seen_At DATETIME2 NOT NULL,
            Last_Account_ID INT NULL,
            CONSTRAINT FK_Machine_Heartbeats_Computer FOREIGN KEY (Computer_ID) REFERENCES dbo.Computers(Computer_ID),
            CONSTRAINT FK_Machine_Heartbeats_Account FOREIGN KEY (Last_Account_ID) REFERENCES dbo.App_Accounts(Account_ID)
        );

        IF OBJECT_ID('dbo.Idempotency_Records', 'U') IS NULL
        CREATE TABLE dbo.Idempotency_Records (
            Idempotency_ID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Idempotency_Records PRIMARY KEY,
            Account_ID INT NOT NULL,
            Route NVARCHAR(120) NOT NULL,
            Idempotency_Key NVARCHAR(100) NOT NULL,
            Response_Json NVARCHAR(MAX) NOT NULL,
            Created_At DATETIME2 NOT NULL CONSTRAINT DF_Idempotency_Records_Created_At DEFAULT SYSUTCDATETIME(),
            CONSTRAINT UQ_Idempotency_Records UNIQUE (Account_ID, Route, Idempotency_Key),
            CONSTRAINT FK_Idempotency_Records_Account FOREIGN KEY (Account_ID) REFERENCES dbo.App_Accounts(Account_ID)
        );

        IF OBJECT_ID('dbo.Outbox_Events', 'U') IS NULL
        CREATE TABLE dbo.Outbox_Events (
            Event_ID UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Outbox_Events PRIMARY KEY,
            Event_Type NVARCHAR(80) NOT NULL,
            Entity_ID NVARCHAR(80) NOT NULL,
            Payload NVARCHAR(MAX) NOT NULL,
            Occurred_At DATETIME2 NOT NULL CONSTRAINT DF_Outbox_Events_Occurred_At DEFAULT SYSUTCDATETIME(),
            Published_At DATETIME2 NULL
        );

        IF OBJECT_ID('dbo.Suppliers', 'U') IS NULL
        CREATE TABLE dbo.Suppliers (
            Supplier_ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
            Name NVARCHAR(150) NOT NULL,
            Phone VARCHAR(20) NULL,
            Email VARCHAR(100) NULL,
            Address NVARCHAR(255) NULL,
            Status VARCHAR(20) NOT NULL CONSTRAINT DF_Suppliers_Status DEFAULT 'Active',
            Created_At DATETIME2 NOT NULL CONSTRAINT DF_Suppliers_Created_At DEFAULT SYSUTCDATETIME(),
            Updated_At DATETIME2 NOT NULL CONSTRAINT DF_Suppliers_Updated_At DEFAULT SYSUTCDATETIME(),
            CONSTRAINT CHK_Suppliers_Status CHECK (Status IN ('Active', 'Inactive'))
        );

        -- These additions translate the group schema into the API contract without
        -- changing or deleting the tables maintained by the original SQL script.
        -- Each schema addition runs through dynamic SQL. SQL Server compiles an
        -- entire batch before executing it, so a later constraint in the same
        -- batch cannot reliably refer to a column added immediately above.
        IF COL_LENGTH('dbo.Inventory_Transactions', 'Supplier_ID') IS NULL
            EXEC(N'ALTER TABLE dbo.Inventory_Transactions ADD Supplier_ID INT NULL;');
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Inventory_Transactions_Supplier')
            EXEC(N'ALTER TABLE dbo.Inventory_Transactions ADD CONSTRAINT FK_Inventory_Transactions_Supplier
                   FOREIGN KEY (Supplier_ID) REFERENCES dbo.Suppliers(Supplier_ID);');

        IF COL_LENGTH('dbo.Surveys', 'Lifecycle_Status') IS NULL
            EXEC(N'ALTER TABLE dbo.Surveys ADD Lifecycle_Status VARCHAR(20) NOT NULL
                   CONSTRAINT DF_Surveys_Lifecycle_Status DEFAULT ''Draft'';');
        IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Surveys_Lifecycle_Status')
            EXEC(N'ALTER TABLE dbo.Surveys ADD CONSTRAINT CHK_Surveys_Lifecycle_Status
                   CHECK (Lifecycle_Status IN (''Draft'', ''Published'', ''Closed''));');
        -- Existing group surveys predate Lifecycle_Status. Preserve their active
        -- state and make legacy published surveys visible to active customers.
        EXEC(N'UPDATE dbo.Surveys
               SET Lifecycle_Status = ''Published''
               WHERE Lifecycle_Status = ''Draft'' AND ISNULL(Is_Active, 0) = 1;');
        IF OBJECT_ID('dbo.Survey_Targets', 'U') IS NULL
        CREATE TABLE dbo.Survey_Targets (
            Survey_ID INT NOT NULL,
            Customer_ID INT NOT NULL,
            Created_At DATETIME2 NOT NULL CONSTRAINT DF_Survey_Targets_Created_At DEFAULT SYSUTCDATETIME(),
            CONSTRAINT PK_Survey_Targets PRIMARY KEY (Survey_ID, Customer_ID),
            CONSTRAINT FK_Survey_Targets_Survey FOREIGN KEY (Survey_ID) REFERENCES dbo.Surveys(Survey_ID),
            CONSTRAINT FK_Survey_Targets_Customer FOREIGN KEY (Customer_ID) REFERENCES dbo.Customers(Customer_ID)
        );
        EXEC(N'INSERT INTO dbo.Survey_Targets (Survey_ID, Customer_ID)
               SELECT s.Survey_ID, c.Customer_ID
               FROM dbo.Surveys s
               CROSS JOIN dbo.Customers c
               WHERE s.Lifecycle_Status = ''Published'' AND c.Status = ''Active''
                 AND NOT EXISTS (SELECT 1 FROM dbo.Survey_Targets st WHERE st.Survey_ID = s.Survey_ID);');

        IF COL_LENGTH('dbo.Payroll', 'Workflow_Status') IS NULL
            EXEC(N'ALTER TABLE dbo.Payroll ADD Workflow_Status VARCHAR(20) NOT NULL
                   CONSTRAINT DF_Payroll_Workflow_Status DEFAULT ''Draft'';');
        IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Payroll_Workflow_Status')
            EXEC(N'ALTER TABLE dbo.Payroll ADD CONSTRAINT CHK_Payroll_Workflow_Status
                   CHECK (Workflow_Status IN (''Draft'', ''Approved'', ''Paid''));');
        EXEC(N'UPDATE dbo.Payroll
               SET Workflow_Status = ''Paid''
               WHERE Workflow_Status = ''Draft'' AND Status = ''Paid'';');

        IF COL_LENGTH('dbo.Idempotency_Records', 'Request_Hash') IS NULL
            ALTER TABLE dbo.Idempotency_Records ADD Request_Hash CHAR(64) NULL;

        COMMIT TRANSACTION;
        """;

    // This is intentionally independent of Database/InternetCafe Final.sql. That file is
    // the schema/reset script supplied with the project; this startup seed must remain
    // harmless when the application is restarted against a database that contains work.
    private const string SeedSql = """
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Departments)
            INSERT INTO dbo.Departments (Department_Name)
            VALUES (N'Vận hành'), (N'Kỹ thuật'), (N'Thu ngân');

        DECLARE @OperationsDepartmentId INT = COALESCE(
            (SELECT TOP 1 Department_ID FROM dbo.Departments WHERE Department_Name = N'Vận hành'),
            (SELECT MIN(Department_ID) FROM dbo.Departments));
        DECLARE @TechnicalDepartmentId INT = COALESCE(
            (SELECT TOP 1 Department_ID FROM dbo.Departments WHERE Department_Name = N'Kỹ thuật'),
            @OperationsDepartmentId);
        DECLARE @CashierDepartmentId INT = COALESCE(
            (SELECT TOP 1 Department_ID FROM dbo.Departments WHERE Department_Name = N'Thu ngân'),
            @OperationsDepartmentId);

        IF NOT EXISTS (SELECT 1 FROM dbo.Positions)
            INSERT INTO dbo.Positions (Position_Name, Department_ID, Access_Level)
            VALUES
                (N'Quản lý ca', @OperationsDepartmentId, 'Manager'),
                (N'Kỹ thuật viên', @TechnicalDepartmentId, 'Staff'),
                (N'Nhân viên thu ngân', @CashierDepartmentId, 'Staff');

        DECLARE @ManagerPositionId INT = COALESCE(
            (SELECT TOP 1 Position_ID FROM dbo.Positions WHERE Position_Name = N'Quản lý ca'),
            (SELECT MIN(Position_ID) FROM dbo.Positions));
        DECLARE @TechnicianPositionId INT = COALESCE(
            (SELECT TOP 1 Position_ID FROM dbo.Positions WHERE Position_Name = N'Kỹ thuật viên'),
            @ManagerPositionId);
        DECLARE @CashierPositionId INT = COALESCE(
            (SELECT TOP 1 Position_ID FROM dbo.Positions WHERE Position_Name = N'Nhân viên thu ngân'),
            @ManagerPositionId);

        IF NOT EXISTS (SELECT 1 FROM dbo.Employees)
            INSERT INTO dbo.Employees
                (Username, Password_Hash, Full_Name, Date_Of_Birth, Gender, Phone_Number, Email, Position_ID, Base_Salary, Status)
            VALUES
                ('emp01', 'seeded-via-app-accounts', N'Lê Văn Quân', '1995-03-10', N'Nam', '0901111111', 'quan@internetcafe.local', @ManagerPositionId, 8000000, 'Active'),
                ('emp02', 'seeded-via-app-accounts', N'Phạm Thị Ngân', '1999-07-22', N'Nữ', '0902222222', 'ngan@internetcafe.local', @CashierPositionId, 6000000, 'Active'),
                ('emp03', 'seeded-via-app-accounts', N'Nguyễn Minh Khoa', '1998-11-03', N'Nam', '0903333333', 'khoa@internetcafe.local', @TechnicianPositionId, 6500000, 'Active');

        DECLARE @ManagerEmployeeId INT = COALESCE(
            (SELECT TOP 1 Employee_ID FROM dbo.Employees WHERE Username = 'emp01'),
            (SELECT MIN(Employee_ID) FROM dbo.Employees));
        DECLARE @CashierEmployeeId INT = COALESCE(
            (SELECT TOP 1 Employee_ID FROM dbo.Employees WHERE Username = 'emp02'),
            @ManagerEmployeeId);

        IF NOT EXISTS (SELECT 1 FROM dbo.Membership_Tier)
            INSERT INTO dbo.Membership_Tier (Tier_Name, Discount_Rate)
            VALUES (N'Member', 0), (N'VIP', 0), (N'Silver', 5), (N'Gold', 10);

        DECLARE @MemberTierId INT = COALESCE(
            (SELECT TOP 1 Tier_ID FROM dbo.Membership_Tier WHERE Tier_Name = N'Member'),
            (SELECT MIN(Tier_ID) FROM dbo.Membership_Tier));
        DECLARE @VipTierId INT = COALESCE(
            (SELECT TOP 1 Tier_ID FROM dbo.Membership_Tier WHERE Tier_Name = N'VIP'),
            @MemberTierId);

        IF NOT EXISTS (SELECT 1 FROM dbo.Product_Categories)
            INSERT INTO dbo.Product_Categories (Category_Name)
            VALUES (N'Thức uống'), (N'Đồ ăn');

        DECLARE @DrinkCategoryId INT = COALESCE(
            (SELECT TOP 1 Category_ID FROM dbo.Product_Categories WHERE Category_Name = N'Thức uống'),
            (SELECT MIN(Category_ID) FROM dbo.Product_Categories));
        DECLARE @FoodCategoryId INT = COALESCE(
            (SELECT TOP 1 Category_ID FROM dbo.Product_Categories WHERE Category_Name = N'Đồ ăn'),
            @DrinkCategoryId);

        IF NOT EXISTS (SELECT 1 FROM dbo.Work_Shifts)
            INSERT INTO dbo.Work_Shifts (Shift_Name, Start_Time, End_Time)
            VALUES
                (N'Ca sáng', '06:00', '14:00'),
                (N'Ca chiều', '14:00', '22:00'),
                (N'Ca tối', '22:00', '06:00');

        DECLARE @MorningShiftId INT = COALESCE(
            (SELECT TOP 1 Shift_ID FROM dbo.Work_Shifts WHERE Shift_Name = N'Ca sáng'),
            (SELECT MIN(Shift_ID) FROM dbo.Work_Shifts));
        DECLARE @AfternoonShiftId INT = COALESCE(
            (SELECT TOP 1 Shift_ID FROM dbo.Work_Shifts WHERE Shift_Name = N'Ca chiều'),
            @MorningShiftId);

        IF NOT EXISTS (SELECT 1 FROM dbo.Computers)
            INSERT INTO dbo.Computers (Computer_Code, Zone_Type, Hourly_Rate, Status)
            VALUES
                ('PC01', 'Standard', 8000, 'Available'),
                ('PC02', 'VIP', 15000, 'Available'),
                ('PC03', 'Standard', 8000, 'Available'),
                ('PC04', 'Standard', 8000, 'Available'),
                ('PC05', 'VIP', 15000, 'Available'),
                ('PC06', 'VIP', 15000, 'Available'),
                ('PC07', 'Standard', 8000, 'Available');

        IF NOT EXISTS (SELECT 1 FROM dbo.Customers)
            INSERT INTO dbo.Customers
                (Username, Password_Hash, Full_Name, Date_Of_Birth, Hobbies, Phone_Number, Email, Balance, Tier_ID, Status)
            VALUES
                ('user01', 'seeded-via-app-accounts', N'Nguyễn Văn An', '2000-05-15', N'Game FPS, nước ngọt', '0901000001', 'an@internetcafe.local', 180000, @MemberTierId, 'Active'),
                ('user02', 'seeded-via-app-accounts', N'Trần Thị Bình', '1998-10-20', N'Game MOBA, trà sữa', '0901000002', 'binh@internetcafe.local', 120000, @VipTierId, 'Active'),
                ('user03', 'seeded-via-app-accounts', N'Lê Minh Tuấn', '2001-03-22', N'Battle Royale, cà phê', '0901000003', 'tuan@internetcafe.local', 250000, @MemberTierId, 'Active'),
                ('user04', 'seeded-via-app-accounts', N'Nguyễn Thị Hoa', '1997-08-14', N'MMORPG, trà đào', '0901000004', 'hoa@internetcafe.local', 500000, @VipTierId, 'Active'),
                ('user05', 'seeded-via-app-accounts', N'Trần Văn Dũng', '2003-11-05', N'FPS, game đối kháng', '0901000005', 'dung@internetcafe.local', 150000, @MemberTierId, 'Active');

        IF NOT EXISTS (SELECT 1 FROM dbo.Combos)
            INSERT INTO dbo.Combos (Combo_Name, Price, Bonus_Balance, Description, Is_Active)
            VALUES
                (N'Nạp 50.000 tặng 20.000', 50000, 20000, N'Ưu đãi demo cho khách thành viên.', 1),
                (N'Nạp 100.000 tặng 50.000', 100000, 50000, N'Ưu đãi demo cho khách thường xuyên.', 1);

        IF NOT EXISTS (SELECT 1 FROM dbo.Products)
            INSERT INTO dbo.Products (Category_ID, Product_Name, Price, Stock_Quantity, Status, Image_Url)
            VALUES
                (@DrinkCategoryId, N'Trà sữa trân châu', 25000, 50, 'Active', NULL),
                (@DrinkCategoryId, N'Cà phê sữa đá', 20000, 100, 'Active', NULL),
                (@DrinkCategoryId, N'Trà đào cam sả', 30000, 80, 'Active', NULL),
                (@DrinkCategoryId, N'Nước ngọt Pepsi', 15000, 150, 'Active', NULL),
                (@FoodCategoryId, N'Mì xào hải sản', 45000, 30, 'Active', NULL),
                (@FoodCategoryId, N'Gà rán ba miếng', 45000, 40, 'Active', NULL),
                (@FoodCategoryId, N'Cơm rang dưa bò', 40000, 35, 'Active', NULL),
                (@FoodCategoryId, N'Bánh mì thịt nguội', 25000, 60, 'Active', NULL);

        IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers)
            INSERT INTO dbo.Suppliers (Name, Phone, Email, Address, Status)
            VALUES
                (N'Highlands Coffee Supply', '02873006666', 'supply@highlandscoffee.com.vn', N'Quận 1, TP. Hồ Chí Minh', 'Active'),
                (N'PepsiCo Việt Nam', '02838247555', 'sales@pepsico.vn', N'Quận 3, TP. Hồ Chí Minh', 'Active'),
                (N'KFC Supply Việt Nam', '02873007575', 'supply@kfcvietnam.com.vn', N'Quận Bình Thạnh, TP. Hồ Chí Minh', 'Active');

        IF NOT EXISTS (SELECT 1 FROM dbo.Surveys)
            INSERT INTO dbo.Surveys (Title, Description, Created_By, Is_Active)
            VALUES (N'Khảo sát món ăn đêm', N'Giúp quán cải thiện thực đơn phục vụ khách chơi tối.', @ManagerEmployeeId, 1);

        DECLARE @SurveyId INT = (SELECT MIN(Survey_ID) FROM dbo.Surveys);
        IF NOT EXISTS (SELECT 1 FROM dbo.Survey_Questions) AND @SurveyId IS NOT NULL
            INSERT INTO dbo.Survey_Questions (Survey_ID, Question_Text, Question_Type, Options)
            VALUES
                (@SurveyId, N'Bạn thường ăn đêm tại quán không?', 'Choice', N'Có|Không|Thỉnh thoảng'),
                (@SurveyId, N'Bạn muốn bổ sung món nào?', 'Text', NULL);

        IF NOT EXISTS (SELECT 1 FROM dbo.Work_Schedules)
           AND @ManagerEmployeeId IS NOT NULL AND @CashierEmployeeId IS NOT NULL
            INSERT INTO dbo.Work_Schedules (Employee_ID, Shift_ID, Work_Date, Status)
            VALUES
                (@ManagerEmployeeId, @AfternoonShiftId, CAST(GETDATE() AS date), 'Scheduled'),
                (@CashierEmployeeId, @MorningShiftId, CAST(GETDATE() AS date), 'Scheduled');

        COMMIT TRANSACTION;
        """;
}
