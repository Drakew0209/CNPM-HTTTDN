namespace InternetCafe.Backend.Data;

/// <summary>
/// MRP: nguyên vật liệu, định mức thành phẩm (BOM), lệnh sản xuất và phiếu đề nghị xuất kho.
/// Mỗi câu chạy riêng một lượt vì SQL Server biên dịch trọn batch trước khi thi hành —
/// bảng sau tham chiếu khoá ngoại tới bảng vừa tạo trong cùng batch sẽ không biên dịch được.
/// Toàn bộ câu lệnh đều idempotent nên khởi động lại API không phá dữ liệu đang có.
/// </summary>
public static class MrpSql
{
    public static readonly string[] SchemaStatements =
    [
        """
        IF OBJECT_ID('dbo.Materials','U') IS NULL
        CREATE TABLE dbo.Materials (
            Material_ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Materials PRIMARY KEY,
            Material_Code NVARCHAR(30) NOT NULL CONSTRAINT UQ_Materials_Code UNIQUE,
            Material_Name NVARCHAR(150) NOT NULL,
            Unit NVARCHAR(20) NOT NULL CONSTRAINT DF_Materials_Unit DEFAULT N'cái',
            Stock_Quantity DECIMAL(12,2) NOT NULL CONSTRAINT DF_Materials_Stock DEFAULT 0,
            Reorder_Level DECIMAL(12,2) NOT NULL CONSTRAINT DF_Materials_Reorder DEFAULT 0,
            Unit_Cost DECIMAL(12,2) NOT NULL CONSTRAINT DF_Materials_Unit_Cost DEFAULT 0,
            Supplier_ID INT NULL,
            Status VARCHAR(20) NOT NULL CONSTRAINT DF_Materials_Status DEFAULT 'Active',
            Created_At DATETIME2 NOT NULL CONSTRAINT DF_Materials_Created_At DEFAULT SYSUTCDATETIME(),
            Updated_At DATETIME2 NOT NULL CONSTRAINT DF_Materials_Updated_At DEFAULT SYSUTCDATETIME(),
            CONSTRAINT CHK_Materials_Stock CHECK (Stock_Quantity >= 0),
            CONSTRAINT CHK_Materials_Status CHECK (Status IN ('Active','Inactive')),
            CONSTRAINT FK_Materials_Supplier FOREIGN KEY (Supplier_ID) REFERENCES dbo.Suppliers(Supplier_ID)
        );
        """,
        """
        IF OBJECT_ID('dbo.Material_Transactions','U') IS NULL
        CREATE TABLE dbo.Material_Transactions (
            Material_Trans_ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Material_Transactions PRIMARY KEY,
            Material_ID INT NOT NULL,
            Trans_Type VARCHAR(20) NOT NULL,
            Quantity DECIMAL(12,2) NOT NULL,
            Unit_Cost DECIMAL(12,2) NOT NULL CONSTRAINT DF_Material_Trans_Unit_Cost DEFAULT 0,
            Supplier_ID INT NULL,
            Note NVARCHAR(500) NULL,
            Reference NVARCHAR(60) NULL,
            Performed_By_Account_ID INT NULL,
            Created_At DATETIME2 NOT NULL CONSTRAINT DF_Material_Trans_Created_At DEFAULT SYSUTCDATETIME(),
            CONSTRAINT CHK_Material_Trans_Type CHECK (Trans_Type IN ('Import','Export','Produce')),
            CONSTRAINT CHK_Material_Trans_Quantity CHECK (Quantity > 0),
            CONSTRAINT FK_Material_Trans_Material FOREIGN KEY (Material_ID) REFERENCES dbo.Materials(Material_ID),
            CONSTRAINT FK_Material_Trans_Supplier FOREIGN KEY (Supplier_ID) REFERENCES dbo.Suppliers(Supplier_ID)
        );
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Material_Transactions_Material')
            CREATE INDEX IX_Material_Transactions_Material ON dbo.Material_Transactions(Material_ID, Created_At DESC);
        """,
        """
        IF OBJECT_ID('dbo.Product_Materials','U') IS NULL
        CREATE TABLE dbo.Product_Materials (
            Product_ID INT NOT NULL,
            Material_ID INT NOT NULL,
            Quantity DECIMAL(12,2) NOT NULL,
            CONSTRAINT PK_Product_Materials PRIMARY KEY (Product_ID, Material_ID),
            CONSTRAINT CHK_Product_Materials_Quantity CHECK (Quantity > 0),
            CONSTRAINT FK_Product_Materials_Product FOREIGN KEY (Product_ID) REFERENCES dbo.Products(Product_ID),
            CONSTRAINT FK_Product_Materials_Material FOREIGN KEY (Material_ID) REFERENCES dbo.Materials(Material_ID)
        );
        """,
        """
        IF OBJECT_ID('dbo.Production_Orders','U') IS NULL
        CREATE TABLE dbo.Production_Orders (
            Production_ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Production_Orders PRIMARY KEY,
            Product_ID INT NOT NULL,
            Quantity INT NOT NULL,
            Status VARCHAR(20) NOT NULL CONSTRAINT DF_Production_Orders_Status DEFAULT 'Requested',
            Note NVARCHAR(500) NULL,
            Requested_By_Account_ID INT NULL,
            Completed_By_Account_ID INT NULL,
            Created_At DATETIME2 NOT NULL CONSTRAINT DF_Production_Orders_Created_At DEFAULT SYSUTCDATETIME(),
            Completed_At DATETIME2 NULL,
            CONSTRAINT CHK_Production_Orders_Quantity CHECK (Quantity > 0),
            CONSTRAINT CHK_Production_Orders_Status CHECK (Status IN ('Requested','Completed','Cancelled')),
            CONSTRAINT FK_Production_Orders_Product FOREIGN KEY (Product_ID) REFERENCES dbo.Products(Product_ID)
        );
        """,
        """
        IF OBJECT_ID('dbo.Material_Requests','U') IS NULL
        CREATE TABLE dbo.Material_Requests (
            Request_ID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Material_Requests PRIMARY KEY,
            Item_Type VARCHAR(20) NOT NULL,
            Material_ID INT NULL,
            Product_ID INT NULL,
            Quantity DECIMAL(12,2) NOT NULL,
            Status VARCHAR(20) NOT NULL CONSTRAINT DF_Material_Requests_Status DEFAULT 'Pending',
            Note NVARCHAR(500) NULL,
            Requested_By_Account_ID INT NULL,
            Decided_By_Account_ID INT NULL,
            Decided_At DATETIME2 NULL,
            Created_At DATETIME2 NOT NULL CONSTRAINT DF_Material_Requests_Created_At DEFAULT SYSUTCDATETIME(),
            CONSTRAINT CHK_Material_Requests_Quantity CHECK (Quantity > 0),
            CONSTRAINT CHK_Material_Requests_Status CHECK (Status IN ('Pending','Approved','Rejected')),
            CONSTRAINT CHK_Material_Requests_Item CHECK (
                (Item_Type = 'Material' AND Material_ID IS NOT NULL AND Product_ID IS NULL) OR
                (Item_Type = 'Product'  AND Product_ID IS NOT NULL AND Material_ID IS NULL)),
            CONSTRAINT FK_Material_Requests_Material FOREIGN KEY (Material_ID) REFERENCES dbo.Materials(Material_ID),
            CONSTRAINT FK_Material_Requests_Product FOREIGN KEY (Product_ID) REFERENCES dbo.Products(Product_ID)
        );
        """,
    ];

