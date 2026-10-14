-- =============================================
-- MRP Extension for Internet Cafe Management System
-- Modules: Materials, Bill of Materials (BOM), Material Requisitions & Transactions
-- Compatible with Microsoft SQL Server LocalDB
-- =============================================

USE InternetCafeDB;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Drop existing MRP tables in child-to-parent order if re-running
IF OBJECT_ID('dbo.Material_Transactions', 'U') IS NOT NULL DROP TABLE dbo.Material_Transactions;
IF OBJECT_ID('dbo.Material_Requisition_Details', 'U') IS NOT NULL DROP TABLE dbo.Material_Requisition_Details;
IF OBJECT_ID('dbo.Material_Requisitions', 'U') IS NOT NULL DROP TABLE dbo.Material_Requisitions;
IF OBJECT_ID('dbo.Bill_Of_Materials', 'U') IS NOT NULL DROP TABLE dbo.Bill_Of_Materials;
IF OBJECT_ID('dbo.Materials', 'U') IS NOT NULL DROP TABLE dbo.Materials;
GO

-- 2. Create Materials table (Nguyên vật liệu)
CREATE TABLE dbo.Materials (
    Material_ID INT IDENTITY(1,1) NOT NULL,
    Material_Code NVARCHAR(50) NOT NULL,
    Material_Name NVARCHAR(100) NOT NULL,
    Unit NVARCHAR(30) NOT NULL, -- Gói, Quả, Gam, Lon, Cây, Kg, Ml
    Stock_Quantity DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    Min_Stock DECIMAL(12,2) NOT NULL DEFAULT 10.00,
    Cost_Price DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    Supplier_Name NVARCHAR(150) NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Active', -- Active, Discontinued
    Created_Date DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Materials PRIMARY KEY (Material_ID),
    CONSTRAINT UQ_Materials_Code UNIQUE (Material_Code),
    CONSTRAINT CHK_Materials_Stock CHECK (Stock_Quantity >= 0),
    CONSTRAINT CHK_Materials_Cost CHECK (Cost_Price >= 0)
);
GO

-- 3. Create Bill_Of_Materials (BOM - Định mức công thức sản phẩm F&B)
CREATE TABLE dbo.Bill_Of_Materials (
    BOM_ID INT IDENTITY(1,1) NOT NULL,
    Product_ID INT NOT NULL,
    Material_ID INT NOT NULL,
    Quantity DECIMAL(10,2) NOT NULL, -- Định mức NVL trên 1 đơn vị món
    Note NVARCHAR(255) NULL,
    CONSTRAINT PK_Bill_Of_Materials PRIMARY KEY (BOM_ID),
    CONSTRAINT FK_BOM_Product FOREIGN KEY (Product_ID) REFERENCES dbo.Products(Product_ID) ON DELETE CASCADE,
    CONSTRAINT FK_BOM_Material FOREIGN KEY (Material_ID) REFERENCES dbo.Materials(Material_ID) ON DELETE CASCADE,
    CONSTRAINT UQ_BOM_Product_Material UNIQUE (Product_ID, Material_ID),
    CONSTRAINT CHK_BOM_Quantity CHECK (Quantity > 0)
);
GO

-- 4. Create Material_Requisitions (Phiếu đề nghị xuất nguyên vật liệu)
CREATE TABLE dbo.Material_Requisitions (
    Requisition_ID INT IDENTITY(1,1) NOT NULL,
    Requisition_Code NVARCHAR(50) NOT NULL,
    Requested_By INT NOT NULL,
    Shift_ID INT NULL,
    Target_Product_ID INT NULL, -- Món dự kiến chế biến
    Target_Quantity INT NULL DEFAULT 1, -- Số lượng suất chế biến
    Purpose NVARCHAR(255) NOT NULL, -- Chế biến phục vụ khách, Pha chế đầu ca, Dự trữ...
    Status VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Approved, Rejected
    Approved_By INT NULL,
    Approved_Date DATETIME NULL,
    Note NVARCHAR(500) NULL,
    Created_Date DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Material_Requisitions PRIMARY KEY (Requisition_ID),
    CONSTRAINT UQ_Requisition_Code UNIQUE (Requisition_Code),
    CONSTRAINT FK_Req_Employee FOREIGN KEY (Requested_By) REFERENCES dbo.Employees(Employee_ID),
    CONSTRAINT FK_Req_Shift FOREIGN KEY (Shift_ID) REFERENCES dbo.Work_Shifts(Shift_ID),
    CONSTRAINT FK_Req_Product FOREIGN KEY (Target_Product_ID) REFERENCES dbo.Products(Product_ID),
    CONSTRAINT FK_Req_Approver FOREIGN KEY (Approved_By) REFERENCES dbo.Employees(Employee_ID),
    CONSTRAINT CHK_Req_Status CHECK (Status IN ('Pending', 'Approved', 'Rejected'))
);
GO

