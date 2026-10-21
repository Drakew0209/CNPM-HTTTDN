# CẨM NANG HƯỚNG DẪN KIỂM THỬ API VỚI POSTMAN
## (DỰ ÁN HỆ THỐNG QUẢN TRỊ & VẬN HÀNH INTERNET CAFE)

Chào mừng bạn đến với bộ tài liệu thực hành **API Testing (Manual & Automation)** với **Postman** dành riêng cho dự án Internet Cafe!

Thư mục này chứa đầy đủ:
1. 📂 [InternetCafe_API.postman_collection.json](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/testing/postman/InternetCafe_API.postman_collection.json): Bộ sưu tập hơn 20 API requests mẫu kèm kịch bản test tự động.
2. ⚙️ [InternetCafe_Local.postman_environment.json](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/testing/postman/InternetCafe_Local.postman_environment.json): File cấu hình biến môi trường cục bộ (tài khoản mẫu, dynamic token, ID trung gian).

---

## 1. TỔNG QUAN: TẠI SAO PHẢI HỌC KIỂM THỬ API?

Trong kim tự tháp kiểm thử phần mềm (**Test Pyramid**):
```
        /  UI Tests (E2E)  \       <- Chậm, dễ gãy (flaky), chi phí cao
       /────────────────────\
      /   API / Service      \     <- Nhanh, bao quát nghiệp vụ sâu, phát hiện lỗi sớm
     /────────────────────────\
    /        Unit Tests        \   <- Rất nhanh, kiểm tra từng hàm nhỏ
   /────────────────────────────\
```
- **Kiểm thử API (API Testing)** giúp kiểm tra trực tiếp các quy tắc nghiệp vụ (Business Rules), tính toàn vẹn dữ liệu và an toàn bảo mật mà không cần chờ giao diện Web/App hoàn thiện.
- **Postman** là công cụ số 1 thế giới hiện nay cho phép:
  - **Manual API Testing:** Soạn request (Headers, Body, Params), gửi lên server và quan sát phản hồi.
  - **Automation API Testing:** Viết code JavaScript tự động kiểm tra mã phản hồi (Status Code), dữ liệu trả về (Data Assertions), thời gian phản hồi (Response Time) và xâu chuỗi dữ liệu (Request Chaining).

---

## 2. HƯỚNG DẪN CÀI ĐẶT & IMPORT VÀO POSTMAN

### Bước 1: Khởi động Backend API của dự án
Trước khi kiểm thử, đảm bảo Backend đang chạy tại cổng `5055`:
- Mở PowerShell / Terminal tại thư mục gốc dự án và chạy:
  ```powershell
  .\chay-backend.bat
  ```
  *(Hoặc lệnh: `dotnet run --project InternetCafe/apps/backend/InternetCafe.Backend/InternetCafe.Backend.csproj --urls http://127.0.0.1:5055`)*
- Kiểm tra backend đã sẵn sàng: Truy cập trình duyệt vào `http://127.0.0.1:5055/health`, thấy kết quả `{"data":{"status":"ok",...}}` là thành công!

### Bước 2: Import vào Postman
1. Mở ứng dụng **Postman** trên máy tính của bạn.
2. Ở góc trên bên trái, nhấn nút **Import**.
3. Kéo thả 2 file sau vào cửa sổ Import (hoặc chọn Browse files):
   - `testing/postman/InternetCafe_API.postman_collection.json`
   - `testing/postman/InternetCafe_Local.postman_environment.json`
4. Nhấn **Import**.

### Bước 3: Kích hoạt Environment
1. Nhìn vào góc trên bên phải của Postman (chỗ dropdown Environment).
2. Nhấp chọn: **`InternetCafe Localhost Environment`**.
3. Bấm vào icon con mắt (Environment quick look) để xem các biến: `baseUrl` (`http://127.0.0.1:5055/api/v1`), `adminUsername`, `adminPassword`, v.v.

---

## 3. CẤU TRÚC BỘ KIỂM THỬ TOÀN DIỆN (47 API REQUESTS - ĐỘ PHỦ 100% TOÀN DỰ ÁN)

