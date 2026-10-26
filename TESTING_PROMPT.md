# PROMPT KIỂM THỬ TOÀN DIỆN (END-TO-END TEST PROMPT)
## Hệ thống Quản trị Cyber Cafe - CNPM-HTTTDN

---

## 🎯 MỤC TIÊU
Kiểm thử **từ giao diện (UI) → nghiệp vụ (Business Logic) → tích hợp (Integration) → bảo mật (Security) → hiệu năng (Performance)** toàn bộ 3 ứng dụng: **Web Admin**, **Backend API**, **Desktop Client (WPF)**.

---

## 📋 PHẦN 1: KIỂM THỬ WEB ADMIN (React 19 + Ant Design 6)

### 1.1. Xác thực & Phân quyền (Authentication & Authorization)
```
[ ] TC-AUTH-01: Đăng nhập thành công với 6 vai trò (owner/admin/manager/cashier/staff/gamer)
[ ] TC-AUTH-02: Đăng nhập sai mật khẩu → hiển thị error "Tên đăng nhập hoặc mật khẩu không đúng"
[ ] TC-AUTH-03: Đăng nhập vai trò Customer (gamer) → bị chặn, hướng dẫn dùng Desktop Client
[ ] TC-AUTH-04: Đăng nhập vai trò Staff → redirect về /staff/home (Cổng nhân viên)
[ ] TC-AUTH-05: Đăng nhập vai trò Admin → menu "HỆ THỐNG" (4 mục: Tài khoản, Phân quyền, Game, Cấu hình)
[ ] TC-AUTH-06: Đăng nhập vai trò Manager/Cashier → menu "MANAGEMENT STUDIO" (Phòng máy, Thu ngân, Kho, HRM, CRM, Báo cáo)
[ ] TC-AUTH-07: F5.refresh trang → phiên đăng nhập duy trì (localStorage + sessionStorage)
[ ] TC-AUTH-08: Mở tab mới → tự động đăng nhập (sessionStorage)
[ ] TC-AUTH-09: Token hết hạn (24h) → tự động redirect login
[ ] TC-AUTH-10: Đăng xuất → xóa token, về trang login, không truy cập được /workspace
[ ] TC-AUTH-11: Thử truy cập /system/* với role Manager → 403 Forbidden
[ ] TC-AUTH-12: Thử truy cập /management/* với role Admin → 403 Forbidden (có chủ đích)
[ ] TC-AUTH-13: Brute-force login >10 lần/phút/IP → Rate Limited 429
```

### 1.2. Phòng máy & Quản lý phiên (Machines & Sessions)
```
[ ] TC-PC-01: Sơ đồ lưới máy hiển thị đúng 3 trạng thái: 🟢 Available, 🔵 InUse, 🟠 Maintenance
[ ] TC-PC-02: Filter máy theo Khu vực (Standard/VIP), Trạng thái, Tìm kiếm tên máy
[ ] TC-PC-03: Click máy → Drawer chi tiết: Heartbeat, Cước tạm tính, Lệnh điều khiển
[ ] TC-PC-04: Bật "Bảo trì" → máy chuyển Maintenance, không cho khách mở phiên
[ ] TC-PC-05: Gửi lệnh "Khóa máy" → Client WPF nhận lệnh realtime, khóa màn hình
[ ] TC-PC-06: Gửi lệnh "Tắt máy/Restart" → Client WPF shutdown/restart Windows
[ ] TC-PC-07: Chuyển máy (Transfer): PC01 đang chơi → chuyển sang PC03 → PC01 Available, PC03 InUse, số dư giữ nguyên
[ ] TC-PC-08: Heartbeat 5 phút → máy Offline nếu không nhận heartbeat (đèn xám)
[ ] TC-PC-09: Đổi giá giờ chơi realtime → máy đang chơi giữ giá cũ, máy mới áp dụng giá mới
```

