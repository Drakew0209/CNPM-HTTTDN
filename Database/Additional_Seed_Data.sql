-- =============================================
-- Additional Demo Seed Data for InternetCafeDB
-- File: Additional_Seed_Data.sql
-- Yeu cau: 5 KH moi, 10 phien choi, 5 don hang F&B,
--          du lieu cham cong, tinh luong
-- RDBMS: Microsoft SQL Server
-- =============================================

USE InternetCafeDB;
GO

-- =============================================
-- SECTION 1: Them Membership Tier moi
-- =============================================
-- Kiem tra va them tier Silver, Gold neu chua co
IF NOT EXISTS (SELECT 1 FROM dbo.Membership_Tier WHERE Tier_Name = 'Silver')
    INSERT INTO dbo.Membership_Tier (Tier_Name, Discount_Rate) VALUES ('Silver', 5.00);
IF NOT EXISTS (SELECT 1 FROM dbo.Membership_Tier WHERE Tier_Name = 'Gold')
    INSERT INTO dbo.Membership_Tier (Tier_Name, Discount_Rate) VALUES ('Gold', 10.00);
GO

-- =============================================
-- SECTION 2: 5 Khach hang moi
-- (Gia su Tier_ID: 1=Member, 2=VIP, 3=Silver, 4=Gold)
-- =============================================
INSERT INTO dbo.Customers (Username, Password_Hash, Full_Name, Date_Of_Birth, Hobbies, Phone_Number, Balance, Tier_ID, Status)
VALUES
    ('user03', 'hash$2a$10$abc003', N'Lê Minh Tuấn',   '2001-03-22', N'Game Battle Royale, Cà phê đen', '0903333333', 250000, 1, 'Active'),
    ('user04', 'hash$2a$10$abc004', N'Nguyễn Thị Hoa',  '1997-08-14', N'MMORPG, Trà đào cam sả',          '0904444444', 500000, 2, 'Active'),
    ('user05', 'hash$2a$10$abc005', N'Trần Văn Dũng',   '2003-11-05', N'FPS, Game đối kháng, Gà rán',     '0905555555', 150000, 1, 'Active'),
    ('user06', 'hash$2a$10$abc006', N'Phạm Thị Lan',    '1999-06-30', N'MOBA, Boba tea, Đọc truyện',      '0906666666', 750000, 3, 'Active'),
    ('user07', 'hash$2a$10$abc007', N'Hoàng Văn Khoa',  '2000-01-18', N'Esport, Streamer, Cafe sữa đá',   '0907777777', 1200000, 4, 'Active');
GO

-- =============================================
-- SECTION 3: Them may tinh moi de co du may cho 10 phien
-- =============================================
INSERT INTO dbo.Computers (Computer_Code, Zone_Type, Hourly_Rate, Status)
VALUES
    ('PC03', 'Standard', 8000,  'Available'),
    ('PC04', 'Standard', 8000,  'Available'),
    ('PC05', 'VIP',      15000, 'Available'),
    ('PC06', 'VIP',      15000, 'Available'),
    ('PC07', 'Standard', 8000,  'Available');
GO

-- =============================================
-- SECTION 4: 10 Phien choi (Usage_Sessions) day du gio bat dau/ket thuc va tien
-- Customer IDs: 1,2 (cu), 3,4,5,6,7 (moi sau IDENTITY)
-- Computer IDs: 1,2 (cu), 3,4,5,6,7 (moi)
-- Employee ID: 2 (emp02 - Nhan vien thu ngan)
-- =============================================

-- Phien 2: KH user01 (ID=1), PC02 VIP, 3 gio, 15000/h = 45000
INSERT INTO dbo.Usage_Sessions (Customer_ID, Computer_ID, Employee_ID, Start_Time, End_Time, Start_Balance, Applied_Hourly_Rate, Amount, Status)
VALUES (1, 2, 2, '2026-09-20 09:00', '2026-09-20 12:00', 100000, 15000, 45000, 'Completed');

