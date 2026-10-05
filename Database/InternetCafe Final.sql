-- =============================================
-- Database Design for Internet Cafe Management System
-- Modules: CRM + HRM + Order/Inventory (F&B) + Usage_Sessions (Prepaid PC rental)
-- RDBMS: Microsoft SQL Server
-- =============================================

-- =============================================
-- 0. Create and Use Database
-- =============================================
IF DB_ID('InternetCafeDB') IS NULL
BEGIN
    CREATE DATABASE InternetCafeDB;
END
GO

USE InternetCafeDB;
GO

-- Required before creating persisted computed columns and filtered indexes.
-- Without these settings, SQL Server stops at Usage_Sessions and leaves a
-- partially created, unseeded schema.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================
-- 1. Drop existing tables (strict child-before-parent order)
-- =============================================
IF OBJECT_ID('dbo.Attendance', 'U') IS NOT NULL DROP TABLE dbo.Attendance;
IF OBJECT_ID('dbo.Leave_Requests', 'U') IS NOT NULL DROP TABLE dbo.Leave_Requests;
IF OBJECT_ID('dbo.Payroll', 'U') IS NOT NULL DROP TABLE dbo.Payroll;
IF OBJECT_ID('dbo.Order_Details', 'U') IS NOT NULL DROP TABLE dbo.Order_Details;
IF OBJECT_ID('dbo.Inventory_Transactions', 'U') IS NOT NULL DROP TABLE dbo.Inventory_Transactions;
IF OBJECT_ID('dbo.Feedback', 'U') IS NOT NULL DROP TABLE dbo.Feedback;
IF OBJECT_ID('dbo.TopUp_Receipts', 'U') IS NOT NULL DROP TABLE dbo.TopUp_Receipts;
IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL DROP TABLE dbo.Transactions;
IF OBJECT_ID('dbo.Combos', 'U') IS NOT NULL DROP TABLE dbo.Combos;
IF OBJECT_ID('dbo.Survey_Responses', 'U') IS NOT NULL DROP TABLE dbo.Survey_Responses;
IF OBJECT_ID('dbo.Work_Schedules', 'U') IS NOT NULL DROP TABLE dbo.Work_Schedules;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Usage_Sessions', 'U') IS NOT NULL DROP TABLE dbo.Usage_Sessions;
IF OBJECT_ID('dbo.Survey_Questions', 'U') IS NOT NULL DROP TABLE dbo.Survey_Questions;
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL DROP TABLE dbo.Customers;
IF OBJECT_ID('dbo.Work_Shifts', 'U') IS NOT NULL DROP TABLE dbo.Work_Shifts;
IF OBJECT_ID('dbo.Product_Categories', 'U') IS NOT NULL DROP TABLE dbo.Product_Categories;
IF OBJECT_ID('dbo.Surveys', 'U') IS NOT NULL DROP TABLE dbo.Surveys;
IF OBJECT_ID('dbo.Membership_Tier', 'U') IS NOT NULL DROP TABLE dbo.Membership_Tier;
IF OBJECT_ID('dbo.Employees', 'U') IS NOT NULL DROP TABLE dbo.Employees;
IF OBJECT_ID('dbo.Computers', 'U') IS NOT NULL DROP TABLE dbo.Computers;
IF OBJECT_ID('dbo.Positions', 'U') IS NOT NULL DROP TABLE dbo.Positions;
IF OBJECT_ID('dbo.Departments', 'U') IS NOT NULL DROP TABLE dbo.Departments;
GO

-- =============================================
-- 2. Create tables (strict parent-before-child order)
-- =============================================

-- HRM base
CREATE TABLE dbo.Departments (
    Department_ID INT IDENTITY(1,1) NOT NULL,
    Department_Name NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_Departments PRIMARY KEY (Department_ID)
);
GO

CREATE TABLE dbo.Positions (
    Position_ID INT IDENTITY(1,1) NOT NULL,
    Position_Name NVARCHAR(100) NOT NULL,
    Department_ID INT NOT NULL,
    Access_Level VARCHAR(20) DEFAULT 'Staff', -- Admin, Manager, Staff
    CONSTRAINT PK_Positions PRIMARY KEY (Position_ID),
    CONSTRAINT FK_Positions_Department FOREIGN KEY (Department_ID)
        REFERENCES dbo.Departments (Department_ID)
);
GO

CREATE TABLE dbo.Employees (
    Employee_ID INT IDENTITY(1,1) NOT NULL,
    Username NVARCHAR(50) NOT NULL,
    Password_Hash NVARCHAR(255) NOT NULL,
    Full_Name NVARCHAR(100) NOT NULL,
    Date_Of_Birth DATE NULL,
    Gender NVARCHAR(10) NULL,
    Phone_Number VARCHAR(15) NULL,
    Email VARCHAR(100) NULL,
    Address NVARCHAR(255) NULL,
    Position_ID INT NOT NULL,
    Hire_Date DATE DEFAULT GETDATE(),
    Base_Salary DECIMAL(12,2) DEFAULT 0.00,
    Status VARCHAR(20) DEFAULT 'Active', -- Active, Resigned, Suspended
    Created_Date DATETIME DEFAULT GETDATE(),
    CONSTRAINT PK_Employees PRIMARY KEY (Employee_ID),
    CONSTRAINT UQ_Employees_Username UNIQUE (Username),
    CONSTRAINT FK_Employees_Position FOREIGN KEY (Position_ID)
        REFERENCES dbo.Positions (Position_ID)
);
GO

-- Usage_Sessions base
CREATE TABLE dbo.Computers (
    Computer_ID INT IDENTITY(1,1) NOT NULL,
    Computer_Code VARCHAR(20) NOT NULL,
    Zone_Type VARCHAR(20) DEFAULT 'Standard', -- Standard, VIP
    Hourly_Rate DECIMAL(10,2) NOT NULL,
    Status VARCHAR(20) DEFAULT 'Available', -- Available, InUse, Maintenance
    CONSTRAINT PK_Computers PRIMARY KEY (Computer_ID),
    CONSTRAINT UQ_Computers_Code UNIQUE (Computer_Code)
);
GO