### 1.3. Thu ngân & Ví khách hàng (Billing & Wallet)
```
[ ] TC-POS-01: Danh sách khách hàng: hiển thị Hạng (Member/Silver/Gold/Diamond), Số dư, Trạng thái
[ ] TC-POS-02: Nạp tiền mặt: chọn khách → nhập số tiền → ghi chú → Lưu → Số dư cộng realtime
[ ] TC-POS-03: Nạp VietQR: tạo mã QR động → hiển thị syntax "NAP {username}" → Thu ngân duyệt → Cộng tiền
[ ] TC-POS-04: Nạp combo khuyến mãi (50k tặng 15k, 100k tặng 35k...) → cộng đúng tiền + bonus
[ ] TC-POS-05: Hoàn tiền/Rút tiền: kiểm tra số dư khả dụng (đã trừ tiền giờ đang chơi) → từ chối nếu vượt
[ ] TC-POS-06: Danh sách đơn F&B: trạng thái Pending/Preparing/Served/Cancelled, lọc theo máy/khách
[ ] TC-POS-07: Duyệt đơn: Pending → Preparing → Served (trừ kho NVL theo BOM)
[ ] TC-POS-08: Hủy đơn: Served → Cancelled (không được), Pending → Cancelled (hoàn tiền + hoàn kho)
[ ] TC-POS-09: Thông báo realtime (SignalR toast) khi có yêu cầu nạp tiền/đơn F&B mới
```

### 1.4. MRP & Kho nguyên vật liệu (MRP & Inventory)
```
[ ] TC-MRP-01: Danh mục NVL: Mã, Tên, Đơn vị, Tồn kho, Reorder Level, Giá vốn, NCC
[ ] TC-MRP-02: Cảnh báo tồn kho thấp (🔴 dưới Reorder Level) highlight đỏ
[ ] TC-MRP-03: BOM: Thiết lập công thức món (VD: Trà sữa = 20g trà + 30ml sữa + 50g trân châu)
[ ] TC-MRP-04: Yêu cầu xuất NVL: Nhân viên tạo → Quản lý duyệt → Tự động trừ kho NVL
[ ] TC-MRP-05: Lệnh sản xuất F&B: Duyệt → Trừ NVL theo BOM + Cộng món thành phẩm vào Products
[ ] TC-MRP-06: Nhập kho từ NCC: Chọn NCC, NVL, số lượng → Tăng tồn kho + Ghi nhận giá vốn
[ ] TC-MRP-07: Báo cáo Nhập-Xuất-Tồn: Lọc Tháng/Quý/Năm, giá trị vốn kho NVL
```

### 1.5. HRM - Nhân sự & Chấm công (HRM & Attendance)
```
[ ] TC-HRM-01: Hồ sơ nhân viên: Thêm/Sửa/Xóa (soft delete), Full name, DOB, Gender, Phone, Email, Qualification
[ ] TC-HRM-02: Phân ca: Ca sáng (6-14), Chiều (14-22), Tối (22-6), gán nhân viên theo ngày
[ ] TC-HRM-03: Chấm công vào: So sánh giờ thực tế vs ca → Tag: "Đúng giờ", "Đi trễ X phút"
[ ] TC-HRM-04: Chấm công ra: Tag "Đúng giờ", "Về sớm Y phút"
[ ] TC-HRM-05: Quản lý chốt công hộ: Nhân viên quên chấm ra → Manager/Admin chốt hộ
[ ] TC-HRM-06: Đơn nghỉ phép: 4 loại (Annual/Sick/Maternity/Resignation) → Duyệt → Cập nhật lịch ca = OnLeave
[ ] TC-HRM-07: Thăng chức: Nhân viên → Trưởng nhóm ca/Quản lý ca → Tự động sync Role tài khoản
[ ] TC-HRM-08: Báo cáo HRM: Tháp tuổi, trình độ, thâm niên, tổng quỹ lương cơ bản
[ ] TC-HRM-09: Cổng nhân viên (/staff/home): Xem lịch ca, chấm công, gửi đơn nghỉ, đổi mật khẩu
```

### 1.6. CRM - Khách hàng & Khảo sát (CRM & Survey)
```
[ ] TC-CRM-01: Thêm khách: CCCD, DOB, Sở thích, Hạng thành viên
[ ] TC-CRM-02: Khóa tài khoản (Banned) → Client WPF từ chối đăng nhập
[ ] TC-CRM-03: Phản hồi/Gọi hỗ trợ: Khách gửi từ Client → Web hiển thị realtime → Trả lời
[ ] TC-CRM-04: Tạo khảo sát: Soạn câu hỏi (đơn chọn/đa chọn), Phát hành → Gửi đến khách Active
[ ] TC-CRM-05: Khách làm khảo sát trên Client WPF → Web thống kê realtime: % từng phương án
[ ] TC-CRM-06: Báo cáo CRM: Phân bố hạng, nhóm tuổi, sở thích (tag cloud)
```