-- Phien 3: KH user02 (ID=2), PC01 Standard, 2.5 gio, 8000/h = 20000
INSERT INTO dbo.Usage_Sessions (Customer_ID, Computer_ID, Employee_ID, Start_Time, End_Time, Start_Balance, Applied_Hourly_Rate, Amount, Status)
VALUES (2, 1, 2, '2026-09-20 14:00', '2026-09-20 16:30', 50000, 8000, 20000, 'Completed');

-- Phien 4: KH user03 (ID=3), PC03 Standard, 4 gio, 8000/h = 32000
INSERT INTO dbo.Usage_Sessions (Customer_ID, Computer_ID, Employee_ID, Start_Time, End_Time, Start_Balance, Applied_Hourly_Rate, Amount, Status)
VALUES (3, 3, 2, '2026-09-21 08:00', '2026-09-21 12:00', 250000, 8000, 32000, 'Completed');

-- Phien 5: KH user04 (ID=4), PC05 VIP, 5 gio, 15000/h = 75000
INSERT INTO dbo.Usage_Sessions (Customer_ID, Computer_ID, Employee_ID, Start_Time, End_Time, Start_Balance, Applied_Hourly_Rate, Amount, Status)
VALUES (4, 5, 2, '2026-09-21 10:00', '2026-09-21 15:00', 500000, 15000, 75000, 'Completed');

-- Phien 6: KH user05 (ID=5), PC04 Standard, 1.5 gio, 8000/h = 12000
INSERT INTO dbo.Usage_Sessions (Customer_ID, Computer_ID, Employee_ID, Start_Time, End_Time, Start_Balance, Applied_Hourly_Rate, Amount, Status)
VALUES (5, 4, 2, '2026-09-22 13:00', '2026-09-22 14:30', 150000, 8000, 12000, 'Completed');

-- Phien 7: KH user06 (ID=6), PC06 VIP, 6 gio, 15000/h = 90000
INSERT INTO dbo.Usage_Sessions (Customer_ID, Computer_ID, Employee_ID, Start_Time, End_Time, Start_Balance, Applied_Hourly_Rate, Amount, Status)
VALUES (6, 6, 2, '2026-09-22 18:00', '2026-09-23 00:00', 750000, 15000, 90000, 'Completed');

-- Phien 8: KH user07 (ID=7), PC07 Standard, 3 gio, 8000/h = 24000
INSERT INTO dbo.Usage_Sessions (Customer_ID, Computer_ID, Employee_ID, Start_Time, End_Time, Start_Balance, Applied_Hourly_Rate, Amount, Status)
VALUES (7, 7, 2, '2026-09-23 09:00', '2026-09-23 12:00', 1200000, 8000, 24000, 'Completed');

-- Phien 9: KH user03 (ID=3), PC05 VIP, 2 gio, 15000/h = 30000
INSERT INTO dbo.Usage_Sessions (Customer_ID, Computer_ID, Employee_ID, Start_Time, End_Time, Start_Balance, Applied_Hourly_Rate, Amount, Status)
VALUES (3, 5, 2, '2026-09-24 20:00', '2026-09-24 22:00', 218000, 15000, 30000, 'Completed');

-- Phien 10: KH user01 (ID=1), PC01 Standard, 5 gio, 8000/h = 40000
INSERT INTO dbo.Usage_Sessions (Customer_ID, Computer_ID, Employee_ID, Start_Time, End_Time, Start_Balance, Applied_Hourly_Rate, Amount, Status)
VALUES (1, 1, 2, '2026-09-25 08:00', '2026-09-25 13:00', 150000, 8000, 40000, 'Completed');