    public static readonly string[] SeedStatements =
    [
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Materials WHERE Material_Code = N'NVL-MI')
            INSERT INTO dbo.Materials (Material_Code, Material_Name, Unit, Stock_Quantity, Reorder_Level, Unit_Cost, Supplier_ID)
            VALUES (N'NVL-MI', N'Mì gói Acecook', N'gói', 120, 30, 5000,
                (SELECT TOP 1 Supplier_ID FROM dbo.Suppliers WHERE Name LIKE N'%Acecook%'));
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Materials WHERE Material_Code = N'NVL-TRUNG')
            INSERT INTO dbo.Materials (Material_Code, Material_Name, Unit, Stock_Quantity, Reorder_Level, Unit_Cost, Supplier_ID)
            VALUES (N'NVL-TRUNG', N'Trứng gà', N'quả', 200, 40, 3000,
                (SELECT TOP 1 Supplier_ID FROM dbo.Suppliers WHERE Name LIKE N'%VISSAN%'));
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Materials WHERE Material_Code = N'NVL-XUCXICH')
            INSERT INTO dbo.Materials (Material_Code, Material_Name, Unit, Stock_Quantity, Reorder_Level, Unit_Cost, Supplier_ID)
            VALUES (N'NVL-XUCXICH', N'Xúc xích', N'cây', 150, 30, 6000,
                (SELECT TOP 1 Supplier_ID FROM dbo.Suppliers WHERE Name LIKE N'%VISSAN%'));
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Materials WHERE Material_Code = N'NVL-CAFE')
            INSERT INTO dbo.Materials (Material_Code, Material_Name, Unit, Stock_Quantity, Reorder_Level, Unit_Cost, Supplier_ID)
            VALUES (N'NVL-CAFE', N'Bột cà phê rang xay', N'kg', 20, 5, 180000,
                (SELECT TOP 1 Supplier_ID FROM dbo.Suppliers WHERE Name LIKE N'%Highlands%'));
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Materials WHERE Material_Code = N'NVL-SUA')
            INSERT INTO dbo.Materials (Material_Code, Material_Name, Unit, Stock_Quantity, Reorder_Level, Unit_Cost, Supplier_ID)
            VALUES (N'NVL-SUA', N'Sữa đặc', N'hộp', 40, 10, 22000,
                (SELECT TOP 1 Supplier_ID FROM dbo.Suppliers WHERE Name LIKE N'%Highlands%'));
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Materials WHERE Material_Code = N'NVL-TRA')
            INSERT INTO dbo.Materials (Material_Code, Material_Name, Unit, Stock_Quantity, Reorder_Level, Unit_Cost, Supplier_ID)
            VALUES (N'NVL-TRA', N'Trà đen khô', N'kg', 12, 3, 150000,
                (SELECT TOP 1 Supplier_ID FROM dbo.Suppliers WHERE Name LIKE N'%Highlands%'));
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Materials WHERE Material_Code = N'NVL-DA')
            INSERT INTO dbo.Materials (Material_Code, Material_Name, Unit, Stock_Quantity, Reorder_Level, Unit_Cost, Supplier_ID)
            VALUES (N'NVL-DA', N'Đá viên', N'kg', 60, 15, 8000, NULL);
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Materials WHERE Material_Code = N'NVL-BANHMI')
            INSERT INTO dbo.Materials (Material_Code, Material_Name, Unit, Stock_Quantity, Reorder_Level, Unit_Cost, Supplier_ID)
            VALUES (N'NVL-BANHMI', N'Bánh mì', N'ổ', 80, 20, 6000, NULL);
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Materials WHERE Material_Code = N'NVL-PEPSI')
            INSERT INTO dbo.Materials (Material_Code, Material_Name, Unit, Stock_Quantity, Reorder_Level, Unit_Cost, Supplier_ID)
            VALUES (N'NVL-PEPSI', N'Lon Pepsi', N'lon', 240, 48, 9000,
                (SELECT TOP 1 Supplier_ID FROM dbo.Suppliers WHERE Name LIKE N'%PepsiCo%'));
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Materials WHERE Material_Code = N'NVL-STING')
            INSERT INTO dbo.Materials (Material_Code, Material_Name, Unit, Stock_Quantity, Reorder_Level, Unit_Cost, Supplier_ID)
            VALUES (N'NVL-STING', N'Lon Sting dâu', N'lon', 200, 48, 9500,
                (SELECT TOP 1 Supplier_ID FROM dbo.Suppliers WHERE Name LIKE N'%PepsiCo%'));
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Product_Materials pm JOIN dbo.Products p ON p.Product_ID = pm.Product_ID
                       WHERE p.Product_Name = N'Mì tôm 2 trứng xúc xích')
           AND EXISTS (SELECT 1 FROM dbo.Products WHERE Product_Name = N'Mì tôm 2 trứng xúc xích')
        INSERT INTO dbo.Product_Materials (Product_ID, Material_ID, Quantity)
        SELECT p.Product_ID, m.Material_ID, v.Quantity
        FROM (VALUES (N'Mì gói Acecook', CAST(1 AS DECIMAL(12,2))),
                     (N'Trứng gà', CAST(2 AS DECIMAL(12,2))),
                     (N'Xúc xích', CAST(1 AS DECIMAL(12,2)))) AS v(Material_Name, Quantity)
        JOIN dbo.Products p ON p.Product_Name = N'Mì tôm 2 trứng xúc xích'
        JOIN dbo.Materials m ON m.Material_Name = v.Material_Name;
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Product_Materials pm JOIN dbo.Products p ON p.Product_ID = pm.Product_ID
                       WHERE p.Product_Name = N'Cà phê sữa đá')
           AND EXISTS (SELECT 1 FROM dbo.Products WHERE Product_Name = N'Cà phê sữa đá')
        INSERT INTO dbo.Product_Materials (Product_ID, Material_ID, Quantity)
        SELECT p.Product_ID, m.Material_ID, v.Quantity
        FROM (VALUES (N'Bột cà phê rang xay', CAST(0.02 AS DECIMAL(12,2))),
                     (N'Sữa đặc', CAST(0.5 AS DECIMAL(12,2))),
                     (N'Đá viên', CAST(0.2 AS DECIMAL(12,2)))) AS v(Material_Name, Quantity)
        JOIN dbo.Products p ON p.Product_Name = N'Cà phê sữa đá'
        JOIN dbo.Materials m ON m.Material_Name = v.Material_Name;
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Product_Materials pm JOIN dbo.Products p ON p.Product_ID = pm.Product_ID
                       WHERE p.Product_Name = N'Bánh mì thập cẩm')
           AND EXISTS (SELECT 1 FROM dbo.Products WHERE Product_Name = N'Bánh mì thập cẩm')
        INSERT INTO dbo.Product_Materials (Product_ID, Material_ID, Quantity)
        SELECT p.Product_ID, m.Material_ID, v.Quantity
        FROM (VALUES (N'Bánh mì', CAST(1 AS DECIMAL(12,2))),
                     (N'Xúc xích', CAST(1 AS DECIMAL(12,2))),
                     (N'Trứng gà', CAST(1 AS DECIMAL(12,2)))) AS v(Material_Name, Quantity)
        JOIN dbo.Products p ON p.Product_Name = N'Bánh mì thập cẩm'
        JOIN dbo.Materials m ON m.Material_Name = v.Material_Name;
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Product_Materials pm JOIN dbo.Products p ON p.Product_ID = pm.Product_ID
                       WHERE p.Product_Name = N'Nước ngọt Pepsi lon')
           AND EXISTS (SELECT 1 FROM dbo.Products WHERE Product_Name = N'Nước ngọt Pepsi lon')
        INSERT INTO dbo.Product_Materials (Product_ID, Material_ID, Quantity)
        SELECT p.Product_ID, m.Material_ID, 1
        FROM dbo.Products p JOIN dbo.Materials m ON m.Material_Name = N'Lon Pepsi'
        WHERE p.Product_Name = N'Nước ngọt Pepsi lon';
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Product_Materials pm JOIN dbo.Products p ON p.Product_ID = pm.Product_ID
                       WHERE p.Product_Name = N'Nước ngọt Sting dâu')
           AND EXISTS (SELECT 1 FROM dbo.Products WHERE Product_Name = N'Nước ngọt Sting dâu')
        INSERT INTO dbo.Product_Materials (Product_ID, Material_ID, Quantity)
        SELECT p.Product_ID, m.Material_ID, 1
        FROM dbo.Products p JOIN dbo.Materials m ON m.Material_Name = N'Lon Sting dâu'
        WHERE p.Product_Name = N'Nước ngọt Sting dâu';
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Product_Materials pm JOIN dbo.Products p ON p.Product_ID = pm.Product_ID
                       WHERE p.Product_Name = N'Trà sữa trân châu')
           AND EXISTS (SELECT 1 FROM dbo.Products WHERE Product_Name = N'Trà sữa trân châu')
        INSERT INTO dbo.Product_Materials (Product_ID, Material_ID, Quantity)
        SELECT p.Product_ID, m.Material_ID, v.Quantity
        FROM (VALUES (N'Trà đen khô', CAST(0.01 AS DECIMAL(12,2))),
                     (N'Sữa đặc', CAST(0.3 AS DECIMAL(12,2))),
                     (N'Đá viên', CAST(0.3 AS DECIMAL(12,2)))) AS v(Material_Name, Quantity)
        JOIN dbo.Products p ON p.Product_Name = N'Trà sữa trân châu'
        JOIN dbo.Materials m ON m.Material_Name = v.Material_Name;
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Product_Materials pm JOIN dbo.Products p ON p.Product_ID = pm.Product_ID
                       WHERE p.Product_Name = N'Mì xào hải sản')
           AND EXISTS (SELECT 1 FROM dbo.Products WHERE Product_Name = N'Mì xào hải sản')
        INSERT INTO dbo.Product_Materials (Product_ID, Material_ID, Quantity)
        SELECT p.Product_ID, m.Material_ID, v.Quantity
        FROM (VALUES (N'Mì gói Acecook', CAST(1 AS DECIMAL(12,2))),
                     (N'Trứng gà', CAST(1 AS DECIMAL(12,2)))) AS v(Material_Name, Quantity)
        JOIN dbo.Products p ON p.Product_Name = N'Mì xào hải sản'
        JOIN dbo.Materials m ON m.Material_Name = v.Material_Name;
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Production_Orders)
           AND EXISTS (SELECT 1 FROM dbo.Products WHERE Product_Name = N'Mì tôm 2 trứng xúc xích')
        INSERT INTO dbo.Production_Orders (Product_ID, Quantity, Status, Note)
        SELECT TOP 1 Product_ID, 20, 'Requested', N'Lô mì tôm cho ca tối'
        FROM dbo.Products WHERE Product_Name = N'Mì tôm 2 trứng xúc xích';
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM dbo.Material_Requests)
           AND EXISTS (SELECT 1 FROM dbo.Materials WHERE Material_Code = N'NVL-MI')
        INSERT INTO dbo.Material_Requests (Item_Type, Material_ID, Quantity, Status, Note)
        SELECT TOP 1 'Material', Material_ID, 20, 'Pending', N'Đề nghị xuất nguyên vật liệu cho ca sáng'
        FROM dbo.Materials WHERE Material_Code = N'NVL-MI';
        """,
    ];
}