### 1.7. Báo cáo & Thống kê (Reports)
```
[ ] TC-REP-01: Bộ lọc: Tùy chọn ngày / Nhanh: Hôm nay / Tháng này / Quý này / Năm nay / Tất cả
[ ] TC-REP-02: Doanh thu: Biểu đồ ngày (Giờ chơi vs F&B vs Hoàn tiền), Bảng tổng hợp tháng
[ ] TC-REP-03: Món bán chạy: Top 10 theo số lượng & giá trị
[ ] TC-REP-04: HRM: Cơ cấu phòng ban, tháp tuổi, thâm niên, quỹ lương
[ ] TC-REP-05: Kho NVL: Tồn thực tế, cảnh báo dưới mức, tổng giá trị vốn
[ ] TC-REP-06: Xuất CSV & In ấn (Print) từng báo cáo
```

### 1.8. Quản trị hệ thống (System Admin)
```
[ ] TC-ADM-01: Quản lý tài khoản: Tạo/Sửa/Xóa (soft ban), Phân quyền 6 role
[ ] TC-ADM-02: Ma trận quyền: 5 vai trò x N module (Read/Write/Create/Delete)
[ ] TC-ADM-03: Quản lý Game: Thêm game (.exe path, banner, category) → Hiển thị trên Client WPF
[ ] TC-ADM-04: Combo nạp tiền: CRUD combo (Giá, Bonus, Mô tả, Active)
[ ] TC-ADM-05: Cấu hình máy: Đổi mã, khu vực, giá giờ, trạng thái
```

---

## 📋 PHẦN 2: KIỂM THỬ DESKTOP CLIENT (WPF .NET 10 - Kiosk Mode)

### 2.1. Màn hình khóa & Đăng nhập (Lock Screen & Login)
```
[ ] TC-CLI-01: Khởi động → Fullscreen Kiosk (Topmost, No border, Chặn WinKey/Alt+Tab/TaskMgr)
[ ] TC-CLI-02: Banner carousel (3 banner) tự chuyển 5.5s, click chuột → mở modal đăng nhập
[ ] TC-CLI-03: Đăng nhập gamer/Demo@123 → Tự động Start Session (nếu máy Available & Balance > 0)
[ ] TC-CLI-04: Đăng nhập sai pass → Error "Tên đăng nhập hoặc mật khẩu không đúng"
[ ] TC-CLI-05: Đăng ký tài khoản mới: Validate fullname, phone, email, DOB, hobbies (≤500 chars)
[ ] TC-CLI-06: Tài khoản Balance = 0 → Thông báo "Hết số dư, vui lòng nạp tại quầy" → Không cho mở máy
[ ] TC-CLI-07: Tài khoản Banned → Thông báo "Tài khoản đã bị khóa, liên hệ nhân viên"
[ ] TC-CLI-08: F2 → Dialog cấu hình Demo: Đổi Machine ID (PC01-PC07), Đổi Server URL, Windowed Mode
[ ] TC-CLI-09: Windowed Mode → Resize, Move, Split screen cho demo 2 máy trên 1 laptop
[ ] TC-CLI-10: Esc → Đóng modal login / Thoát app (nếu không có session active)
[ ] TC-CLI-11: Ctrl+Shift+F12 → Emergency Bypass: Nhập Kiosk:EmergencyPassword → Tạm disable Kiosk
[ ] TC-CLI-12: Không có EmergencyPassword trong config → Emergency Bypass disabled hoàn toàn
```

### 2.2. Phiên chơi & Đồng hồ tính tiền (Session & Billing)
```
[ ] TC-CLI-13: Đồng hồ realtime: Elapsed (HH:mm:ss), Remaining (dựa trên Balance - EstimatedRental)
[ ] TC-CLI-14: Low Balance ≤ 10.000đ hoặc ≤ 10 phút → Cảnh báo vàng/đỏ, âm thanh
[ ] TC-CLI-15: Hết tiền (Remaining ≤ 0) → Auto End Session → Lock screen → Thông báo "Hết số dư"
[ ] TC-CLI-16: Chuyển máy từ Web Admin → Client nhận lệnh realtime: "Phiên chuyển sang PCxx, vui lòng di chuyển"
[ ] TC-CLI-17: Khóa tạm thời (Lock Temporary): Nhập pass → Màn hình khóa → Unlock pass đúng → Mở lại
[ ] TC-CLI-18: Compact Widget: Click "Cửa sổ gọn" → Thu góc phải, hiển thị đồng hồ, Balance, nút Mở rộng
```