-- Phien 11: KH user07 (ID=7), PC06 VIP, dang choi (Active, End_Time = NULL)
INSERT INTO dbo.Usage_Sessions (Customer_ID, Computer_ID, Employee_ID, Start_Time, End_Time, Start_Balance, Applied_Hourly_Rate, Amount, Status)
VALUES (7, 6, 2, '2026-09-29 19:00', NULL, 1176000, 15000, NULL, 'Active');
GO

-- =============================================
-- SECTION 5: Them san pham moi cho don hang da dang
-- =============================================
INSERT INTO dbo.Products (Category_ID, Product_Name, Price, Stock_Quantity)
VALUES
    (1, N'Cà phê sữa đá',     20000, 0),
    (1, N'Trà đào cam sả',    30000, 0),
    (1, N'Nước ngọt Pepsi',   15000, 0),
    (2, N'Gà rán (3 miếng)',  45000, 0),
    (2, N'Cơm rang dưa bò',   40000, 0),
    (2, N'Bánh mì thịt nguội',25000, 0);
GO

-- Nhập tồn trước khi tạo đơn hàng; trigger bán hàng sẽ trừ từ tồn kho đã nhập.
INSERT INTO dbo.Inventory_Transactions (Product_ID, Employee_ID, Trans_Type, Quantity, Note)
VALUES
    (3, 1, 'Import', 100, N'Nhập cà phê sữa đá - NCC Highlands'),
    (4, 1, 'Import',  80, N'Nhập trà đào cam sả - NCC Gong Cha'),
    (5, 1, 'Import', 150, N'Nhập Pepsi - NCC Coca-Cola VN'),
    (6, 1, 'Import',  40, N'Nhập gà rán - NCC KFC Supply'),
    (7, 1, 'Import',  35, N'Nhập nguyên liệu cơm rang'),
    (8, 1, 'Import',  60, N'Nhập bánh mì thịt nguội');
GO

-- =============================================
-- SECTION 6: 5 Don hang F&B (Orders + Order_Details)
-- =============================================

-- Don hang 2: KH user03 (ID=3), PC03, NV emp02 (ID=2)
INSERT INTO dbo.Orders (Customer_ID, Computer_ID, Employee_ID, Total_Amount, Status)
VALUES (3, 3, 2, 65000, 'Served');

INSERT INTO dbo.Order_Details (Order_ID, Product_ID, Quantity, Unit_Price)
VALUES
    (2, 3, 1, 20000),  -- Ca phe sua da
    (2, 6, 1, 45000);  -- Ga ran

-- Don hang 3: KH user04 (ID=4), PC05, NV emp02 (ID=2)
-- Tra dao cam sa x2 (2*30000=60000) + Com rang dua bo (1*40000=40000) = 100000
INSERT INTO dbo.Orders (Customer_ID, Computer_ID, Employee_ID, Total_Amount, Status)
VALUES (4, 5, 2, 100000, 'Served');

INSERT INTO dbo.Order_Details (Order_ID, Product_ID, Quantity, Unit_Price)
VALUES
    (3, 4, 2, 30000),  -- Tra dao cam sa x2 (Product_ID=4)
    (3, 7, 1, 40000);  -- Com rang dua bo (Product_ID=7)

-- Don hang 4: KH user01 (ID=1), PC01, NV emp02 (ID=2)
-- Nuoc ngot Pepsi (1*15000) + Banh mi (1*25000) + Ca phe sua da (1*20000) = 60000
INSERT INTO dbo.Orders (Customer_ID, Computer_ID, Employee_ID, Total_Amount, Status)
VALUES (1, 1, 2, 60000, 'Served');

INSERT INTO dbo.Order_Details (Order_ID, Product_ID, Quantity, Unit_Price)
VALUES
    (4, 5, 1, 15000),  -- Nuoc ngot Pepsi        (Product_ID=5)
    (4, 8, 1, 25000),  -- Banh mi thit nguoi      (Product_ID=8)
    (4, 3, 1, 20000);  -- Ca phe sua da           (Product_ID=3)

