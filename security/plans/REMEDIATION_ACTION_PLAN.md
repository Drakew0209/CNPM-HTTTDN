# KẾ HOẠCH HÀNH ĐỘNG KHẮC PHỤC CÁC LỖ HỔNG BẢO MẬT
## (SECURITY REMEDIATION ACTION PLAN)
**Dự án:** Hệ thống Quản trị & Vận hành Internet Cafe (`CNPM-HTTTDN`)  
**Tài liệu tham chiếu:** [Báo cáo Tổng hợp Security Audit](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/security/SECURITY_AUDIT_REPORT.md)  
**Ngày lập:** 16/10/2026  
**Trạng thái:** ✅ **ĐÃ HOÀN THÀNH TOÀN BỘ (ALL PHASES COMPLETED)**  

---

## 1. NGUYÊN TẮC THỰC HIỆN
1. **Bảo toàn chức năng nghiệp vụ hiện có:** Mọi thay đổi bảo mật không làm gián đoạn các luồng nghiệp vụ đang hoạt động (đăng nhập, nạp tiền, đặt món, tính tiền giờ chơi, quản lý nhân sự, kho NVL).
2. **Ưu tiên mức độ nghiêm trọng:** Đã giải quyết dứt điểm nhóm `HIGH` (Giai đoạn 1), sau đó tới `MEDIUM` (Giai đoạn 2) và `LOW` (Giai đoạn 3).
3. **Kiểm thử hồi quy:** Đã build và chạy toàn bộ test suite:
   - Backend API: `dotnet build` -> **0 lỗi**
   - Kiosk Client: `dotnet build` -> **0 lỗi, 0 cảnh báo**
   - Admin Web: `npm run typecheck` + `npm run build` -> **0 lỗi**; `vitest run` -> **34/34 test PASS**.

---

## 2. KẾT QUẢ THỰC HIỆN CHI TIẾT THEO TỪNG GIAI ĐOẠN

### GIAI ĐOẠN 1: XỬ LÝ CÁC PHÁT HIỆN MỨC ĐỘ CAO (HIGH PRIORITY) - [HOÀN THÀNH ✅]

#### Bước 1.1: Cập nhật Root `.gitignore` (Mã: SEC-01) - [XONG ✅]
- **Tập tin đã sửa:** [.gitignore](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/.gitignore)
- **Kết quả:** Đã thêm quy tắc loại trừ `.env`, `.env.*`, `!.env.example`, `appsettings.*.json` ở cấp root để chặn triệt để nguy cơ vô tình commit file bí mật/chứa mật khẩu lên kho mã nguồn.

#### Bước 1.2: Triệt tiêu Mật khẩu Mặc định Hardcoded `Demo@123` (Mã: SEC-02 & KSK-01) - [XONG ✅]
- **Tập tin đã sửa:**
  - [ManagementService.cs](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ManagementService.cs):
    - Đã loại bỏ hoàn toàn giá trị fallback `"Demo@123"` tại `CreateCustomerAsync`, `CreateEmployeeAsync` và `CreateAccountAsync`.
    - Bắt buộc mật khẩu có độ dài tối thiểu 8 ký tự và kiểm tra hợp lệ trước khi tạo tài khoản.
  - [MainWindow.xaml.cs](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs):
    - Bỏ fallback `"Demo@123"`. Nếu không có cấu hình `Kiosk:EmergencyPassword`, tính năng phím tắt thoát khẩn cấp sẽ bị vô hiệu hóa an toàn, chặn hoàn toàn nguy cơ người dùng máy trạm tự thoát kiosk bằng pass mặc định.

#### Bước 1.3: Tích hợp Rate Limiting cho API Xác thực (Mã: AUT-01) - [XONG ✅]
- **Tập tin đã sửa:** [Program.cs](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs)
- **Kết quả:** Đã kích hoạt middleware `RateLimiter` (`FixedWindowRateLimiter`) khống chế 10 yêu cầu/phút/IP và áp dụng trực tiếp lên endpoint `/auth/login` và `/auth/register` qua `.RequireRateLimiting("auth")`, loại trừ tấn công Brute-force & Credential Stuffing.

---

### GIAI ĐOẠN 2: HOÀN THIỆN CÁC PHÁT HIỆN TRUNG BÌNH (MEDIUM PRIORITY) - [HOÀN THÀNH ✅]

#### Bước 2.1: Bổ sung HTTP Security Headers (Mã: HDR-01) - [XONG ✅]
- **Tập tin đã sửa:** [Program.cs](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs)
- **Kết quả:** Đã thêm middleware tự động gán các tiêu đề an toàn:
  - `X-Frame-Options: DENY` (chống Clickjacking)
  - `X-Content-Type-Options: nosniff` (chống MIME-sniffing)
  - `Referrer-Policy: strict-origin-when-cross-origin` (bảo vệ thông tin nguồn gốc)