### 2.3. Đặt món F&B (Ordering)
```
[ ] TC-CLI-19: Tab Menu: Tìm kiếm, Filter theo Category (Đồ uống/Thức ăn/Đồ ăn nhanh/Combo)
[ ] TC-CLI-20: Thêm món vào giỏ: Tăng/giảm SL, Validate Stock (max 99, không vượt tồn kho)
[ ] TC-CLI-21: Giỏ hàng max 30 món, hiển thị tổng tiền dự kiến
[ ] TC-CLI-22: Đặt món → Gửi expectedUnitPrice, expectedTotal → Server validate giá, stock, balance
[ ] TC-CLI-23: Giá thay đổi giữa lúc → Server trả PRICE_CHANGED → Client reload menu
[ ] TC-CLI-24: Số dư không đủ (Balance - IncurredRental < Total) → INSUFFICIENT_BALANCE
[ ] TC-CLI-25: Đơn hàng realtime: Pending → Preparing → Served (badge màu trên tab)
[ ] TC-CLI-26: Hủy đơn Pending → Hoàn tiền ví + Hoàn kho realtime
[ ] TC-CLI-27: Giục bếp (Rush Kitchen) → Gửi feedback "[Giục bếp] Đơn hàng #xxx" realtime
```

### 2.4. Nạp tiền & VietQR (Topup & VietQR)
```
[ ] TC-CLI-28: Tab Nạp tiền: Chọn tiền mặt / Chuyển khoản VietQR
[ ] TC-CLI-29: Nhập số tiền 1.000 - 10.000.000 → Validate boundary
[ ] TC-CLI-30: Chọn Combo (Đêm/Sinh Viên/Chiến Thần/Đại Gia) → Auto fill amount + note
[ ] TC-CLI-31: VietQR: Sinh URL api.vietqr.io với amount, syntax "NAP {username}", account name
[ ] TC-CLI-32: Copy syntax ngân hàng / Copy URL QR / Mở trình duyệt quét QR
[ ] TC-CLI-33: Nạp tiền mặt → Gửi note "[Tiền mặt tại quầy] ..." → Chờ thu ngân duyệt
[ ] TC-CLI-34: Nhận thông báo realtime "Nạp tiền thành công! Số dư mới: xx.xxx đ" khi thu ngân duyệt
```

### 2.5. Khảo sát & Phản hồi (Survey & Feedback)
```
[ ] TC-CLI-35: Tab Khảo sát: Hiển thị danh sách Published, Chưa trả lời
[ ] TC-CLI-36: Làm khảo sát: Bắt buộc trả lời đủ câu hỏi → Submit → Thông báo "Cảm ơn bạn đã góp ý"
[ ] TC-CLI-37: Tab Hỗ trợ: Gửi phản hồi (Subject + Content) / Gọi hỗ trợ tại máy (Quick template)
[ ] TC-CLI-38: Nhận phản hồi từ nhân viên → Hiển thị trong danh sách, badge "Đã trả lời"
```

### 2.6. Game Launcher & Lucky Wheel
```
[ ] TC-CLI-39: Tab Game: Danh sách game từ Server (VALORANT, LoL, CS2, PUBG...)
[ ] TC-CLI-40: Launch VALORANT → Tìm Riot Client (quét tất cả ổ Fixed + Registry) → Launch qua Riot Client
[ ] TC-CLI-41: Launch game khác → Process.Start(executable) → Fallback simulate nếu không tìm thấy file
[ ] TC-CLI-42: Launch game → Client auto Compact Widget (minimize theo dõi giờ)
[ ] TC-CLI-43: Vòng quay may mắn: 1 lượt/ngày, nạp 50k+ hoặc tích lũy giờ → +1 lượt
[ ] TC-CLI-44: Quay trúng: + tiền / + giờ / voucher / item → Cập nhật Balance realtime
```

### 2.7. Hồ sơ & Cài đặt (Profile & Settings)
```
[ ] TC-CLI-45: Tab Hồ sơ: Xem/Cập nhật Full name, Phone, Email, DOB, Hobbies
[ ] TC-CLI-46: Đổi mật khẩu: Old pass (8-100 chars), New pass ≠ Old, Confirm match
[ ] TC-CLI-47: Bật/Tắt âm thanh hiệu ứng (Cyberpunk sounds)
[ ] TC-CLI-48: Lịch sử: Phiên chơi, Nạp tiền, Đơn F&B, Giao dịch, Khảo sát đã làm
```

---