-- Don hang 5: KH user06 (ID=6), PC06, NV emp02 (ID=2) - Pending
INSERT INTO dbo.Orders (Customer_ID, Computer_ID, Employee_ID, Total_Amount, Status)
VALUES (6, 6, 2, 75000, 'Preparing');

INSERT INTO dbo.Order_Details (Order_ID, Product_ID, Quantity, Unit_Price)
VALUES
    (5, 6, 1, 45000),  -- Ga ran
    (5, 4, 1, 30000);  -- Tra dao cam sa

-- Don hang 6: KH user07 (ID=7), PC07, NV emp02 (ID=2)
INSERT INTO dbo.Orders (Customer_ID, Computer_ID, Employee_ID, Total_Amount, Status)
VALUES (7, 7, 2, 80000, 'Served');

INSERT INTO dbo.Order_Details (Order_ID, Product_ID, Quantity, Unit_Price)
VALUES
    (6, 7, 2, 40000);  -- Com rang dua bo x2
GO

-- =============================================
-- SECTION 7: Them Combo moi
-- =============================================
INSERT INTO dbo.Combos (Combo_Name, Price, Bonus_Balance, Description)
VALUES
    (N'Nạp 100k tặng 30k',  100000, 30000, N'Combo tiết kiệm cho khách thường xuyên'),
    (N'Nạp 200k tặng 80k',  200000, 80000, N'Combo VIP cuối tuần'),
    (N'Nạp 500k tặng 200k', 500000, 200000, N'Combo tháng - dành cho game thủ chuyên nghiệp');
GO

-- =============================================
-- SECTION 8: Giao dich (Transactions) - Nap tien TopUp cho KH moi
-- =============================================
-- KH user03 nap 100k combo (Combo_ID = 2: Nap 100k tang 30k)
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Combo_ID, Trans_Type, Amount)
VALUES (3, 2, 2, 'TopUp', 100000);

-- KH user04 nap 200k combo (Combo_ID = 3: Nap 200k tang 80k)
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Combo_ID, Trans_Type, Amount)
VALUES (4, 2, 3, 'TopUp', 200000);

-- KH user05 nap 50k combo (Combo_ID = 1: Nap 50k tang 20k - combo goc)
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Combo_ID, Trans_Type, Amount)
VALUES (5, 2, 1, 'TopUp', 50000);

-- KH user06 nap 200k combo (Combo_ID = 3)
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Combo_ID, Trans_Type, Amount)
VALUES (6, 2, 3, 'TopUp', 200000);

-- KH user07 nap 500k combo (Combo_ID = 4: Nap 500k tang 200k)
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Combo_ID, Trans_Type, Amount)
VALUES (7, 2, 4, 'TopUp', 500000);

-- Giao dich Rental cho cac phien choi da hoan tat
-- Session_ID 2 = Phien KH1/PC2, Amount = 45000
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Session_ID, Trans_Type, Amount)
VALUES (1, 2, 2, 'Rental', 45000);

-- Session_ID 3 = Phien KH2/PC1, Amount = 20000
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Session_ID, Trans_Type, Amount)
VALUES (2, 2, 3, 'Rental', 20000);

-- Session_ID 4 = Phien KH3/PC3, Amount = 32000
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Session_ID, Trans_Type, Amount)
VALUES (3, 2, 4, 'Rental', 32000);

-- Session_ID 5 = Phien KH4/PC5, Amount = 75000
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Session_ID, Trans_Type, Amount)
VALUES (4, 2, 5, 'Rental', 75000);

-- Session_ID 6 = Phien KH5/PC4, Amount = 12000
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Session_ID, Trans_Type, Amount)
VALUES (5, 2, 6, 'Rental', 12000);

-- Giao dich FoodOrder
-- Order 2 - KH user03, Amount = 65000
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Order_ID, Trans_Type, Amount)
VALUES (3, 2, 2, 'FoodOrder', 65000);