-- CRM base
CREATE TABLE dbo.Membership_Tier (
    Tier_ID INT IDENTITY(1,1) NOT NULL,
    Tier_Name NVARCHAR(50) NOT NULL,
    Discount_Rate DECIMAL(5,2) DEFAULT 0.00,
    CONSTRAINT PK_Membership_Tier PRIMARY KEY (Tier_ID)
);
GO

-- Survey base (needs Employees)
CREATE TABLE dbo.Surveys (
    Survey_ID INT IDENTITY(1,1) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Created_By INT NULL,
    Created_Date DATETIME DEFAULT GETDATE(),
    Is_Active BIT DEFAULT 1,
    CONSTRAINT PK_Surveys PRIMARY KEY (Survey_ID),
    CONSTRAINT FK_Surveys_Employee FOREIGN KEY (Created_By)
        REFERENCES dbo.Employees (Employee_ID)
);
GO

-- Order F&B base
CREATE TABLE dbo.Product_Categories (
    Category_ID INT IDENTITY(1,1) NOT NULL,
    Category_Name NVARCHAR(100) NOT NULL, -- Do an, Thuc uong, An vat...
    CONSTRAINT PK_Product_Categories PRIMARY KEY (Category_ID)
);
GO

-- HRM shift base
CREATE TABLE dbo.Work_Shifts (
    Shift_ID INT IDENTITY(1,1) NOT NULL,
    Shift_Name NVARCHAR(50) NOT NULL, -- Ca sang, Ca chieu, Ca toi
    Start_Time TIME NOT NULL,
    End_Time TIME NOT NULL,
    CONSTRAINT PK_Work_Shifts PRIMARY KEY (Shift_ID)
);
GO

-- Customers (needs Membership_Tier)
CREATE TABLE dbo.Customers (
    Customer_ID INT IDENTITY(1,1) NOT NULL,
    Username NVARCHAR(50) NOT NULL,
    Password_Hash NVARCHAR(255) NOT NULL,
    Full_Name NVARCHAR(100) NOT NULL,
    Date_Of_Birth DATE NULL,
    Hobbies NVARCHAR(255) NULL,
    Phone_Number VARCHAR(15) NULL,
    Balance DECIMAL(12,2) DEFAULT 0.00,
    Tier_ID INT NOT NULL,
    Status VARCHAR(20) DEFAULT 'Active', -- Active, Banned, Inactive
    Created_Date DATETIME DEFAULT GETDATE(),
    CONSTRAINT PK_Customers PRIMARY KEY (Customer_ID),
    CONSTRAINT UQ_Customers_Username UNIQUE (Username),
    CONSTRAINT FK_Customers_Tier FOREIGN KEY (Tier_ID)
        REFERENCES dbo.Membership_Tier (Tier_ID)
);
GO

-- Combo base (Khuyến mãi nạp giờ)
CREATE TABLE dbo.Combos (
    Combo_ID INT IDENTITY(1,1) NOT NULL,
    Combo_Name NVARCHAR(100) NOT NULL, 
    Price DECIMAL(12,2) NOT NULL,      
    Bonus_Balance DECIMAL(12,2) NOT NULL DEFAULT 0.00, 
    Total_Value AS (Price + Bonus_Balance) PERSISTED,  
    Description NVARCHAR(MAX) NULL,
    Is_Active BIT DEFAULT 1,
    CONSTRAINT PK_Combos PRIMARY KEY (Combo_ID)
);
GO

-- Survey_Questions (needs Surveys)
CREATE TABLE dbo.Survey_Questions (
    Question_ID INT IDENTITY(1,1) NOT NULL,
    Survey_ID INT NOT NULL,
    Question_Text NVARCHAR(MAX) NOT NULL,
    Question_Type VARCHAR(20) DEFAULT 'Choice', -- Choice, Text
    Options NVARCHAR(MAX) NULL,
    CONSTRAINT PK_Survey_Questions PRIMARY KEY (Question_ID),
    CONSTRAINT FK_Questions_Survey FOREIGN KEY (Survey_ID)
        REFERENCES dbo.Surveys (Survey_ID)
        ON DELETE CASCADE
);
GO

-- Usage_Sessions (needs Customers, Computers, Employees)
CREATE TABLE dbo.Usage_Sessions (
    Session_ID INT IDENTITY(1,1) NOT NULL,
    Customer_ID INT NOT NULL,
    Computer_ID INT NOT NULL,
    Employee_ID INT NULL,
    Start_Time DATETIME NOT NULL DEFAULT GETDATE(),
    End_Time DATETIME NULL,
    Start_Balance DECIMAL(12,2) NULL, -- Lưu số dư lúc bắt đầu
    Applied_Hourly_Rate DECIMAL(10,2) NULL, -- Snapshot giá/giờ tại thời điểm bắt đầu phiên
    Total_Hours AS (
        CASE WHEN End_Time IS NOT NULL
             THEN CAST(DATEDIFF(MINUTE, Start_Time, End_Time) AS DECIMAL(10,2)) / 60.0
             ELSE NULL END
    ) PERSISTED,
    Amount DECIMAL(12,2) NULL, 
    Status VARCHAR(20) DEFAULT 'Active', -- Active, Completed, Cancelled
    CONSTRAINT PK_Usage_Sessions PRIMARY KEY (Session_ID),
    CONSTRAINT FK_Sessions_Customer FOREIGN KEY (Customer_ID)
        REFERENCES dbo.Customers (Customer_ID),
    CONSTRAINT FK_Sessions_Computer FOREIGN KEY (Computer_ID)
        REFERENCES dbo.Computers (Computer_ID),
    CONSTRAINT FK_Sessions_Employee FOREIGN KEY (Employee_ID)
        REFERENCES dbo.Employees (Employee_ID)
);
GO