Bộ test được chia thành 7 thư mục nghiệp vụ hoàn chỉnh bao phủ 100% chức năng Backend:

### 📁 01_Authentication & Account Profile (14 Requests - Xác thực & Vòng đời tài khoản)
- **`01. Login as Admin`**: Đăng nhập quyền Quản trị (`admin` / `Demo@123`). Lưu `{{adminToken}}`.
- **`02. Login as Manager`**: Đăng nhập quyền Quản lý (`manager` / `Demo@123`). Lưu `{{managerToken}}`.
- **`03. Login as Cashier`**: Đăng nhập quyền Thu ngân (`cashier` / `Demo@123`). Lưu `{{cashierToken}}`.
- **`04. Login as Staff`**: Đăng nhập quyền Nhân viên (`staff` / `Demo@123`). Lưu `{{staffToken}}`.
- **`05. Login as Customer`**: Đăng nhập quyền Hội viên (`gamer` / `Demo@123`). Lưu `{{customerToken}}` và `{{testCustomerId}}`.
- **`06. Login Fail - Wrong Password`**: Kiểm thử tiêu cực (Negative Test) với mật khẩu sai, bắt lỗi `INVALID_CREDENTIALS`.
- **`07. Register New Customer`**: Đăng ký hội viên mới với dữ liệu sinh ngẫu nhiên.
- **`08. Login Disposable Customer`**: Đăng nhập tài khoản dùng 1 lần để test đổi mật khẩu & đăng xuất an toàn.
- **`09. Change Disposable Customer Password`**: Đổi mật khẩu tài khoản cá nhân (`POST /me/password`).
- **`10. Relogin Disposable Customer with New Password`**: Kiểm tra đăng nhập lại bằng mật khẩu mới thành công.
- **`11. Logout Disposable Customer`**: Thu hồi token và đăng xuất (`POST /auth/logout`).
- **`12. Verify Token Revoked - Negative Test`**: Kiểm tra token đã logout lập tức bị từ chối `401 Unauthorized` khi truy cập `/me`.
- **`13. Get Current Profile (/me)`**: Lấy thông tin tài khoản hiện hành.
- **`14. Update Profile Information (PATCH /me)`**: Cập nhật thông tin cá nhân (họ tên, sở thích).

### 📁 02_System Health & Master Catalog (6 Requests - Hệ thống & Danh mục gốc)
- **`01. Health Check`**: Kiểm tra trạng thái máy chủ và kết nối cơ sở dữ liệu SQL Server.
- **`02. Get Scoped Workspace (/workspace)`**: Mô hình đọc dữ liệu an toàn theo phân quyền (Authorization-aware read model).
- **`03. List System Accounts (Admin Only)`**: Chỉ Admin xem được danh sách tài khoản toàn hệ thống.
- **`04. List Product Categories`**: Danh mục phân loại đồ ăn/đồ uống.
- **`05. List Promotion Combos`**: Danh mục gói combo khuyến mãi nạp giờ chơi.
- **`06. List Gaming Library (/games)`**: Danh mục game cài đặt trên máy trạm.

### 📁 03_Billing, Wallets & Financial Ledger (5 Requests - Tài chính & Ví hội viên)
- **`01. List All Customers`**: Lấy danh sách khách hàng và tự động lưu `{{testCustomerId}}`.
- **`02. Cashier Topup Customer Balance`**: Thu ngân nạp 20.000đ vào ví khách (`TopUp`).
- **`03. Topup Validation - Negative Amount`**: Kiểm thử nạp số âm (`-10.000đ`) bị từ chối hợp lệ (`400/422`).
- **`04. Refund Balance to Customer`**: Hoàn tiền mặt và tạo giao dịch rút tiền `Withdraw`.
- **`05. List Financial Transactions Ledger`**: Sổ cái giao dịch tài chính.