-- Order 3 - KH user04, Amount = 100000 (khớp Order_Details)
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Order_ID, Trans_Type, Amount)
VALUES (4, 2, 3, 'FoodOrder', 100000);

-- Order 4 - KH user01, Amount = 60000 (khớp Order_Details)
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Order_ID, Trans_Type, Amount)
VALUES (1, 2, 4, 'FoodOrder', 60000);

-- Order 6 - KH user07, Amount = 80000
INSERT INTO dbo.Transactions (Customer_ID, Processed_By, Order_ID, Trans_Type, Amount)
VALUES (7, 2, 6, 'FoodOrder', 80000);
GO

-- =============================================
-- SECTION 9: Them nhan vien moi de co du du lieu HRM
-- =============================================
INSERT INTO dbo.Employees (Username, Password_Hash, Full_Name, Date_Of_Birth, Gender, Phone_Number, Email, Position_ID, Base_Salary, Status)
VALUES
    ('emp03', 'hash$2a$10$empC', N'Đỗ Quang Hải',    '1996-04-12', N'Nam', '0908888888', 'hai.do@cafe.vn',   2, 7000000, 'Active'),
    ('emp04', 'hash$2a$10$empD', N'Nguyễn Thị Mai',  '2000-09-03', N'Nữ', '0909999999', 'mai.nt@cafe.vn',  3, 6000000, 'Active'),
    ('emp05', 'hash$2a$10$empE', N'Vũ Tiến Mạnh',    '1994-07-25', N'Nam', '0910000000', 'manh.vt@cafe.vn', 1, 8500000, 'Active');
GO

-- =============================================
-- SECTION 10: Lich lam viec (Work_Schedules) cho nhieu ngay
-- =============================================
-- Thang 9/2026
INSERT INTO dbo.Work_Schedules (Employee_ID, Shift_ID, Work_Date, Status)
VALUES
    -- emp01 (ID=1) - Quan ly ca
    (1, 2, '2026-09-17', 'Completed'),
    (1, 2, '2026-09-18', 'Completed'),
    (1, 2, '2026-09-19', 'Completed'),
    (1, 2, '2026-09-22', 'Completed'),
    (1, 2, '2026-09-23', 'Completed'),

    -- emp02 (ID=2) - Thu ngan
    (2, 1, '2026-09-17', 'Completed'),
    (2, 1, '2026-09-18', 'Completed'),
    (2, 1, '2026-09-19', 'Completed'),
    (2, 1, '2026-09-22', 'Completed'),
    (2, 1, '2026-09-23', 'Completed'),

    -- emp03 (ID=3) - Ky thuat vien
    (3, 3, '2026-09-17', 'Completed'),
    (3, 3, '2026-09-18', 'Completed'),
    (3, 3, '2026-09-19', 'Absent'),
    (3, 3, '2026-09-22', 'Completed'),
    (3, 3, '2026-09-23', 'Completed'),

    -- emp04 (ID=4) - Thu ngan
    (4, 2, '2026-09-17', 'Completed'),
    (4, 2, '2026-09-18', 'Completed'),
    (4, 2, '2026-09-19', 'Completed'),
    (4, 2, '2026-09-22', 'OnLeave'),
    (4, 2, '2026-09-23', 'Completed'),

    -- emp05 (ID=5) - Quan ly ca
    (5, 1, '2026-09-17', 'Completed'),
    (5, 1, '2026-09-18', 'Completed'),
    (5, 1, '2026-09-19', 'Completed'),
    (5, 1, '2026-09-22', 'Completed'),
    (5, 1, '2026-09-23', 'Completed');
GO