-- Orders (needs Customers, Employees, Computers)
CREATE TABLE dbo.Orders (
    Order_ID INT IDENTITY(1,1) NOT NULL,
    Customer_ID INT NOT NULL,
    Computer_ID INT NULL, -- Để biết khách ngồi máy nào mà mang đồ tới
    Employee_ID INT NULL,
    Order_Date DATETIME DEFAULT GETDATE(),
    Total_Amount DECIMAL(12,2) DEFAULT 0.00,
    Status VARCHAR(20) DEFAULT 'Pending', -- Pending, Preparing, Served, Cancelled
    CONSTRAINT PK_Orders PRIMARY KEY (Order_ID),
    CONSTRAINT FK_Orders_Customer FOREIGN KEY (Customer_ID)
        REFERENCES dbo.Customers (Customer_ID),
    CONSTRAINT FK_Orders_Computer FOREIGN KEY (Computer_ID) 
        REFERENCES dbo.Computers (Computer_ID),
    CONSTRAINT FK_Orders_Employee FOREIGN KEY (Employee_ID)
        REFERENCES dbo.Employees (Employee_ID)
);
GO

-- Products (needs Product_Categories)
CREATE TABLE dbo.Products (
    Product_ID INT IDENTITY(1,1) NOT NULL,
    Category_ID INT NOT NULL,
    Product_Name NVARCHAR(100) NOT NULL,
    Price DECIMAL(12,2) NOT NULL,
    Stock_Quantity INT NOT NULL DEFAULT 0, -- Quản lý số lượng tồn kho
    Status VARCHAR(20) DEFAULT 'Active', -- Active, Discontinued
    CONSTRAINT PK_Products PRIMARY KEY (Product_ID),
    CONSTRAINT FK_Products_Category FOREIGN KEY (Category_ID)
        REFERENCES dbo.Product_Categories (Category_ID)
);
GO

-- Inventory_Transactions (needs Products, Employees)
CREATE TABLE dbo.Inventory_Transactions (
    Inv_Trans_ID INT IDENTITY(1,1) NOT NULL,
    Product_ID INT NOT NULL,
    Employee_ID INT NOT NULL, 
    Trans_Type VARCHAR(20) NOT NULL, -- 'Import', 'Export'
    Quantity INT NOT NULL,
    Note NVARCHAR(255) NULL,
    Created_Date DATETIME DEFAULT GETDATE(),
    CONSTRAINT PK_Inventory_Transactions PRIMARY KEY (Inv_Trans_ID),
    CONSTRAINT FK_Inv_Product FOREIGN KEY (Product_ID) REFERENCES dbo.Products(Product_ID),
    CONSTRAINT FK_Inv_Employee FOREIGN KEY (Employee_ID) REFERENCES dbo.Employees(Employee_ID)
);
GO

-- Work_Schedules (needs Employees, Work_Shifts)
CREATE TABLE dbo.Work_Schedules (
    Schedule_ID INT IDENTITY(1,1) NOT NULL,
    Employee_ID INT NOT NULL,
    Shift_ID INT NOT NULL,
    Work_Date DATE NOT NULL,
    Status VARCHAR(20) DEFAULT 'Scheduled', -- Scheduled, Completed, Absent, OnLeave
    CONSTRAINT PK_Work_Schedules PRIMARY KEY (Schedule_ID),
    CONSTRAINT UQ_Employee_Date_Shift UNIQUE (Employee_ID, Work_Date, Shift_ID),
    CONSTRAINT FK_Schedules_Employee FOREIGN KEY (Employee_ID)
        REFERENCES dbo.Employees (Employee_ID)
        ON DELETE CASCADE,
    CONSTRAINT FK_Schedules_Shift FOREIGN KEY (Shift_ID)
        REFERENCES dbo.Work_Shifts (Shift_ID)
);
GO

-- Survey_Responses (needs Surveys, Customers, Survey_Questions)
CREATE TABLE dbo.Survey_Responses (
    Response_ID INT IDENTITY(1,1) NOT NULL,
    Survey_ID INT NOT NULL,
    Customer_ID INT NOT NULL,
    Question_ID INT NOT NULL,
    Answer_Text NVARCHAR(MAX) NOT NULL,
    Submitted_Date DATETIME DEFAULT GETDATE(),
    CONSTRAINT PK_Survey_Responses PRIMARY KEY (Response_ID),
    CONSTRAINT FK_Responses_Survey FOREIGN KEY (Survey_ID) REFERENCES dbo.Surveys (Survey_ID) ON DELETE CASCADE,
    CONSTRAINT FK_Responses_Customer FOREIGN KEY (Customer_ID) REFERENCES dbo.Customers (Customer_ID) ON DELETE CASCADE,
    CONSTRAINT FK_Responses_Question FOREIGN KEY (Question_ID) REFERENCES dbo.Survey_Questions (Question_ID)
);
GO

-- Transactions (needs Customers, Employees, Orders, Usage_Sessions, Combos)
CREATE TABLE dbo.Transactions (
    Transaction_ID INT IDENTITY(1,1) NOT NULL,
    Customer_ID INT NOT NULL,
    Processed_By INT NULL,
    Order_ID INT NULL,
    Session_ID INT NULL,
    Combo_ID INT NULL, -- Nạp theo Combo
    Trans_Type VARCHAR(20) NOT NULL, -- TopUp, FoodOrder, Rental, Refund
    Amount DECIMAL(12,2) NOT NULL,
    Balance_Before DECIMAL(12,2) NULL, -- Snapshot số dư trước giao dịch (dùng cho biên nhận)
    Trans_Date DATETIME DEFAULT GETDATE(),
    CONSTRAINT PK_Transactions PRIMARY KEY (Transaction_ID),
    CONSTRAINT FK_Transactions_Customer FOREIGN KEY (Customer_ID) REFERENCES dbo.Customers (Customer_ID),
    CONSTRAINT FK_Transactions_Employee FOREIGN KEY (Processed_By) REFERENCES dbo.Employees (Employee_ID),
    CONSTRAINT FK_Transactions_Order FOREIGN KEY (Order_ID) REFERENCES dbo.Orders (Order_ID),
    CONSTRAINT FK_Transactions_Session FOREIGN KEY (Session_ID) REFERENCES dbo.Usage_Sessions (Session_ID),
    CONSTRAINT FK_Transactions_Combo FOREIGN KEY (Combo_ID) REFERENCES dbo.Combos (Combo_ID)
);
GO
CREATE UNIQUE INDEX UQ_Transactions_Order ON dbo.Transactions (Order_ID) WHERE Order_ID IS NOT NULL;
CREATE UNIQUE INDEX UQ_Transactions_Session ON dbo.Transactions (Session_ID) WHERE Session_ID IS NOT NULL;
GO