-- 5. Create Material_Requisition_Details (Chi tiết NVL đề nghị xuất)
CREATE TABLE dbo.Material_Requisition_Details (
    Detail_ID INT IDENTITY(1,1) NOT NULL,
    Requisition_ID INT NOT NULL,
    Material_ID INT NOT NULL,
    Requested_Quantity DECIMAL(10,2) NOT NULL,
    Approved_Quantity DECIMAL(10,2) NULL,
    CONSTRAINT PK_Material_Requisition_Details PRIMARY KEY (Detail_ID),
    CONSTRAINT FK_Req_Detail_Parent FOREIGN KEY (Requisition_ID) REFERENCES dbo.Material_Requisitions(Requisition_ID) ON DELETE CASCADE,
    CONSTRAINT FK_Req_Detail_Material FOREIGN KEY (Material_ID) REFERENCES dbo.Materials(Material_ID),
    CONSTRAINT UQ_Req_Material UNIQUE (Requisition_ID, Material_ID),
    CONSTRAINT CHK_Req_Qty CHECK (Requested_Quantity > 0)
);
GO

-- 6. Create Material_Transactions (Giao dịch Nhập / Xuất kho NVL)
CREATE TABLE dbo.Material_Transactions (
    Trans_ID INT IDENTITY(1,1) NOT NULL,
    Material_ID INT NOT NULL,
    Employee_ID INT NOT NULL,
    Trans_Type VARCHAR(20) NOT NULL, -- 'Import', 'Export', 'Adjustment'
    Quantity DECIMAL(12,2) NOT NULL,
    Unit_Cost DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    Requisition_ID INT NULL,
    Note NVARCHAR(255) NULL,
    Created_Date DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Material_Transactions PRIMARY KEY (Trans_ID),
    CONSTRAINT FK_Mat_Trans_Material FOREIGN KEY (Material_ID) REFERENCES dbo.Materials(Material_ID),
    CONSTRAINT FK_Mat_Trans_Employee FOREIGN KEY (Employee_ID) REFERENCES dbo.Employees(Employee_ID),
    CONSTRAINT FK_Mat_Trans_Req FOREIGN KEY (Requisition_ID) REFERENCES dbo.Material_Requisitions(Requisition_ID),
    CONSTRAINT CHK_Mat_Trans_Type CHECK (Trans_Type IN ('Import', 'Export', 'Adjustment')),
    CONSTRAINT CHK_Mat_Trans_Qty CHECK (Quantity > 0)
);
GO

-- 7. Trigger: Tự động đồng bộ tồn kho NVL khi có Giao dịch kho
IF OBJECT_ID('dbo.TR_Sync_Material_Stock', 'TR') IS NOT NULL DROP TRIGGER dbo.TR_Sync_Material_Stock;
GO

CREATE TRIGGER TR_Sync_Material_Stock
ON dbo.Material_Transactions
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE m
    SET m.Stock_Quantity = CASE 
        WHEN i.Trans_Type = 'Import' THEN m.Stock_Quantity + i.Quantity
        ELSE m.Stock_Quantity - i.Quantity
    END
    FROM dbo.Materials m
    JOIN inserted i ON m.Material_ID = i.Material_ID;
END;
GO

-- 8. Seed dữ liệu mẫu chuẩn MRP cho Cybercafe F&B
SET IDENTITY_INSERT dbo.Materials ON;
INSERT INTO dbo.Materials (Material_ID, Material_Code, Material_Name, Unit, Stock_Quantity, Min_Stock, Cost_Price, Supplier_Name)
VALUES 
    (1, N'NVL-MI-HAOHAO', N'Mì gói Hảo Hảo tôm chua cay', N'Gói', 120.00, 30.00, 4200.00, N'Công ty CP Acecook Việt Nam'),
    (2, N'NVL-TRUNG-GA', N'Trứng gà tươi Ba Huân', N'Quả', 150.00, 40.00, 3300.00, N'Công ty CP Ba Huân'),
    (3, N'NVL-XUC-XICH', N'Xúc xích Đức xông khói CP', N'Cây', 95.00, 25.00, 6800.00, N'CP Foods Việt Nam'),
    (4, N'NVL-THIT-BO', N'Thịt bò Mỹ thái mỏng', N'Gam', 3500.00, 1000.00, 340.00, N'Công ty Thực phẩm Sạch Foodmart'),
    (5, N'NVL-HAI-SAN', N'Tôm mực đông lạnh F&B', N'Gam', 2800.00, 800.00, 250.00, N'Hải sản Minh Phú'),
    (6, N'NVL-BANH-MI', N'Bánh mì giòn nóng', N'Ổ', 40.00, 15.00, 3000.00, N'Lò bánh mì Hưng Thịnh'),
    (7, N'NVL-CAFE-ROBUSTA', N'Cà phê hạt Robusta xay', N'Gam', 4500.00, 1000.00, 180.00, N'Trung Nguyên Legend'),
    (8, N'NVL-SUA-ONGTHO', N'Sữa đặc Ông Thọ đỏ', N'Lon', 30.00, 10.00, 24500.00, N'Vinamilk'),
    (9, N'NVL-TRA-DEN', N'Cốt trà đen nguyên chất', N'Gam', 2500.00, 500.00, 160.00, N'Trà Phúc Long'),
    (10, N'NVL-TRAN-CHAU', N'Trân châu đen Đài Loan', N'Gam', 6000.00, 1200.00, 85.00, N'Nguyên liệu Trà sữa Tân Nhất Hương');