-- =============================================
-- SECTION 11: Du lieu Cham cong (Attendance)
-- Schedule_ID bat dau tu 3 (2 cai goc da co ID 1, 2)
-- =============================================
-- Luu y: Work_Schedules duoc insert theo thu tu, ID tu dong tang
-- Lich ID: 3..27 tuong ung voi cac schedule moi insert o Section 10
-- emp01 - Ca chieu (14:00-22:00)
INSERT INTO dbo.Attendance (Schedule_ID, Check_In_Time, Check_Out_Time, Note)
VALUES
    (3,  '2026-09-17 14:02', '2026-09-17 22:00', NULL),
    (4,  '2026-09-18 14:00', '2026-09-18 22:05', NULL),
    (5,  '2026-09-19 14:10', '2026-09-19 22:00', N'Vào trễ 10 phút'),
    (6,  '2026-09-22 14:00', '2026-09-22 22:00', NULL),
    (7,  '2026-09-23 14:00', '2026-09-23 22:00', NULL),
-- emp02 - Ca sang (06:00-14:00)
    (8,  '2026-09-17 06:05', '2026-09-17 14:00', NULL),
    (9,  '2026-09-18 06:00', '2026-09-18 14:00', NULL),
    (10, '2026-09-19 06:00', '2026-09-19 14:00', NULL),
    (11, '2026-09-22 06:00', '2026-09-22 14:00', NULL),
    (12, '2026-09-23 06:05', '2026-09-23 14:00', NULL),
-- emp03 - Ca toi (22:00-06:00) -- Ngay 19 vang mat khong cham cong
    (13, '2026-09-17 22:00', '2026-09-18 06:00', NULL),
    (14, '2026-09-18 22:05', '2026-09-19 06:00', NULL),
    -- Schedule_ID 15 (2026-09-19) bi Absent, khong co ban ghi Attendance
    (16, '2026-09-22 22:00', '2026-09-23 06:00', NULL),
    (17, '2026-09-23 22:10', '2026-09-24 06:00', N'Vào trễ 10 phút'),
-- emp04 - Ca chieu (14:00-22:00) -- Ngay 22 nghi phep
    (18, '2026-09-17 14:00', '2026-09-17 22:00', NULL),
    (19, '2026-09-18 14:00', '2026-09-18 22:00', NULL),
    (20, '2026-09-19 14:00', '2026-09-19 22:00', NULL),
    -- Schedule_ID 21 (2026-09-22) bi OnLeave, khong co ban ghi Attendance
    (22, '2026-09-23 14:00', '2026-09-23 22:00', NULL),
-- emp05 - Ca sang (06:00-14:00)
    (23, '2026-09-17 06:00', '2026-09-17 14:00', NULL),
    (24, '2026-09-18 06:00', '2026-09-18 14:00', NULL),
    (25, '2026-09-19 06:00', '2026-09-19 14:00', NULL),
    (26, '2026-09-22 06:00', '2026-09-22 14:00', NULL),
    (27, '2026-09-23 06:00', '2026-09-23 14:00', NULL);
GO

-- =============================================
-- SECTION 12: Don xin nghi phep (Leave_Requests)
-- =============================================
INSERT INTO dbo.Leave_Requests (Employee_ID, Leave_Type, Start_Date, End_Date, Reason, Status, Approved_By)
VALUES
    -- emp03 vang ngay 19/09 (Sick)
    (3, 'Sick',   '2026-09-19', '2026-09-19', N'Đau đầu, sốt nhẹ',            'Approved', 1),
    -- emp04 nghi ngay 22/09 (Annual)
    (4, 'Annual', '2026-09-22', '2026-09-22', N'Việc gia đình',                 'Approved', 1),
    -- emp05 xin nghi sap toi (Pending)
    (5, 'Annual', '2026-10-05', '2026-10-07', N'Du lịch gia đình cuối năm',     'Pending',  NULL),
    -- emp01 xin nghi (Rejected)
    (1, 'Unpaid', '2026-10-10', '2026-10-12', N'Có việc cá nhân',               'Rejected', 5);
GO