## 📋 PHẦN 3: KIỂM THỬ BACKEND API (ASP.NET Core 10 + SQL Server)

### 3.1. API Contract & Validation
```
[ ] TC-API-01: Tất cả endpoint bắt buộc Authorization (trừ /auth/login, /auth/register, /health)
[ ] TC-API-02: Parameterized queries 100% (không string concat SQL) - Verify qua code review
[ ] TC-API-03: Validation input: Range, Required, Enum, String length, Date format
[ ] TC-API-04: Idempotency Key: POST /orders, /topups với key trùng + body khác → 409 IDEMPOTENCY_CONFLICT
[ ] TC-API-05: Idempotency Key: Key trùng + body giống → Replay response cũ (200 OK)
[ ] TC-API-06: Pagination: page, pageSize, sort, order, search, from/to date
[ ] TC-API-07: Response format chuẩn: { data: ..., meta: {total, page, pageSize} } hoặc { error: {code, message, fields} }
```

### 3.2. Business Logic - Core Flows
```
[ ] TC-BIZ-01: Start Session: Balance > 0, Machine Available, No active session → Success
[ ] TC-BIZ-02: Start Session: Balance ≤ 0 → INSUFFICIENT_BALANCE
[ ] TC-BIZ-03: Start Session: Machine Maintenance/InUse → COMPUTER_UNAVAILABLE
[ ] TC-BIZ-04: Start Session: Customer đã có session active khác → ACTIVE_SESSION
[ ] TC-BIZ-05: End Session: Tính tiền theo giây (Ceiling), trừ Balance, ghi Transaction Rental
[ ] TC-BIZ-06: End Session: Balance không đủ trả hết giờ → Trừ hết Balance, không âm
[ ] TC-BIZ-07: Transfer Session: Chỉ Operator, Machine đích Available, khác máy hiện tại
[ ] TC-BIZ-08: Transfer: Giữ nguyên Start_Time, Start_Balance, Amount → Chuyển Applied_Hourly_Rate
[ ] TC-BIZ-09: Create Order: Validate session active trên máy đó, stock, price lock, balance
[ ] TC-BIZ-10: Cancel Order: Hoàn tiền + Hoàn kho NVL (trừ theo BOM) + Hoàn stock Products
[ ] TC-BIZ-11: Topup Cash: Pending → Cashier Approve → Cộng Balance + Transaction TopUp
[ ] TC-BIZ-12: Topup VietQR: Customer tạo → Cashier thấy realtime → Approve → Cộng tiền
[ ] TC-BIZ-13: Refund: Kiểm tra Available Balance (Balance - IncurredRental) → Withdraw Transaction
[ ] TC-BIZ-14: Heartbeat: Customer chỉ heartbeat máy của mình → 403 nếu máy thuộc session khác
[ ] TC-BIZ-15: CloseExpiredSessions: Offline > 300s hoặc Balance ≤ 0 → Auto End Session
```

### 3.3. Transaction Safety & Concurrency
```
[ ] TC-TXN-01: IsolationLevel.Serializable + UPDLOCK,HOLDLOCK trên mọi transaction
[ ] TC-TXN-02: Đồng thời 2 request Start Session cùng máy → 1 success, 1 COMPUTER_UNAVAILABLE (2601/2627)
[ ] TC-TXN-03: Đồng thời 2 request Topup cùng idempotency key → 1 success, 1 replay
[ ] TC-TXN-04: Đồng thời Order + EndSession → Không double-charge, không double-deduct stock
[ ] TC-TXN-05: Trigger TR_Sync_Customer_Balance: TopUp/Refund/Rental/FoodOrder/Withdraw → Balance sync đúng
[ ] TC-TXN-06: Balance_Before trong Transactions: Chỉ ghi cho TopUp, chạy window function
```

### 3.4. SignalR Realtime
```
[ ] TC-SIG-01: Kết nối /hubs/internetcafe với access_token query string (WebSocket)
[ ] TC-SIG-02: SessionStarted → Web Admin cập nhật máy InUse realtime (không cần refresh)
[ ] TC-SIG-03: OrderCreated → Cashier thấy toast + đơn mới trong danh sách
[ ] TC-SIG-04: TopupRequested → Cashier thấy toast "Yêu cầu nạp tiền mới"
[ ] TC-SIG-05: BalanceChanged → Client WPF cập nhật số dư realtime
[ ] TC-SIG-06: ComputerChanged → Web Admin cập nhật trạng thái máy (Available/InUse/Maintenance)
[ ] TC-SIG-07: RemoteCommand (Shutdown/Restart) → Client WPF nhận và thực thi
[ ] TC-SIG-08: SurveyPublished → Client WPF tab Khảo sát cập nhật realtime
```