-- TopUp_Receipts (biên nhận nạp tiền — 1-1 với giao dịch TopUp)
-- Mỗi giao dịch TopUp trong Transactions có đúng 1 biên nhận tương ứng.
-- Ràng buộc nghiệp vụ quan trọng:
--   • FK tới Transactions (Transaction_ID) đảm bảo biên nhận luôn gắn với giao dịch tồn tại.
--   • UNIQUE(Transaction_ID) đảm bảo 1 giao dịch → tối đa 1 biên nhận (chống retry trùng).
--   • CHECK(Trans_Type_Snapshot = 'TopUp') đảm bảo chỉ giao dịch nạp tiền mới có biên nhận.
--     Lưu ý: SQL Server không hỗ trợ CHECK constraint tham chiếu sang bảng khác (cross-table),
--     vì vậy Trans_Type_Snapshot lưu bản sao loại giao dịch ngay trong biên nhận để CHECK hoạt động.
--     Trigger TR_Create_TopUp_Receipt đảm bảo chỉ INSERT khi Trans_Type = 'TopUp' và tự điền snapshot.
CREATE TABLE dbo.TopUp_Receipts (
    Receipt_ID       INT IDENTITY(1,1) NOT NULL,
    Transaction_ID   INT NOT NULL,                          -- FK tới giao dịch TopUp
    Receipt_Code     AS ('RCP-' +                           -- Mã tra cứu: RCP-YYYYMMDD-{Transaction_ID}
                         CONVERT(VARCHAR(8), Trans_Date, 112) + '-' +
                         CAST(Transaction_ID AS VARCHAR(10))
                        ) PERSISTED,
    Trans_Type_Snapshot VARCHAR(10) NOT NULL                -- Bản sao 'TopUp' để CHECK không cần join
        CONSTRAINT CHK_Receipt_TopUpOnly CHECK (Trans_Type_Snapshot = 'TopUp'),
    Customer_ID      INT NOT NULL,                          -- Snapshot KH (denorm để in biên nhận nhanh)
    Processed_By     INT NULL,                              -- Nhân viên thực hiện
    Combo_ID         INT NULL,                              -- Combo áp dụng (nếu có)
    Paid_Amount      DECIMAL(12,2) NOT NULL,                -- Tiền khách thực trả (= Transactions.Amount)
    Bonus_Amount     DECIMAL(12,2) NOT NULL DEFAULT 0.00,   -- Tiền thưởng từ Combo (= Combos.Bonus_Balance)
    Total_Credited   AS (Paid_Amount + Bonus_Amount) PERSISTED, -- Tổng số dư được cộng
    Balance_Before   DECIMAL(12,2) NOT NULL,                -- Số dư trước khi nạp
    Balance_After    AS (Balance_Before + Paid_Amount + Bonus_Amount) PERSISTED, -- Số dư sau nạp
    Trans_Date       DATETIME NOT NULL,                     -- Thời điểm giao dịch (copy từ Transactions)
    CONSTRAINT PK_TopUp_Receipts PRIMARY KEY (Receipt_ID),
    CONSTRAINT UQ_Receipt_Transaction UNIQUE (Transaction_ID),   -- 1 GD TopUp → 1 biên nhận
    CONSTRAINT FK_Receipt_Transaction FOREIGN KEY (Transaction_ID)
        REFERENCES dbo.Transactions (Transaction_ID),
    CONSTRAINT FK_Receipt_Customer FOREIGN KEY (Customer_ID)
        REFERENCES dbo.Customers (Customer_ID),
    CONSTRAINT FK_Receipt_Employee FOREIGN KEY (Processed_By)
        REFERENCES dbo.Employees (Employee_ID),
    CONSTRAINT FK_Receipt_Combo FOREIGN KEY (Combo_ID)
        REFERENCES dbo.Combos (Combo_ID)
);
GO

-- Feedback (needs Customers, Employees)
CREATE TABLE dbo.Feedback (
    Feedback_ID INT IDENTITY(1,1) NOT NULL,
    Customer_ID INT NOT NULL,
    Handled_By INT NULL,
    Subject NVARCHAR(100) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Submitted_Date DATETIME DEFAULT GETDATE(),
    Status VARCHAR(20) DEFAULT 'Pending', -- Pending, Reviewed, Resolved
    Manager_Notes NVARCHAR(MAX) NULL,
    CONSTRAINT PK_Feedback PRIMARY KEY (Feedback_ID),
    CONSTRAINT FK_Feedback_Customer FOREIGN KEY (Customer_ID)
        REFERENCES dbo.Customers (Customer_ID)
        ON DELETE CASCADE,
    CONSTRAINT FK_Feedback_Employee FOREIGN KEY (Handled_By)
        REFERENCES dbo.Employees (Employee_ID)
);
GO

-- Order_Details (needs Orders, Products)
CREATE TABLE dbo.Order_Details (
    Order_Detail_ID INT IDENTITY(1,1) NOT NULL,
    Order_ID INT NOT NULL,
    Product_ID INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    Unit_Price DECIMAL(12,2) NOT NULL,
    Line_Total AS (Quantity * Unit_Price) PERSISTED,
    CONSTRAINT PK_Order_Details PRIMARY KEY (Order_Detail_ID),
    CONSTRAINT FK_OrderDetails_Order FOREIGN KEY (Order_ID)
        REFERENCES dbo.Orders (Order_ID)
        ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Product FOREIGN KEY (Product_ID)
        REFERENCES dbo.Products (Product_ID)
);
GO