### 📁 04_Computers & Gaming Sessions (5 Requests - Vận hành Máy trạm & Phiên chơi)
- **`01. List All Computers`**: Quét danh sách máy trạm, lưu `{{testComputerId}}` và `{{testTargetComputerId}}`.
- **`02. Customer Start Session on Computer`**: Khách hàng mở máy chơi và lưu `{{testSessionId}}` (giữ phiên để đặt món F&B).
- **`03. Send Machine Heartbeat`**: Gửi xung nhịp định kỳ từ máy trạm lên máy chủ.
- **`04. Remote Control Computer (Maintenance Command)`**: Lệnh chuyển máy sang bảo trì từ xa (`Maintenance`).
- **`05. Restore Computer to Available`**: Hoàn tất bảo trì, mở lại máy sẵn sàng (`Available`).

### 📁 05_F&B Menu, Orders & Inventory (MRP) (8 Requests - Bán hàng & Quản lý Kho)
- **`01. List Menu Products`**: Danh mục món ăn và lưu `{{testProductId}}`.
- **`02. Customer Create F&B Order`**: Đặt món ăn từ máy trạm (khi phiên chơi đang mở).
- **`03. Cashier Transition Order to Preparing`**: Thu ngân chuyển trạng thái đơn hàng sang `Preparing`.
- **`04. List Bill of Materials (BOM)`**: Công thức định lượng nguyên vật liệu cho từng món.
- **`05. List Material Suppliers`**: Danh sách nhà cung ứng nguyên vật liệu & thiết bị.
- **`06. List Material Inventory`**: Kiểm tra tồn kho nguyên vật liệu pha chế.
- **`07. Create New Material`**: Thêm mới nguyên vật liệu vào kho pha chế.
- **`08. Customer End Session`**: Kết thúc phiên chơi sau khi ăn uống & chơi game, tính cước giờ chơi.

### 📁 06_HRM & Shift Attendance (5 Requests - Nhân sự & Chấm công)
- **`01. List Employees`**: Danh sách toàn bộ nhân viên và mức lương.
- **`02. List Work Shifts`**: Danh mục các ca làm việc (Sáng, Chiều, Đêm).
- **`03. List Work Schedules`**: Lịch phân ca của nhân viên và lưu `{{testScheduleId}}`.
- **`04. Employee Check-in Shift`**: Nhân viên bấm chấm công vào ca.
- **`05. List Attendance Records`**: Bảng chấm công toàn bộ nhân viên.

### 📁 07_Financial Reports & Invoices (4 Requests - Báo cáo Doanh thu & Hóa đơn)
- **`01. Summary Revenue Report`**: Báo cáo tổng hợp doanh thu theo tháng (Cashier / Manager).
- **`02. Deep Analytics Report`**: Báo cáo phân tích chuyên sâu cho cấp quản lý.
- **`03. List Invoices`**: Danh sách hóa đơn bán lẻ giờ chơi và F&B.
- **`04. Get Invoice Detail by ID`**: Xem chi tiết một hóa đơn cụ thể (`/invoices/{id}`).

---

## 4. HỌC CÁCH VIẾT TEST SCRIPTS VÀ ASSERTIONS TRONG POSTMAN

Trong Postman, tab **Tests** của mỗi request là nơi bạn viết code JavaScript để kiểm thử tự động. Dưới đây là các kỹ thuật thực tế được áp dụng trong project:

### Kỹ thuật 1: Kiểm tra mã HTTP Status Code
```javascript
pm.test("Status code is 200 OK", function () {
    pm.response.to.have.status(200);
});

// Hoặc kiểm tra nhiều mã hợp lệ:
pm.test("Status is 200 or 201", function () {
    pm.expect(pm.response.code).to.be.oneOf([200, 201]);
});
```

### Kỹ thuật 2: Kiểm tra thời gian phản hồi (Performance SLA)
```javascript
pm.test("Response time is fast (< 300ms)", function () {
    pm.expect(pm.response.responseTime).to.be.below(300);
});
```

### Kỹ thuật 3: Kiểm tra cấu trúc và giá trị trường JSON (Data Assertion)
```javascript
pm.test("Response contains valid token and correct role", function () {
    var json = pm.response.json();
    
    // Kiểm tra trường tồn tại
    pm.expect(json).to.have.property('data');
    
    // Kiểm tra kiểu dữ liệu (String, Number, Array, Object)
    pm.expect(json.data.token).to.be.a('string').and.not.empty;
    
    // Kiểm tra giá trị chính xác
    pm.expect(json.data.account.role).to.eql('Admin');
});
```