### 3.5. Security
```
[ ] TC-SEC-01: SQL Injection: Tất cả input qua parameter → PASS
[ ] TC-SEC-02: XSS: Web Admin không dùng dangerouslySetInnerHTML → PASS
[ ] TC-SEC-03: IDOR: GET /{resource}/{id} → Scoped Workspace Read Model → 404 không phải 403
[ ] TC-SEC-04: Rate Limiting: /auth/login, /auth/register → 10 req/min/IP (FixedWindow)
[ ] TC-SEC-05: Security Headers: X-Frame-Options: DENY, X-Content-Type-Options: nosniff, Referrer-Policy
[ ] TC-SEC-06: CORS: Chỉ cho phép 127.0.0.1:5173, localhost:5173, LAN RFC1918
[ ] TC-SEC-07: Password: PBKDF2 (ASP.NET Core Identity), không log plain text
[ ] TC-SEC-08: Token: CSPRNG 256-bit, sliding expiration 24h, revocable
[ ] TC-SEC-09: Kiosk Emergency Password: Không hardcode, chỉ từ config Kiosk:EmergencyPassword
[ ] TC-SEC-10: .gitignore root có .env, .env.*, appsettings.*.json
```

---

## 📋 PHẦN 4: KIỂM THỬ TÍCH HỢP (INTEGRATION TESTS)

### 4.1. Luồng End-to-End Chính
```
[ ] TC-E2E-01: Khách đăng ký → Đăng nhập Client → Nạp VietQR → Thu ngân duyệt → Mở máy → Chơi game → Đặt món F&B → Thu ngân phục vụ → Kết thúc phiên → Xem hóa đơn
[ ] TC-E2E-02: Nhân viên check-in ca → Làm việc → Gửi đơn nghỉ phép → Quản lý duyệt → Chấm công ra (Admin chốt hộ) → Xem báo cáo chấm công
[ ] TC-E2E-03: Quản lý tạo NVL → Thiết lập BOM món → Nhân viên đề xuất xuất kho → Quản lý duyệt → Trừ kho NVL → Sản xuất món → Cộng menu bán hàng
[ ] TC-E2E-04: Quản lý tạo khảo sát → Phát hành → Khách làm khảo sát Client → Web thống kê realtime
[ ] TC-E2E-05: Demo 2 máy trên 1 laptop: chay-demo-2-may.bat → PC01 + PC02 Windowed + Web Admin split screen
[ ] TC-E2E-06: Demo LAN 2 laptop: Laptop A (Server chay-toan-bo.bat) + Laptop B (Client F2 → IP Laptop A:5055)
```

### 4.2. Offline/Standalone Mode
```
[ ] TC-OFF-01: Client WPF không kết nối API → Chế độ Standalone Demo (SmartCafeApi)
[ ] TC-OFF-02: Standalone: Đăng nhập gamer/Demo@123 → Mock data đầy đủ (Products, Combos, Games, Sessions)
[ ] TC-OFF-03: Standalone: Đặt món, Nạp tiền, Khảo sát, Game launcher → Mô phỏng thành công
[ ] TC-OFF-04: Portable ZIP: dong-goi-portable.bat → Giải nén chạy trên máy không cài .NET → Self-contained
[ ] TC-OFF-05: ClickOnce: dong-goi-clickonce.bat → Publish GitHub Pages → Client auto-update
```

---

## 📋 PHẦN 5: KIỂM THỬ HỒI QUY (REGRESSION) - TEST CASES TỰ ĐỘNG

### 5.1. Playwright E2E (34 Test Cases)
```bash
cd InternetCafe/apps/admin-web
npm run test:e2e
# Hoặc: .\chay-playwright-tests.bat
```
**Mong đợi:** 34/34 PASS (~1.5-2 phút)

| File Test | Test Cases | Phân hệ |
|-----------|------------|---------|
| `app-client-simulation.spec.ts` | 4 | Client App + Web Admin Integration |
| `auth.spec.ts` | 5 | Authentication & Security |
| `machines.spec.ts` | 3 | Computer Fleet Management |
| `pos-billing.spec.ts` | 3 | Cashier, Wallet, F&B |
| `mrp-inventory.spec.ts` | 5 | MRP & BOM |
| `hrm-staff.spec.ts` | 6 | HRM & Staff Portal |
| `crm-feedback.spec.ts` | 2 | CRM Survey & Feedback |
| `reports.spec.ts` | 2 | Financial Reports & Invoices |
| `system-admin.spec.ts` | 4 | System Admin & RBAC |