SET IDENTITY_INSERT dbo.Materials OFF;
GO

-- Seed Bill of Materials (BOM) định mức chuẩn công thức sản phẩm F&B
-- Product 1: Trà sữa trân châu (20g cốt trà đen, 30g sữa đặc, 50g trân châu)
INSERT INTO dbo.Bill_Of_Materials (Product_ID, Material_ID, Quantity, Note)
VALUES 
    (1, 9, 20.00, N'Ủ 20g trà đen với 150ml nước sôi'),
    (1, 8, 0.08, N'Định mức 1/12 lon sữa đặc (~30g)'),
    (1, 10, 50.00, N'50g trân châu đen nấu dẻo');

-- Product 2: Mì xào hải sản (1 gói mì, 80g tôm mực)
INSERT INTO dbo.Bill_Of_Materials (Product_ID, Material_ID, Quantity, Note)
VALUES 
    (2, 1, 1.00, N'1 gói mì Hảo Hảo trụng vừa chín'),
    (2, 5, 80.00, N'80g tôm mực xào lửa lớn');

-- Product 5: Cà phê sữa đá (25g cafe, 0.08 lon sữa)
INSERT INTO dbo.Bill_Of_Materials (Product_ID, Material_ID, Quantity, Note)
VALUES 
    (5, 7, 25.00, N'25g cà phê pha phin'),
    (5, 8, 0.08, N'Định mức ~30g sữa đặc');

-- Product 7: Mì tôm 2 trứng xúc xích (1 gói mì, 2 trứng, 1 xúc xích)
INSERT INTO dbo.Bill_Of_Materials (Product_ID, Material_ID, Quantity, Note)
VALUES 
    (7, 1, 1.00, N'1 gói mì Hảo Hảo'),
    (7, 2, 2.00, N'2 quả trứng chiên lòng đào'),
    (7, 3, 1.00, N'1 cây xúc xích rán khía hoa');

-- Product 8: Cơm rang dưa bò (100g thịt bò, 1 quả trứng)
INSERT INTO dbo.Bill_Of_Materials (Product_ID, Material_ID, Quantity, Note)
VALUES 
    (8, 4, 100.00, N'100g thịt bò xào dưa chua'),
    (8, 2, 1.00, N'1 quả trứng trộn cơm rang vàng giòn');

-- Product 9: Bánh mì thập cẩm (1 ổ bánh mì, 1 trứng, 1 xúc xích)
INSERT INTO dbo.Bill_Of_Materials (Product_ID, Material_ID, Quantity, Note)
VALUES 
    (9, 6, 1.00, N'1 ổ bánh mì nướng giòn'),
    (9, 2, 1.00, N'1 quả trứng ốp la'),
    (9, 3, 1.00, N'1 cây xúc xích chiên');

-- Product 10: Xúc xích nướng tiêu (1 cây xúc xích)
INSERT INTO dbo.Bill_Of_Materials (Product_ID, Material_ID, Quantity, Note)
VALUES 
    (10, 3, 1.00, N'1 cây xúc xích nướng rắc tiêu đen');
GO

-- Seed 2 Phiếu đề nghị xuất NVL mẫu (1 Đã duyệt, 1 Chờ duyệt)
SET IDENTITY_INSERT dbo.Material_Requisitions ON;
INSERT INTO dbo.Material_Requisitions (Requisition_ID, Requisition_Code, Requested_By, Shift_ID, Target_Product_ID, Target_Quantity, Purpose, Status, Approved_By, Approved_Date, Note)
VALUES 
    (1, N'REQ-202610-001', 3, 1, 7, 10, N'Xuất NVL chế biến Mì tôm 2 trứng xúc xích phục vụ ca sáng', 'Approved', 2, DATEADD(hour, -3, GETDATE()), N'Đã kiểm tra kho và cấp đủ nguyên liệu'),
    (2, N'REQ-202610-002', 3, 2, 1, 15, N'Xuất NVL pha chế Trà sữa trân châu phục vụ giải đấu chiều', 'Pending', NULL, NULL, N'Chuẩn bị nguyên liệu cho giải đấu game');
SET IDENTITY_INSERT dbo.Material_Requisitions OFF;
GO

INSERT INTO dbo.Material_Requisition_Details (Requisition_ID, Material_ID, Requested_Quantity, Approved_Quantity)
VALUES 
    (1, 1, 10.00, 10.00), -- 10 gói mì
    (1, 2, 20.00, 20.00), -- 20 quả trứng
    (1, 3, 10.00, 10.00), -- 10 cây xúc xích
    (2, 9, 300.00, NULL),  -- 300g trà đen
    (2, 8, 1.20, NULL),    -- 1.2 lon sữa
    (2, 10, 750.00, NULL); -- 750g trân châu
GO

PRINT 'MRP Extension applied successfully!';
GO