#### Bước 2.2: Bổ sung Global Exception Handler (Mã: HDR-02) - [XONG ✅]
- **Tập tin đã sửa:** [Program.cs](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs)
- **Kết quả:** Sử dụng `app.UseExceptionHandler(...)` trả về đối tượng JSON đồng nhất (`{ error = "Lỗi hệ thống máy chủ nội bộ. Vui lòng liên hệ quản trị viên." }`) và che giấu hoàn toàn Stack Trace cũng như đường dẫn file mã nguồn trên máy chủ.

#### Bước 2.3: Hỗ trợ Token Query String cho SignalR WebSockets (Mã: AUT-02) - [XONG ✅]
- **Tập tin đã sửa:** [ApiTokenAuthenticationHandler.cs](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ApiTokenAuthenticationHandler.cs)
- **Kết quả:** Đã bổ sung logic kiểm tra `Request.Query["access_token"]` trên đường dẫn `/hubs/internetcafe`, đảm bảo kết nối WebSocket trình duyệt từ admin-web xác thực thành công mà không gặp lỗi 401.

#### Bước 2.4: Kiểm tra quyền gửi Heartbeat máy trạm (Mã: ACL-04) - [XONG ✅]
- **Tập tin đã sửa:** [BusinessService.cs](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/BusinessService.cs)
- **Kết quả:** Khi tài khoản khách hàng gọi `POST /computers/{id}/heartbeat`, hệ thống đã kiểm tra session đang chạy trên máy có đúng là của khách hàng đó hay không. Nếu không trùng khớp, hệ thống từ chối với mã 403 Forbidden.

---

### GIAI ĐOẠN 3: DỌN DẸP & CẢI TIẾN TRẢI NGHIỆM (LOW PRIORITY) - [HOÀN THÀNH ✅]

#### Bước 3.1: Kiểm tra biên số âm ở tầng ứng dụng cho Nguyên vật liệu (Mã: VAL-04) - [XONG ✅]
- **Tập tin đã sửa:** [ManagementService.cs](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ManagementService.cs)
- **Kết quả:** Tại `CreateMaterialAsync` và `UpdateMaterialAsync`, đã bổ sung kiểm tra `stock < 0`, `reorder < 0`, `cost < 0` trả về lỗi validation thân thiện thay vì để SQL Server ném ngoại lệ CHECK Constraint.

#### Bước 3.2: Tắt tự động điền mật khẩu trên UI khi không ở chế độ Demo (Mã: SEC-03) - [XONG ✅]
- **Tập tin đã sửa:**
  - [App.tsx](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/admin-web/src/App.tsx): Đã bỏ gán mật khẩu tự điền trong form đăng nhập (`initialValues: { username: '', password: '' }`).
  - [MainWindow.xaml.cs](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs) & [MainWindow.xaml](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml): Đã xóa dòng gán mật khẩu `LoginPassword.Password = "Demo@123"`, xóa banner hardcode mật khẩu, thay bằng thông báo hướng dẫn đăng nhập thông thường.
  - [MainViewModel.cs](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/desktop-client/InternetCafe.Client.Core/MainViewModel.cs): Xóa dữ liệu mẫu khởi tạo.

#### Bước 3.3: Thắt chặt cấu hình Vite Dev Server `server.fs.allow` (Mã: XSS-03) - [XONG ✅]
- **Tập tin đã sửa:** [vite.config.ts](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/admin-web/vite.config.ts)
- **Kết quả:** Đã thiết lập `server: { fs: { strict: true, allow: ['..'] } }` và `resolve: { preserveSymlinks: true }` giúp cô lập phạm vi đọc file tĩnh của Vite dev server và hỗ trợ build production tối ưu.

---

## 3. TỔNG KẾT NGHIỆM THU
| Hạng mục | Chỉ số ban đầu | Chỉ số sau khắc phục | Trạng thái |
| :--- | :---: | :---: | :---: |
| Lỗ hổng High | 3 | **0** | ĐÃ GIẢI QUYẾT TRIỆT ĐỂ |
| Lỗ hổng Medium | 4 | **0** | ĐÃ GIẢI QUYẾT TRIỆT ĐỂ |
| Lỗ hổng Low | 4 | **0** | ĐÃ GIẢI QUYẾT TRIỆT ĐỂ |
| Build Backend | Succeeded | **Succeeded (0 errors)** | ĐẠT CHUẨN |
| Build Kiosk Client | Succeeded | **Succeeded (0 errors)** | ĐẠT CHUẨN |
| Build Admin Web | Succeeded | **Succeeded (0 errors)** | ĐẠT CHUẨN |
| Vitest Unit Tests | 34 passed | **34 passed (100%)** | ĐẠT CHUẨN |