### 5.2. Postman/Newman API Tests (115 Test Cases)
```bash
npx newman run testing/postman/InternetCafe_API.postman_collection.json -e testing/postman/InternetCafe_Local.postman_environment.json
# Hoặc: .\chay-postman-tests.bat
```
**Mong đợi:** 115/115 PASS

---

## 📋 PHẦN 6: KIỂM THỬ HIỆU NĂNG & TAI (PERFORMANCE & STRESS)

```
[ ] TC-PERF-01: Backend /health < 50ms
[ ] TC-PERF-02: /workspace (full load) < 300ms
[ ] TC-PERF-03: 50 client WPF đồng thời heartbeat 5 phút → CPU/Memory Backend ổn định
[ ] TC-PERF-04: 100 request/giây /auth/login → Rate limiting hoạt động, không crash
[ ] TC-PERF-05: SignalR 100 connections broadcast event → < 200ms latency đến all clients
[ ] TC-PERF-06: Database: 10k Usage_Sessions, 50k Transactions → Query báo cáo < 1s
[ ] TC-PERF-07: Client WPF Cold start < 3s, Warm start < 1s
[ ] TC-PERF-08: Web Admin First Contentful Paint < 2s (Vite + React 19)
```

---

## 📋 PHẦN 7: KIỂM THỬ CÁC TRƯỜNG HỢP BIẾT (EDGE CASES)

```
[ ] TC-EDGE-01: Ngày nh nhuận (29/02) - DOB, Shift scheduling, Payroll month
[ ] TC-EDGE-02: Ca đêm qua nửa đêm (22:00 - 06:00) - Check-in 23:00, Check-out 05:00
[ ] TC-EDGE-03: Múi giờ UTC vs Local (Vietnam UTC+7) - Heartbeat, Session timing, Reports
[ ] TC-EDGE-04: Tên có dấu tiếng Việt (Nguyễn Văn An, Trà sữa trân châu) - UTF-8 DB + UI
[ ] TC-EDGE-05: Số tiền lớn: 10.000.000đ nạp, 999.999.999 doanh thu - decimal(18,2) không overflow
[ ] TC-EDGE-06: Máy client đổi tên MachineName (F2) trùng máy khác → Server xử lý conflict
[ ] TC-EDGE-07: Network lag: Heartbeat timeout 30s → Server đánh dấu Offline → Client reconnect auto
[ ] TC-EDGE-08: Server restart: Client WPF reconnect SignalR tự động,.sync session state
[ ] TC-EDGE-09: Concurrent: 2 khách cùng đặt món cuối cùng trong kho → 1 success, 1 OUT_OF_STOCK
[ ] TC-EDGE-10: Xóa mềm (Soft delete): Customer/Employee/Account → Status Inactive/Banned → Giữ lịch sử
```

---

## 📋 PHẦN 8: CHECKLIST TRIỂN KHAI & DEMO (DEPLOYMENT & DEMO READINESS)

```
[ ] DEP-01: chay-toan-bo.bat → 1 click: SQL LocalDB + Backend (5055) + Web (5173) + Client (PC01)
[ ] DEP-02: dung-toan-bo.bat → Kill tất cả process, giải phóng port 5055, 5173
[ ] DEP-03: chay-demo-2-may.bat → Web Admin (trái) + PC01 + PC02 (phải, windowed)
[ ] DEP-04: chay-backend.bat / chay-web-admin.bat / chay-desktop-client.bat → Chạy riêng lẻ
[ ] DEP-05: dong-goi-portable.bat → InternetCafe/dist/InternetCafe-Client-Portable.zip (Self-contained Win-x64)
[ ] DEP-06: dong-goi-clickonce.bat → Publish GitHub Pages → https://drakew0209.github.io/CNPM-HTTTDN/client/
[ ] DEP-07: README.md cập nhật: Tech stack (.NET 10, React 19, Ant Design 6), Accounts demo, Demo guide
[ ] DEP-08: docs/KichBanDemo.md: 4 kịch bản demo (MRP, HRM, CRM, Báo cáo) + 10 bước demo tiêu chuẩn
[ ] DEP-09: Database/InternetCafe Final.sql: 28 bảng, Run được trên SQL Server LocalDB mới
[ ] DEP-10: .gitignore root: .env, .env.*, appsettings.*.json, !.env.example
```

