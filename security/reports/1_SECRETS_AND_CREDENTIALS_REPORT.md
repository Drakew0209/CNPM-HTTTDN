# BÁO CÁO KIỂM TRA BẢO MẬT: BÍ MẬT & THÔNG TIN XÁC THỰC (SECRETS & CREDENTIALS)
**Mã phân loại vibe-check:** `1. SECRETS_EXPOSURE`  
**Dự án:** Hệ thống Quản trị & Vận hành Internet Cafe (`CNPM-HTTTDN`)  
**Ngày kiểm định:** 16/10/2026  
**Trạng thái kiểm tra:** Hoàn tất  

---

## 1. TỔNG QUAN HẠNG MỤC
Hạng mục này kiểm tra việc bảo vệ các thông tin nhạy cảm bao gồm: file môi trường (`.env`), chuỗi kết nối cơ sở dữ liệu (`ConnectionStrings`), khóa bí mật API, cấu hình Git Ignore, và các mật khẩu mặc định được mã hóa cứng trong mã nguồn.

---

## 2. BẢNG TỔNG HỢP KẾT QUẢ KIỂM TRA

| Mã phát hiện | Hạng mục kiểm tra | Mức độ nghiêm trọng | Trạng thái xác minh | Vị trí (File & Dòng) |
| :--- | :--- | :---: | :---: | :--- |
| **SEC-01** | Bỏ sót mẫu `.env` trong file root `.gitignore` | **HIGH** | Đã xác minh (Verified) | [.gitignore](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/.gitignore#L1-L50) |
| **SEC-02** | Mật khẩu mặc định `Demo@123` mã hóa cứng trên nhiều tầng | **HIGH** | Đã xác minh (Verified) | [ManagementService.cs:133](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ManagementService.cs#L133), [DatabaseBootstrapper.cs:39](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Data/DatabaseBootstrapper.cs#L39), [MainWindow.xaml.cs:57](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs#L57) |
| **SEC-03** | Tự động điền mật khẩu đăng nhập trên giao diện Client & Web | **MEDIUM** | Đã xác minh (Verified) | [MainWindow.xaml.cs:69](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs#L69), [App.tsx:25](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/admin-web/src/App.tsx#L25) |
| **SEC-04** | Chuỗi kết nối LocalDB kích hoạt `TrustServerCertificate=true` | **LOW** | Đã xác minh (Verified) | [appsettings.json:3](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/appsettings.json#L3) |
| **SEC-05** | Lưu trữ mã token và mật khẩu trong Database | **PASS** | Đã xác minh (Verified) | [AuthService.cs:28](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/AuthService.cs#L28) |

---

## 3. CHI TIẾT CÁC PHÁT HIỆN

### Phát hiện SEC-01: Bỏ sót mẫu cấu hình `.env` tại root `.gitignore`
- **Mức độ:** `HIGH`
- **Tập tin liên quan:** [.gitignore](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/.gitignore#L1-L50)
- **Bằng chứng mã nguồn:**
  ```gitignore
  # Build output & Temp directories
  dist/
  !docs/client/
  Temp/
  /Backend/
  /WebAdmin/
  InternetCafe/scratch/
  .build-check*
  *.log
  ```
- **Phân tích rủi ro:**
  Thư mục con `InternetCafe/.gitignore` có khai báo `.env`, tuy nhiên thư mục gốc của repository Git là `CNPM-HTTTDN/.gitignore` hoàn toàn không có quy tắc loại trừ `.env`, `.env.*`, hoặc `appsettings.Production.json`. Nếu nhà phát triển tạo file `.env` ở cấp thư mục cha hoặc cấu hình môi trường triển khai thực tế, Git sẽ theo dõi và có nguy cơ bị `git add .` rồi đẩy toàn bộ bí mật lên Git server công khai.
- **Biện pháp khắc phục khuyến nghị:**
  Thêm các quy tắc sau vào root `.gitignore`:
  ```gitignore
  .env
  .env.*
  !.env.example
  appsettings.*.json
  !appsettings.json
  !appsettings.Development.json
  ```

---

### Phát hiện SEC-02: Mật khẩu mặc định `Demo@123` bị mã hóa cứng (Hardcoded Fallback)
- **Mức độ:** `HIGH`
- **Tập tin liên quan:**
  - [ManagementService.cs:133, 215, 292](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ManagementService.cs#L133)
  - [DatabaseBootstrapper.cs:39, 72, 102](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Data/DatabaseBootstrapper.cs#L39)
  - [MainWindow.xaml.cs:57](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs#L57)
- **Bằng chứng mã nguồn:**
  *Tại ManagementService.cs (dòng 133, 215, 292):*
  ```csharp
  var password = Value(body, "password") ?? "Demo@123";
  ```
  *Tại MainWindow.xaml.cs (dòng 57):*
  ```csharp
  emergencyPassword = config["Kiosk:EmergencyPassword"] ?? "Demo@123";
  ```
- **Phân tích rủi ro:**
  Khi tạo mới khách hàng, nhân viên hoặc tài khoản quản trị viên mà không truyền `password`, hệ thống tự động gán mật khẩu `"Demo@123"`. Kẻ tấn công hoặc người dùng bất kỳ có thể đăng nhập vào các tài khoản mới tạo bằng mật khẩu mặc định phổ biến này. Đặc biệt trên Desktop Client, mật khẩu thoát chế độ Kiosk cũng mặc định là `"Demo@123"`.
- **Biện pháp khắc phục khuyến nghị:**
  1. Yêu cầu tham số `password` là bắt buộc khi tạo tài khoản (`Required(body, "password")`), không dùng fallback `"Demo@123"` trong logic nghiệp vụ.
  2. Với Desktop Client, cấu hình `Kiosk:EmergencyPassword` phải được đặt qua biến môi trường hoặc cấu hình bảo mật, không để fallback cứng.

---

### Phát hiện SEC-03: Tự động điền mật khẩu đăng nhập trên giao diện người dùng
- **Mức độ:** `MEDIUM`
- **Tập tin liên quan:**
  - [MainWindow.xaml.cs:69](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs#L69)
  - [App.tsx:25](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/admin-web/src/App.tsx#L25)
- **Bằng chứng mã nguồn:**
  *Tại MainWindow.xaml.cs:*
  ```csharp
  Loaded += (_, _) => { UpdateKioskMode(); if (!viewModel.IsAuthenticated) { LoginPassword.Password = "Demo@123"; LoginPassword.Focus(); } };
  ```
  *Tại App.tsx:*
  ```tsx
  <Form form={form} layout="vertical" onFinish={login} initialValues={{username:'manager',password:'Demo@123'}}>
  ```
- **Phân tích rủi ro:**
  Việc điền sẵn mật khẩu nhằm mục đích thuận tiện cho quá trình phát triển/demo, nhưng khi mở máy trạm cho khách chơi, bất kỳ ai cũng có thể chỉ cần nhấn Enter là đăng nhập vào tài khoản demo có sẵn.
- **Biện pháp khắc phục khuyến nghị:**
  Xóa giá trị `initialValues` chứa password trong môi trường chạy chính thức hoặc chỉ bật khi có cờ `VITE_DEMO_MODE=true`.

---

### Phát hiện SEC-04: Cấu hình kết nối LocalDB tắt xác thực SSL Server Certificate
- **Mức độ:** `LOW`
- **Tập tin liên quan:** [appsettings.json:3](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/appsettings.json#L3)
- **Bằng chứng mã nguồn:**
  ```json
  "ConnectionStrings": {
    "InternetCafe": "Server=(localdb)\\MSSQLLocalDB;Database=InternetCafeDB;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
  ```
- **Phân tích rủi ro:**
  Cơ chế xác thực sử dụng Windows Integrated Security (không lộ tài khoản/mật khẩu `sa`). Thuộc tính `TrustServerCertificate=true` phù hợp với LocalDB phát triển cục bộ, nhưng nếu chuyển sang máy chủ SQL Server từ xa qua mạng LAN/Internet, thiết lập này sẽ dễ bị tấn công Man-In-The-Middle (MITM) vì không kiểm tra chứng chỉ máy chủ.
- **Biện pháp khắc phục khuyến nghị:**
  Khi cấu hình cho môi trường Production / Network SQL Server, loại bỏ `TrustServerCertificate=true` và cài đặt chứng chỉ TLS hợp lệ.

---

### Phát hiện SEC-05: Lưu trữ và mã hóa Token / Password trong cơ sở dữ liệu
- **Mức độ:** `PASS` (Đạt chuẩn an toàn)
- **Tập tin liên quan:** [AuthService.cs:11, 28, 110](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/AuthService.cs#L11-L32)
- **Phân tích:**
  - Mật khẩu được mã hóa bằng `Microsoft.AspNetCore.Identity.PasswordHasher<object>` (PBKDF2 HMAC-SHA256/SHA512 với 100.000+ vòng lặp và muối ngẫu nhiên).
  - Token Bearer là chuỗi 32 byte ngẫu nhiên an toàn (`RandomNumberGenerator.GetBytes(32)` -> 64 ký tự hex).
  - Trong cơ sở dữ liệu bảng `dbo.App_Tokens`, hệ thống chỉ lưu `Token_Hash = SHA256(rawToken)`, không bao giờ lưu token gốc. Khi kiểm tra xác thực, token gửi lên được băm lại bằng SHA-256 để so khớp.
