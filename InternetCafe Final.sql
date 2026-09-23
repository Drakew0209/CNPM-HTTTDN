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

-- =============================================
-- 1. Drop existing tables (strict child-before-parent order)
-- =============================================
IF OBJECT_ID('dbo.Attendance', 'U') IS NOT NULL DROP TABLE dbo.Attendance;
IF OBJECT_ID('dbo.Leave_Requests', 'U') IS NOT NULL DROP TABLE dbo.Leave_Requests;
IF OBJECT_ID('dbo.Payroll', 'U') IS NOT NULL DROP TABLE dbo.Payroll;
IF OBJECT_ID('dbo.Order_Details', 'U') IS NOT NULL DROP TABLE dbo.Order_Details;
IF OBJECT_ID('dbo.Inventory_Transactions', 'U') IS NOT NULL DROP TABLE dbo.Inventory_Transactions;
IF OBJECT_ID('dbo.Feedback', 'U') IS NOT NULL DROP TABLE dbo.Feedback;
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
    Gender VARCHAR(10) NULL,
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
    Total_Hours AS (
        CASE WHEN End_Time IS NOT NULL
             THEN CAST(DATEDIFF(MINUTE, Start_Time, End_Time) AS DECIMAL(10,2)) / 60.0
             ELSE NULL END
    ) PERSISTED,
    Amount DECIMAL(12,2) NULL, 
    Status VARCHAR(20) DEFAULT 'Active', -- Active, Completed, Cancelled
    CONSTRAINT PK_Usage_Sessions PRIMARY KEY (Session_ID),
    CONSTRAINT FK_Sessions_Customer FOREIGN KEY (Customer_ID)
        REFERENCES dbo.Customers (Customer_ID)
        ON DELETE CASCADE,
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
        REFERENCES dbo.Customers (Customer_ID)
        ON DELETE CASCADE,
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
    Trans_Date DATETIME DEFAULT GETDATE(),
    CONSTRAINT PK_Transactions PRIMARY KEY (Transaction_ID),
    CONSTRAINT FK_Transactions_Customer FOREIGN KEY (Customer_ID) REFERENCES dbo.Customers (Customer_ID) ON DELETE CASCADE,
    CONSTRAINT FK_Transactions_Employee FOREIGN KEY (Processed_By) REFERENCES dbo.Employees (Employee_ID),
    CONSTRAINT FK_Transactions_Order FOREIGN KEY (Order_ID) REFERENCES dbo.Orders (Order_ID),
    CONSTRAINT FK_Transactions_Session FOREIGN KEY (Session_ID) REFERENCES dbo.Usage_Sessions (Session_ID),
    CONSTRAINT FK_Transactions_Combo FOREIGN KEY (Combo_ID) REFERENCES dbo.Combos (Combo_ID)
);
GO
CREATE UNIQUE INDEX UQ_Transactions_Order ON dbo.Transactions (Order_ID) WHERE Order_ID IS NOT NULL;
CREATE UNIQUE INDEX UQ_Transactions_Session ON dbo.Transactions (Session_ID) WHERE Session_ID IS NOT NULL;
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
-- 3. Seed data
-- =============================================
INSERT INTO dbo.Departments (Department_Name) VALUES (N'Vận hành'), (N'Kỹ thuật'), (N'Thu ngân');
GO
INSERT INTO dbo.Positions (Position_Name, Department_ID, Access_Level)
VALUES (N'Quản lý ca', 1, 'Manager'), (N'Kỹ thuật viên', 2, 'Staff'), (N'Nhân viên thu ngân', 3, 'Staff');
GO
INSERT INTO dbo.Employees (Username, Password_Hash, Full_Name, Date_Of_Birth, Gender, Phone_Number, Position_ID, Base_Salary, Status)
VALUES
    ('emp01', 'hashE1', N'Lê Văn Quản', '1995-03-10', N'Nam', '0901111111', 1, 8000000, 'Active'),
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
INSERT INTO dbo.Usage_Sessions (Customer_ID, Computer_ID, Employee_ID, Start_Time, End_Time, Start_Balance, Amount, Status)
VALUES (1, 1, 2, '2026-09-16 08:00', '2026-09-16 10:00', 100000, 16000, 'Completed');
GO
INSERT INTO dbo.Orders (Customer_ID, Computer_ID, Employee_ID, Total_Amount, Status)
VALUES (1, 1, 2, 45000, 'Served');
GO
INSERT INTO dbo.Products (Category_ID, Product_Name, Price, Stock_Quantity)
VALUES (1, N'Trà sữa trân châu', 25000, 50), (2, N'Mì xào hải sản', 45000, 30);
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
INSERT INTO dbo.Payroll (Employee_ID, Pay_Month, Pay_Year, Base_Salary, Bonus, Deduction, Status)
VALUES (1, 8, 2026, 8000000, 500000, 0, 'Paid'), (2, 8, 2026, 6000000, 0, 100000, 'Paid');
GO
INSERT INTO dbo.Leave_Requests (Employee_ID, Leave_Type, Start_Date, End_Date, Reason, Status, Approved_By)
VALUES (2, 'Sick', '2026-09-20', '2026-09-21', N'Bị cảm', 'Approved', 1);
GO
INSERT INTO dbo.Attendance (Schedule_ID, Check_In_Time, Check_Out_Time)
VALUES (2, '2026-09-16 06:05', '2026-09-16 14:00');
GO