---

## 📊 BÁO CÁO KẾT QUẢ MONG ĐỢI

| Loại test | Tổng TC | Pass Rate mong đợi | Thời gian |
|-----------|---------|-------------------|-----------|
| **Playwright E2E** | 34 | **100% (34/34)** | ~2 phút |
| **Postman API** | 115 | **100% (115/115)** | ~3 phút |
| **Manual UI/UX** | ~200 | **100% Critical/High** | ~4-6 giờ |
| **Integration E2E** | 6 | **100%** | ~30 phút |
| **Performance** | 8 | **Pass thresholds** | ~15 phút |
| **Edge Cases** | 10 | **100%** | ~30 phút |
| **Deployment/Demo** | 10 | **100%** | ~15 phút |

---

## 🛠️ CÔNG CỤ HỖ TRỢ KIỂM THỬ

```bash
# 1. Khởi động toàn bộ hệ thống
.\chay-toan-bo.bat

# 2. Chạy Playwright E2E (Web Admin + Client Simulation)
.\chay-playwright-tests.bat
# Xem báo cáo HTML:
cd InternetCafe/apps/admin-web && npx playwright show-report

# 3. Chạy Postman API Tests
.\chay-postman-tests.bat
# Hoặc Newman CLI:
npx newman run testing/postman/InternetCafe_API.postman_collection.json -e testing/postman/InternetCafe_Local.postman_environment.json -r htmlextra,cli

# 4. Build & Test Backend
cd InternetCafe/apps/backend/InternetCafe.Backend
dotnet build          # 0 errors
dotnet test           # Nếu có unit tests

# 5. Build & Test Client
cd InternetCafe/apps/desktop-client
dotnet build          # 0 errors, 0 warnings

# 6. Build Web Admin
cd InternetCafe/apps/admin-web
npm run typecheck     # TypeScript 0 errors
npm run build         # Vite build success
npm run test          # Vitest unit tests

# 7. Demo 2 máy trên 1 laptop
.\chay-demo-2-may.bat

# 8. Đóng hệ thống
.\dung-toan-bo.bat
```

---

## 📝 TEMPLATE BÁO CÁO LỖI (BUG REPORT TEMPLATE)

Khi phát hiện bug, ghi theo format:
```markdown
**Bug ID:** TC-XXX-##
**Module:** [Web Admin / Desktop Client / Backend API / Database]
**Severity:** [Blocker / Critical / Major / Minor / Trivial]
**Priority:** [P1-Immediate / P2-High / P3-Medium / P4-Low]
**Môi trường:** Windows 11, Chrome 120+, SQL LocalDB, .NET 10, React 19
**Steps to Reproduce:**
1. ...
2. ...
3. ...
**Actual Result:** ...
**Expected Result:** ...
**Evidence:** Screenshot / Video / Log / Request/Response payload
**Root Cause Analysis:** (nếu biết)
**Suggested Fix:** ...
```

---

## ✅ TIÊU CHÍ CHẤP NHẬN (ACCEPTANCE CRITERIA)

Dự án coi là **PASSED** khi:
1. ✅ **34/34 Playwright E2E PASS**
2. ✅ **115/115 Postman API PASS**
3. ✅ **0 Build Errors** (Backend + Web + Client)
4. ✅ **Demo 2 máy trên 1 laptop** chạy mượt (chay-demo-2-may.bat)
5. ✅ **Demo LAN 2 laptop** kết nối thành công
6. ✅ **Portable ZIP** chạy được trên máy sạch (không cài .NET)
7. ✅ **ClickOnce auto-update** hoạt động qua GitHub Pages
8. ✅ **Không lỗi Critical/High** bảo mật (theo Security Audit)
9. ✅ **14/14 chức năng bắt buộc** đạt (theo DoiChieuYeuCau.md)
10. ✅ **Tài liệu** README.md, KichBanDemo.md, DoiChieuYeuCau.md cập nhật đồng bộ

---

> **Lưu ý:** Prompt này thiết kế cho việc **kiểm thử handbook** (thủ công) và **tự động hóa** song song. Tester có thể import checklist vào TestRail/Jira/Xray hoặc chạy script tự động. Ưu tiên chạy **Playwright + Postman** trước để baseline, sau đó test manual các case UI/UX phức tạp.