-- =============================================
-- SECTION 13: Bang luong thang 9/2026 (Payroll)
-- =============================================
INSERT INTO dbo.Payroll (Employee_ID, Pay_Month, Pay_Year, Base_Salary, Bonus, Deduction, Status, Payment_Date)
VALUES
    -- emp01 (ID=1): Luong 8tr, thuong 1tr (hoan thanh KPI)
    (1, 9, 2026, 8000000, 1000000, 0,      'Paid', '2026-10-05 10:00'),
    -- emp02 (ID=2): Luong 6tr, khau tru 0 (di lam day du)
    (2, 9, 2026, 6000000, 300000,  0,      'Paid', '2026-10-05 10:00'),
    -- emp03 (ID=3): Luong 7tr, khau tru 1 ngay vang (7tr/26 ngay ~ 269k)
    (3, 9, 2026, 7000000, 0,       269000, 'Paid', '2026-10-05 10:00'),
    -- emp04 (ID=4): Luong 6tr, nghi phep nam duoc chap thuan, khong khau tru
    (4, 9, 2026, 6000000, 0,       0,      'Paid', '2026-10-05 10:00'),
    -- emp05 (ID=5): Luong 8.5tr, thuong 1.5tr (nhan vien xuat sac thang)
    (5, 9, 2026, 8500000, 1500000, 0,      'Paid', '2026-10-05 10:00');
GO

-- =============================================
-- SECTION 14: Khao sat va phan hoi bo sung
-- =============================================
-- Them khao sat moi
INSERT INTO dbo.Surveys (Title, Description, Created_By)
VALUES (N'Khảo sát hài lòng dịch vụ tháng 9', N'Đánh giá chất lượng phục vụ và cơ sở vật chất', 1);

INSERT INTO dbo.Survey_Questions (Survey_ID, Question_Text, Question_Type, Options)
VALUES
    (2, N'Bạn đánh giá chất lượng máy tính tại quán?',          'Choice', N'Rất tốt, Tốt, Bình thường, Kém'),
    (2, N'Bạn đánh giá thái độ phục vụ của nhân viên?',         'Choice', N'Rất tốt, Tốt, Bình thường, Kém'),
    (2, N'Bạn có muốn mở thêm khu VIP premium không?',          'Choice', N'Có, Không, Cần cân nhắc thêm'),
    (2, N'Góp ý hoặc mong muốn khác của bạn?',                  'Text',   NULL);

-- Cau tra loi khao sat tu KH moi
INSERT INTO dbo.Survey_Responses (Survey_ID, Customer_ID, Question_ID, Answer_Text)
VALUES
    (2, 3, 3, N'Tốt'),
    (2, 3, 4, N'Rất tốt'),
    (2, 3, 5, N'Có'),
    (2, 3, 6, N'Mong quán thêm ghế massage'),
    (2, 4, 3, N'Rất tốt'),
    (2, 4, 4, N'Rất tốt'),
    (2, 4, 5, N'Có'),
    (2, 4, 6, N'Quán rất ổn, giữ nguyên chất lượng'),
    (2, 5, 3, N'Bình thường'),
    (2, 5, 4, N'Tốt'),
    (2, 5, 5, N'Cần cân nhắc thêm'),
    (2, 5, 6, N'Mong giảm giá cho sinh viên');

-- Them phan hoi/khieu nai bo sung
INSERT INTO dbo.Feedback (Customer_ID, Handled_By, Subject, Content, Status, Manager_Notes)
VALUES
    (3, 2, N'Máy PC03 bàn phím bị liệt phím',
     N'Phím Space và Enter của PC03 không hoạt động, ảnh hưởng game FPS.',
     'Resolved', N'Đã thay bàn phím mới ngày 22/09'),

    (4, NULL, N'Muốn thêm món Phở vào menu',
     N'Khu VIP nên có thêm món ăn đêm như phở bò, bún bò để phục vụ chơi khuya.',
     'Pending', NULL),

    (7, 2, N'Tốc độ internet khu Standard chậm hơn VIP',
     N'Băng thông khu Standard hơi chậm khi đông người, mong quán nâng cấp đường truyền.',
     'Reviewed', N'Đã ghi nhận, lên kế hoạch nâng cấp Q4/2026');