-- Payroll (needs Employees)
CREATE TABLE dbo.Payroll (
    Payroll_ID INT IDENTITY(1,1) NOT NULL,
    Employee_ID INT NOT NULL,
    Pay_Month INT NOT NULL,
    Pay_Year INT NOT NULL,
    Base_Salary DECIMAL(12,2) NOT NULL,
    Bonus DECIMAL(12,2) DEFAULT 0.00,
    Deduction DECIMAL(12,2) DEFAULT 0.00,
    Net_Salary AS (Base_Salary + Bonus - Deduction) PERSISTED,
    Payment_Date DATETIME NULL,
    Status VARCHAR(20) DEFAULT 'Unpaid', -- Unpaid, Paid
    CONSTRAINT PK_Payroll PRIMARY KEY (Payroll_ID),
    CONSTRAINT UQ_Employee_Month_Year UNIQUE (Employee_ID, Pay_Month, Pay_Year),
    CONSTRAINT FK_Payroll_Employee FOREIGN KEY (Employee_ID)
        REFERENCES dbo.Employees (Employee_ID)
        ON DELETE CASCADE
);
GO

-- Leave_Requests (needs Employees x2)
CREATE TABLE dbo.Leave_Requests (
    Leave_ID INT IDENTITY(1,1) NOT NULL,
    Employee_ID INT NOT NULL,
    Leave_Type VARCHAR(20) NOT NULL, -- Annual, Sick, Unpaid
    Start_Date DATE NOT NULL,
    End_Date DATE NOT NULL,
    Reason NVARCHAR(255) NULL,
    Status VARCHAR(20) DEFAULT 'Pending', -- Pending, Approved, Rejected
    Approved_By INT NULL,
    Request_Date DATETIME DEFAULT GETDATE(),
    CONSTRAINT PK_Leave_Requests PRIMARY KEY (Leave_ID),
    CONSTRAINT FK_Leave_Employee FOREIGN KEY (Employee_ID)
        REFERENCES dbo.Employees (Employee_ID)
        ON DELETE CASCADE,
    CONSTRAINT FK_Leave_ApprovedBy FOREIGN KEY (Approved_By)
        REFERENCES dbo.Employees (Employee_ID)
);
GO

-- Attendance (needs Work_Schedules)
CREATE TABLE dbo.Attendance (
    Attendance_ID INT IDENTITY(1,1) NOT NULL,
    Schedule_ID INT NOT NULL,
    Check_In_Time DATETIME NULL,
    Check_Out_Time DATETIME NULL,
    Note NVARCHAR(255) NULL,
    CONSTRAINT PK_Attendance PRIMARY KEY (Attendance_ID),
    CONSTRAINT UQ_Attendance_Schedule UNIQUE (Schedule_ID),
    CONSTRAINT FK_Attendance_Schedule FOREIGN KEY (Schedule_ID)
        REFERENCES dbo.Work_Schedules (Schedule_ID)
        ON DELETE CASCADE
);
GO

-- =============================================
-- 2.5 Add Check Constraints
-- =============================================
ALTER TABLE dbo.Customers ADD CONSTRAINT CHK_Customer_Status CHECK (Status IN ('Active', 'Banned', 'Inactive'));
ALTER TABLE dbo.Orders ADD CONSTRAINT CHK_Order_Status CHECK (Status IN ('Pending', 'Preparing', 'Served', 'Cancelled'));
ALTER TABLE dbo.Transactions ADD CONSTRAINT CHK_Trans_Type CHECK (Trans_Type IN ('TopUp', 'FoodOrder', 'Rental', 'Refund'));
ALTER TABLE dbo.Transactions ADD CONSTRAINT CHK_Trans_Amount CHECK (Amount > 0);
ALTER TABLE dbo.Transactions ADD CONSTRAINT CHK_Trans_Balance_Before CHECK (Balance_Before IS NULL OR Balance_Before >= 0);
ALTER TABLE dbo.Work_Schedules ADD CONSTRAINT CHK_Schedule_Status CHECK (Status IN ('Scheduled', 'Completed', 'Absent', 'OnLeave'));
ALTER TABLE dbo.Employees ADD CONSTRAINT CHK_Emp_Gender CHECK (Gender IN (N'Nam', N'Nữ', N'Khác'));

ALTER TABLE dbo.Products ADD CONSTRAINT CHK_Product_Price CHECK (Price >= 0);
ALTER TABLE dbo.Products ADD CONSTRAINT CHK_Stock_Quantity CHECK (Stock_Quantity >= 0);
ALTER TABLE dbo.Order_Details ADD CONSTRAINT CHK_Order_Quantity CHECK (Quantity > 0);
ALTER TABLE dbo.Customers ADD CONSTRAINT CHK_Customer_Balance CHECK (Balance >= 0);
ALTER TABLE dbo.Computers ADD CONSTRAINT CHK_Computer_Status CHECK (Status IN ('Available', 'InUse', 'Maintenance'));

-- CHECKs bổ sung: thời gian, lịch nghỉ phép, lương
ALTER TABLE dbo.Usage_Sessions ADD CONSTRAINT CHK_Session_EndAfterStart
    CHECK (End_Time IS NULL OR End_Time > Start_Time);
ALTER TABLE dbo.Usage_Sessions ADD CONSTRAINT CHK_Session_Status
    CHECK (Status IN ('Active', 'Completed', 'Cancelled'));
ALTER TABLE dbo.Usage_Sessions ADD CONSTRAINT CHK_Session_HourlyRate
    CHECK (Applied_Hourly_Rate IS NULL OR Applied_Hourly_Rate >= 0);