### Kỹ thuật 4: Xâu chuỗi tham số giữa các Request (Request Chaining)
Đây là kỹ thuật quan trọng nhất trong Automation:
```javascript
// Request A (Login): Lưu token vào Environment
var json = pm.response.json();
pm.environment.set("adminToken", json.data.token);

// Request B (Get Profile): Tự động dùng token này trong Header:
// Authorization: Bearer {{adminToken}}
```

### Kỹ thuật 5: Pre-request Script (Chuẩn bị dữ liệu trước khi gửi)
Trong tab **Pre-request Script** của request `Register New Customer`:
```javascript
// Tự động sinh tên ngẫu nhiên: test_gamer_48123
var randomSuffix = Math.floor(Math.random() * 100000);
pm.variables.set("newCustomerUser", "test_gamer_" + randomSuffix);
pm.variables.set("newCustomerPhone", "098" + Math.floor(1000000 + Math.random() * 9000000));
```
Khi request gửi đi, Body sẽ tự thay thế `{{newCustomerUser}}` bằng chuỗi vừa sinh, đảm bảo không bao giờ bị lỗi trùng tài khoản!

---

## 5. HƯỚNG DẪN CHẠY TEST TỰ ĐỘNG (COLLECTION RUNNER & NEWMAN)

### Cách 1: Chạy bằng Collection Runner trên giao diện Postman
1. Nhấp chuột vào tên Collection: **`InternetCafe Management System API - Test Suite`**.
2. Nhấn nút **Run** (màu xanh hoặc nút Run Collection ở menu tác vụ).
3. Tại màn hình Runner:
   - Đảm bảo chọn đúng Environment: `InternetCafe Localhost Environment`.
   - Chọn số lần lặp (Iterations: `1`).
4. Nhấn **Run InternetCafe Management System API...**.
5. Bạn sẽ thấy Postman tự động gửi lần lượt toàn bộ 20+ request trong vài giây và hiển thị bảng kết quả:
   - ✅ **Passed**: Các assertion đạt chuẩn.
   - ❌ **Failed**: Các assertion thất bại (nếu có lỗi logic hoặc server chết).

### Cách 2: Chạy tự động trong dòng lệnh với Newman (CI/CD Ready)
Bạn có thể chạy toàn bộ test suite này ngay trong Terminal mà không cần mở Postman:
```powershell
# Chạy với Newman qua npx (không cần cài đặt trước)
npx newman run testing/postman/InternetCafe_API.postman_collection.json -e testing/postman/InternetCafe_Local.postman_environment.json
```
Newman sẽ in ra bảng tổng kết kết quả kiểm thử cực kỳ chuyên nghiệp ngay trên màn hình Console!

---

## 6. BÀI TẬP THỰC HÀNH TỰ LUYỆN (QA HANDS-ON CHALLENGES)

Để nâng cao tay nghề, bạn hãy thử tự làm 3 bài tập thực hành sau:

- [ ] **Bài tập 1: Thêm Request Đổi mật khẩu cá nhân**
  - Tạo request `POST {{baseUrl}}/me/password`
  - Header: `Authorization: Bearer {{customerToken}}`
  - Body: `{ "currentPassword": "Demo@123", "newPassword": "NewPassword@2026" }`
  - Viết test script kiểm tra trả về thành công mã 200.

- [ ] **Bài tập 2: Kiểm thử giới hạn nạp tiền tối đa (Boundary Test)**
  - Thử nạp tiền `10.000.001đ` (vượt trần 10 triệu) vào ví khách hàng.
  - Viết test script kiểm tra hệ thống có từ chối với mã 400 hoặc 422 hay không.

- [ ] **Bài tập 3: Kiểm thử Phân quyền (Access Control Testing)**
  - Dùng token của Khách hàng (`{{customerToken}}`) để gọi API lấy danh sách tài khoản hệ thống `GET {{baseUrl}}/accounts`.
  - Viết test script kiểm tra hệ thống phải trả về đúng mã **`403 Forbidden`** (vì chỉ có Admin mới được xem tài khoản).