GO

-- =============================================
-- KIEM TRA DU LIEU SAU KHI THEM
-- =============================================
/*
-- Xem tong quan khach hang va so du
SELECT Customer_ID, Username, Full_Name, Balance, Tier_ID, Status
FROM dbo.Customers ORDER BY Customer_ID;

-- Xem 10 phien choi vua them
SELECT Session_ID, Customer_ID, Computer_ID, Start_Time, End_Time,
       Total_Hours, Amount, Status
FROM dbo.Usage_Sessions ORDER BY Session_ID;

-- Xem bang luong thang 9
SELECT p.Payroll_ID, e.Full_Name, p.Pay_Month, p.Pay_Year,
       p.Base_Salary, p.Bonus, p.Deduction, p.Net_Salary, p.Status
FROM dbo.Payroll p
JOIN dbo.Employees e ON p.Employee_ID = e.Employee_ID
ORDER BY p.Employee_ID;

-- Xem cham cong
SELECT a.Attendance_ID, e.Full_Name, ws.Work_Date, sh.Shift_Name,
       a.Check_In_Time, a.Check_Out_Time, ws.Status, a.Note
FROM dbo.Attendance a
JOIN dbo.Work_Schedules ws ON a.Schedule_ID = ws.Schedule_ID
JOIN dbo.Employees e ON ws.Employee_ID = e.Employee_ID
JOIN dbo.Work_Shifts sh ON ws.Shift_ID = sh.Shift_ID
ORDER BY ws.Work_Date, e.Employee_ID;

-- Xem don hang va chi tiet
SELECT o.Order_ID, c.Full_Name AS Customer, o.Order_Date, o.Total_Amount, o.Status,
       p.Product_Name, od.Quantity, od.Unit_Price, od.Line_Total
FROM dbo.Orders o
JOIN dbo.Customers c ON o.Customer_ID = c.Customer_ID
JOIN dbo.Order_Details od ON o.Order_ID = od.Order_ID
JOIN dbo.Products p ON od.Product_ID = p.Product_ID
ORDER BY o.Order_ID;
*/
-- =============================================
-- SECTION 16: Kiem tra bien nhan TopUp (TopUp_Receipts)
-- Bien nhan duoc tu dong tao boi trigger TR_Create_TopUp_Receipt
-- khi co INSERT vao Transactions voi Trans_Type = 'TopUp'.
-- Cac giao dich TopUp trong Section 8 se tu sinh bien nhan tuong ung.
-- =============================================
/*
-- Xem toan bo bien nhan nap tien kem thong tin KH, thu ngan, combo
SELECT
    r.Receipt_ID,
    r.Receipt_Code,
    c.Username              AS Customer_Username,
    c.Full_Name             AS Customer_Name,
    e.Full_Name             AS Cashier_Name,
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
ORDER BY r.Trans_Date DESC;

-- Tra cuu bien nhan theo ma Receipt_Code
-- Vi du: bien nhan giao dich TopUp dau tien (Transaction_ID = 4 trong Additional data)
-- SELECT * FROM dbo.TopUp_Receipts WHERE Receipt_Code = 'RCP-20261005-4';

-- Xem so bien nhan da tao theo tung khach hang
SELECT c.Username, c.Full_Name, COUNT(r.Receipt_ID) AS Total_TopUps,
       SUM(r.Paid_Amount) AS Total_Paid, SUM(r.Total_Credited) AS Total_Credited
FROM dbo.TopUp_Receipts r
JOIN dbo.Customers c ON r.Customer_ID = c.Customer_ID
GROUP BY c.Username, c.Full_Name
ORDER BY Total_Paid DESC;
*/