ALTER TABLE dbo.Leave_Requests ADD CONSTRAINT CHK_Leave_DateRange
    CHECK (End_Date >= Start_Date);
ALTER TABLE dbo.Payroll ADD CONSTRAINT CHK_Payroll_Paid_HasDate
    CHECK (Status <> 'Paid' OR Payment_Date IS NOT NULL);

-- UNIQUE filtered index: chỉ 1 phiên Active trên mỗi máy tại một thời điểm
CREATE UNIQUE INDEX UQ_Computer_Active_Session
    ON dbo.Usage_Sessions (Computer_ID)
    WHERE Status = 'Active';
GO

-- =============================================
-- 2.6 Create Triggers
-- =============================================

-- Trigger 1: Sync Inventory_Transactions with Products.Stock_Quantity
CREATE TRIGGER TR_Sync_Inventory
ON dbo.Inventory_Transactions
AFTER INSERT
AS
BEGIN
    UPDATE p
    SET p.Stock_Quantity = p.Stock_Quantity + i.Quantity
    FROM dbo.Products p
    JOIN inserted i ON p.Product_ID = i.Product_ID
    WHERE i.Trans_Type = 'Import';

    UPDATE p
    SET p.Stock_Quantity = p.Stock_Quantity - i.Quantity
    FROM dbo.Products p
    JOIN inserted i ON p.Product_ID = i.Product_ID
    WHERE i.Trans_Type = 'Export';
END;
GO

-- Trigger 2: Sync Order_Details with Products.Stock_Quantity (Selling food)
CREATE TRIGGER TR_Sync_Order_Details_Inventory
ON dbo.Order_Details
AFTER INSERT
AS
BEGIN
    UPDATE p
    SET p.Stock_Quantity = p.Stock_Quantity - i.Quantity
    FROM dbo.Products p
    JOIN inserted i ON p.Product_ID = i.Product_ID;
END;
GO

-- Trigger 3: Sync Customer.Balance from Transactions
-- Trigger tự cập nhật số dư và snapshot Balance_Before, kể cả INSERT nhiều dòng.
CREATE TRIGGER TR_Sync_Customer_Balance
ON dbo.Transactions
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @OpeningBalances TABLE (
        Customer_ID INT PRIMARY KEY,
        Opening_Balance DECIMAL(12,2) NOT NULL
    );

    ;WITH Deltas AS (
        SELECT i.Customer_ID,
               SUM(CASE i.Trans_Type
                    WHEN 'TopUp' THEN i.Amount + ISNULL(cb.Bonus_Balance, 0)
                    WHEN 'Refund' THEN i.Amount
                    WHEN 'Rental' THEN -i.Amount
                    WHEN 'FoodOrder' THEN -i.Amount
                    ELSE 0 END) AS Net_Change
        FROM inserted i
        LEFT JOIN dbo.Combos cb ON i.Combo_ID = cb.Combo_ID
        GROUP BY i.Customer_ID
    )
    UPDATE c
    SET c.Balance = c.Balance + d.Net_Change
    OUTPUT inserted.Customer_ID, deleted.Balance
        INTO @OpeningBalances (Customer_ID, Opening_Balance)
    FROM dbo.Customers c
    JOIN Deltas d ON d.Customer_ID = c.Customer_ID;

    -- Lưu snapshot riêng cho từng TopUp theo thứ tự Transaction_ID.
    -- Snapshot tính cả các giao dịch đứng trước trong cùng batch.
    ;WITH TransactionDeltas AS (
        SELECT i.Transaction_ID, i.Customer_ID, i.Trans_Type,
               CASE i.Trans_Type
                    WHEN 'TopUp' THEN i.Amount + ISNULL(cb.Bonus_Balance, 0)
                    WHEN 'Refund' THEN i.Amount
                    WHEN 'Rental' THEN -i.Amount
                    WHEN 'FoodOrder' THEN -i.Amount
                    ELSE 0 END AS Balance_Delta
        FROM inserted i
        LEFT JOIN dbo.Combos cb ON i.Combo_ID = cb.Combo_ID
    ),
    RunningBalances AS (
        SELECT td.Transaction_ID, td.Customer_ID, td.Trans_Type,
               ob.Opening_Balance + COALESCE(
                   SUM(td.Balance_Delta) OVER (
                       PARTITION BY td.Customer_ID
                       ORDER BY td.Transaction_ID
                       ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING
                   ), 0
               ) AS Balance_Before
        FROM TransactionDeltas td
        JOIN @OpeningBalances ob ON ob.Customer_ID = td.Customer_ID
    )
    UPDATE t
    SET t.Balance_Before = rb.Balance_Before
    FROM dbo.Transactions t
    JOIN RunningBalances rb ON rb.Transaction_ID = t.Transaction_ID
    WHERE rb.Trans_Type = 'TopUp'
      AND t.Balance_Before IS NULL;
END;
GO

-- Trigger 4: Tự tạo biên nhận khi có giao dịch TopUp.
-- Thứ tự First/Last được cấu hình rõ bằng sp_settriggerorder phía dưới.
CREATE TRIGGER TR_Create_TopUp_Receipt
ON dbo.Transactions
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Chỉ xử lý giao dịch TopUp; bỏ qua FoodOrder, Rental, Refund
    INSERT INTO dbo.TopUp_Receipts (
        Transaction_ID,
        Trans_Type_Snapshot,
        Customer_ID,
        Processed_By,
        Combo_ID,
        Paid_Amount,
        Bonus_Amount,
        Balance_Before,
        Trans_Date
    )
    SELECT
        i.Transaction_ID,
        'TopUp',                                    -- Trans_Type_Snapshot cố định
        i.Customer_ID,
        i.Processed_By,
        i.Combo_ID,
        i.Amount,                                   -- Tiền khách trả
        ISNULL(cb.Bonus_Balance, 0.00),             -- Thưởng từ Combo
        t.Balance_Before,                           -- Được TR_Sync_Customer_Balance ghi trước
        i.Trans_Date
    FROM inserted i
    JOIN dbo.Transactions t ON i.Transaction_ID = t.Transaction_ID  -- Lấy Balance_Before đã ghi
    LEFT JOIN dbo.Combos cb ON i.Combo_ID = cb.Combo_ID
    WHERE i.Trans_Type = 'TopUp';
END;
GO

-- Trigger 5: Dong bo Computer.Status theo trang thai Usage_Sessions
-- INSERT -> Active  : chuyen may sang InUse
-- UPDATE -> Completed / Cancelled: chuyen may ve Available
CREATE TRIGGER TR_Sync_Computer_Status
ON dbo.Usage_Sessions
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Không cho mở phiên trên máy đang bảo trì.
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.Computers c ON c.Computer_ID = i.Computer_ID
        WHERE i.Status = 'Active' AND c.Status = 'Maintenance'
    )
    BEGIN
        ;THROW 51001, 'Cannot start an active session on a computer under maintenance.', 1;
    END;

    -- Tính lại trạng thái cho mọi máy bị ảnh hưởng từ trạng thái phiên thực tế.
    -- Giữ Maintenance nguyên vẹn, tránh trả máy Available khi còn phiên Active.
    UPDATE comp
    SET comp.Status = CASE
        WHEN comp.Status = 'Maintenance' THEN 'Maintenance'
        WHEN EXISTS (
            SELECT 1
            FROM dbo.Usage_Sessions active_session
            WHERE active_session.Computer_ID = comp.Computer_ID
              AND active_session.Status = 'Active'
        ) THEN 'InUse'
        ELSE 'Available'
    END
    FROM dbo.Computers comp
    WHERE comp.Computer_ID IN (
        SELECT Computer_ID FROM inserted
        UNION
        SELECT Computer_ID FROM deleted
    );
END;
GO

-- Chốt giá giờ theo giá máy tại thời điểm tạo phiên; không phụ thuộc ứng dụng.
CREATE TRIGGER TR_Snapshot_Session_Hourly_Rate
ON dbo.Usage_Sessions
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE s
    SET Applied_Hourly_Rate = c.Hourly_Rate
    FROM dbo.Usage_Sessions s
    JOIN inserted i ON i.Session_ID = s.Session_ID
    JOIN dbo.Computers c ON c.Computer_ID = i.Computer_ID
    WHERE s.Applied_Hourly_Rate IS NULL OR s.Applied_Hourly_Rate <> c.Hourly_Rate;
END;
GO

CREATE TRIGGER TR_Prevent_Maintenance_While_InUse
ON dbo.Computers
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.Usage_Sessions s ON s.Computer_ID = i.Computer_ID
        WHERE i.Status <> 'InUse' AND s.Status = 'Active'
    )
    BEGIN
        ;THROW 51002, 'A computer with an active session must remain InUse.', 1;
    END;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE i.Status = 'InUse'
          AND NOT EXISTS (
              SELECT 1
              FROM dbo.Usage_Sessions s
              WHERE s.Computer_ID = i.Computer_ID
                AND s.Status = 'Active'
          )
    )
    BEGIN
        ;THROW 51003, 'A computer can be InUse only while an active session exists.', 1;
    END;
END;
GO

-- Đặt thứ tự rõ ràng trước khi seed hoặc ứng dụng ghi giao dịch nạp tiền.
EXEC sp_settriggerorder
    @triggername = 'dbo.TR_Sync_Customer_Balance',
    @order       = 'First',
    @stmttype    = 'INSERT';
GO
EXEC sp_settriggerorder
    @triggername = 'dbo.TR_Create_TopUp_Receipt',
    @order       = 'Last',
    @stmttype    = 'INSERT';
GO

-- =============================================
-- 3. Seed data
-- =============================================
INSERT INTO dbo.Departments (Department_Name) VALUES (N'Vận hành'), (N'Kỹ thuật'), (N'Thu ngân');
GO
INSERT INTO dbo.Positions (Position_Name, Department_ID, Access_Level)
VALUES (N'Quản lý ca', 1, 'Manager'), (N'Kỹ thuật viên', 2, 'Staff'), (N'Nhân viên thu ngân', 3, 'Staff');
GO
INSERT INTO dbo.Employees (Username, Password_Hash, Full_Name, Date_Of_Birth, Gender, Phone_Number, Position_ID, Base_Salary, Status)
VALUES
    ('emp01', 'hashE1', N'Lê Văn Quân', '1995-03-10', N'Nam', '0901111111', 1, 8000000, 'Active'),
    ('emp02', 'hashE2', N'Phạm Thị Ngân', '1999-07-22', N'Nữ', '0902222222', 3, 6000000, 'Active');
GO
INSERT INTO dbo.Computers (Computer_Code, Zone_Type, Hourly_Rate, Status)
VALUES ('PC01', 'Standard', 8000, 'Available'), ('PC02', 'VIP', 15000, 'Available');
GO
INSERT INTO dbo.Membership_Tier (Tier_Name) VALUES ('Member'), ('VIP');
GO
INSERT INTO dbo.Surveys (Title, Description, Created_By)
VALUES (N'Khảo sát nhu cầu thêm món ăn đêm', N'Nhằm mục đích ra mắt menu mới vào tháng sau.', 1);
GO
INSERT INTO dbo.Product_Categories (Category_Name) VALUES (N'Thức uống'), (N'Đồ ăn');
GO
INSERT INTO dbo.Work_Shifts (Shift_Name, Start_Time, End_Time)
VALUES (N'Ca sáng', '06:00', '14:00'), (N'Ca chiều', '14:00', '22:00'), (N'Ca tối', '22:00', '06:00');
GO
INSERT INTO dbo.Customers (Username, Password_Hash, Full_Name, Date_Of_Birth, Hobbies, Tier_ID, Status, Balance)
VALUES
    ('user01', 'hash1', N'Nguyễn Văn A', '2000-05-15', N'Game FPS, Nước ngọt có ga', 1, 'Active', 100000),
    ('user02', 'hash2', N'Trần Thị B', '1998-10-20', N'Game MOBA, Trà sữa', 2, 'Active', 50000);
GO
INSERT INTO dbo.Combos (Combo_Name, Price, Bonus_Balance, Description)
VALUES (N'Nạp 50k tặng 20k', 50000, 20000, N'Combo khuyến mãi cuối tuần');
GO
INSERT INTO dbo.Survey_Questions (Survey_ID, Question_Text, Options)
VALUES
    (1, N'Bạn thường xuyên ăn đêm tại quán không?', N'Có, Không, Thỉnh thoảng'),
    (1, N'Bạn thích món nào sau đây được thêm vào menu?', N'Mì xào hải sản, Cơm rang dưa bò, Gà rán');
GO
INSERT INTO dbo.Usage_Sessions (Customer_ID, Computer_ID, Employee_ID, Start_Time, End_Time, Start_Balance, Applied_Hourly_Rate, Amount, Status)
VALUES (1, 1, 2, '2026-09-16 08:00', '2026-09-16 10:00', 100000, 8000, 16000, 'Completed');
GO
INSERT INTO dbo.Orders (Customer_ID, Computer_ID, Employee_ID, Total_Amount, Status)
VALUES (1, 1, 2, 45000, 'Served');
GO
-- Stock_Quantity khởi tạo = 0; Inventory_Transactions Import bên dưới sẽ cộng đúng số lượng thông qua trigger TR_Sync_Inventory.
-- (Tải INSERT với Stock_Quantity > 0 và Import sẽ cộng đôi tồn kho — đã sửa)
INSERT INTO dbo.Products (Category_ID, Product_Name, Price, Stock_Quantity)
VALUES (1, N'Trà sữa trân châu', 25000, 0), (2, N'Mì xào hải sản', 45000, 0);
GO
INSERT INTO dbo.Inventory_Transactions (Product_ID, Employee_ID, Trans_Type, Quantity, Note)
VALUES (1, 1, 'Import', 50, N'Nhập kho ban đầu'), (2, 1, 'Import', 30, N'Nhập nguyên liệu mì xào');
GO
INSERT INTO dbo.Work_Schedules (Employee_ID, Shift_ID, Work_Date, Status)
VALUES (1, 2, '2026-09-16', 'Scheduled'), (2, 1, '2026-09-16', 'Scheduled');
GO
INSERT INTO dbo.Survey_Responses (Survey_ID, Customer_ID, Question_ID, Answer_Text)
VALUES (1, 1, 1, N'Thỉnh thoảng'), (1, 1, 2, N'Cơm rang dưa bò');
GO
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Order_ID, Session_ID, Combo_ID, Trans_Type, Amount)
VALUES
    (1, NULL, NULL, NULL, 1, 'TopUp', 50000), 
    (1, 2, NULL, 1, NULL, 'Rental', 16000),
    (1, 2, 1, NULL, NULL, 'FoodOrder', 45000);
GO
INSERT INTO dbo.Feedback (Customer_ID, Handled_By, Subject, Content, Status)
VALUES (1, 2, N'Máy số 5 hơi lag', N'Chơi CSGO FPS bị tụt liên tục, mong quán kiểm tra lại', 'Pending');
GO
INSERT INTO dbo.Order_Details (Order_ID, Product_ID, Quantity, Unit_Price)
VALUES (1, 2, 1, 45000);
GO
INSERT INTO dbo.Payroll (Employee_ID, Pay_Month, Pay_Year, Base_Salary, Bonus, Deduction, Payment_Date, Status)
VALUES (1, 8, 2026, 8000000, 500000, 0, '2026-08-31', 'Paid'),
       (2, 8, 2026, 6000000, 0, 100000, '2026-08-31', 'Paid');
GO
INSERT INTO dbo.Leave_Requests (Employee_ID, Leave_Type, Start_Date, End_Date, Reason, Status, Approved_By)
VALUES (2, 'Sick', '2026-09-20', '2026-09-21', N'Bị cảm', 'Approved', 1);
GO
INSERT INTO dbo.Attendance (Schedule_ID, Check_In_Time, Check_Out_Time)
VALUES (2, '2026-09-16 06:05', '2026-09-16 14:00');
GO

-- =============================================
-- 5. Truy vấn mẫu: Tra cứu biên nhận nạp tiền
-- =============================================
/*
-- 5.1 Tra cứu biên nhận theo mã (Receipt_Code)
SELECT
    r.Receipt_Code,
    c.Full_Name         AS Customer_Name,
    c.Username          AS Customer_Username,
    e.Full_Name         AS Cashier_Name,
    cb.Combo_Name,
    r.Paid_Amount,
    r.Bonus_Amount,
    r.Total_Credited,
    r.Balance_Before,
    r.Balance_After,
    r.Trans_Date
FROM dbo.TopUp_Receipts r
JOIN dbo.Customers  c ON r.Customer_ID  = c.Customer_ID
LEFT JOIN dbo.Employees  e ON r.Processed_By = e.Employee_ID
LEFT JOIN dbo.Combos    cb ON r.Combo_ID     = cb.Combo_ID
WHERE r.Receipt_Code = 'RCP-20260916-1';  -- Thay bằng mã cần tra cứu

-- 5.2 Lịch sử nạp tiền của một khách hàng (tra theo username)
SELECT
    r.Receipt_Code,
    r.Paid_Amount,
    r.Bonus_Amount,
    r.Total_Credited,
    r.Balance_Before,
    r.Balance_After,
    cb.Combo_Name,
    e.Full_Name AS Cashier,
    r.Trans_Date
FROM dbo.TopUp_Receipts r
JOIN dbo.Customers  c ON r.Customer_ID  = c.Customer_ID
LEFT JOIN dbo.Employees  e ON r.Processed_By = e.Employee_ID
LEFT JOIN dbo.Combos    cb ON r.Combo_ID     = cb.Combo_ID
WHERE c.Username = 'user01'
ORDER BY r.Trans_Date DESC;
*/
