# BÁO CÁO KIỂM TRA BẢO MẬT: MÔI TRƯỜNG MÁY TRẠM CLIENT & CHẾ ĐỘ KIOSK
**Mã phân loại vibe-check:** `13. CLIENT_SECURITY`  
**Dự án:** Ứng dụng Desktop Client Máy trạm (`InternetCafe.Client`)  
**Ngày kiểm định:** 16/10/2026  
**Trạng thái kiểm tra:** Hoàn tất  

---

## 1. TỔNG QUAN HẠNG MỤC
Đối với hệ thống phòng máy Internet Cafe, ứng dụng Client đóng vai trò như một Kiosk Shell khóa máy toàn màn hình (Full-screen Kiosk) ngăn người dùng trái phép truy cập màn hình Windows, Command Prompt, hoặc Task Manager trước khi đăng nhập và có phiên chơi hợp lệ. Hạng mục này kiểm tra khả năng thoát (breakout) khỏi chế độ Kiosk, mật khẩu khẩn cấp và bảo vệ bàn phím.

---

## 2. BẢNG TỔNG HỢP KẾT QUẢ KIỂM TRA

| Mã phát hiện | Hạng mục kiểm tra | Mức độ nghiêm trọng | Trạng thái xác minh | Vị trí (File & Dòng) |
| :--- | :--- | :---: | :---: | :--- |
| **KSK-01** | Mật khẩu thoát khẩn cấp Kiosk Mode mặc định `Demo@123` | **HIGH** | Đã xác minh (Verified) | [MainWindow.xaml.cs:57](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs#L57) |
| **KSK-02** | Khóa các tổ hợp phím hệ thống nguy hiểm (Alt+Tab, WinKey, TaskMgr) | **PASS** | Đã xác minh (Verified) | [MainWindow.xaml.cs:58, 63, 114-120](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs#L58) |
| **KSK-03** | Khởi chạy game từ danh sách với đường dẫn thực thi an toàn | **PASS** | Đã xác minh (Verified) | [MainViewModel.cs](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/desktop-client/InternetCafe.Client.Core/MainViewModel.cs) |

---

## 3. CHI TIẾT CÁC PHÁT HIỆN

### Phát hiện KSK-01: Mật khẩu thoát khẩn cấp Kiosk Mode mặc định `Demo@123`
- **Mức độ:** `HIGH`
- **Tập tin liên quan:** [MainWindow.xaml.cs:57](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs#L57)
- **Bằng chứng mã nguồn:**
  ```csharp
  emergencyPassword = config["Kiosk:EmergencyPassword"] ?? "Demo@123";
  ```
- **Phân tích rủi ro:**
  - Nếu file cấu hình không đặt mật khẩu, mã nguồn sẽ tự gán `Demo@123` làm mật khẩu vượt quyền Kiosk.
  - Người dùng có thể nhấn tổ hợp phím thoát khẩn cấp và nhập `Demo@123` để mở khóa màn hình, vô hiệu hóa Hook bàn phím và truy cập trực tiếp vào Windows của máy trạm mà không cần tạo phiên chơi hoặc trả tiền.
- **Biện pháp khắc phục khuyến nghị:**
  - Không fallback về một chuỗi cố định công khai.
  - Nếu cấu hình `Kiosk:EmergencyPassword` bị trống, vô hiệu hóa hoàn toàn phím tắt thoát khẩn cấp hoặc yêu cầu xác thực API bằng tài khoản Quản lý (Manager/Owner).

---

### Phát hiện KSK-02: Cơ chế khóa phím hệ thống và giám sát Task Manager
- **Mức độ:** `PASS` (Đạt chuẩn)
- **Tập tin liên quan:** [MainWindow.xaml.cs:58, 63, 114-120](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs#L58)
- **Phân tích:**
  - Ứng dụng cài đặt Low-level Keyboard Hook qua Windows API (`SetWindowsHookEx`) để chặn các phím tắt nguy hiểm (phím Windows, Alt+Tab, Alt+Esc, Ctrl+Esc).
  - Có tiến trình `taskManagerGuard` chạy định kỳ 1.5 giây để phát hiện và thu hồi quyền nếu Task Manager cố tình khởi chạy trong lúc máy đang bị khóa.
  - Khi chưa đăng nhập hoặc khi máy bị khóa tạm thời (`IsTemporaryLocked`), cửa sổ luôn ở chế độ `Topmost = true`, chiếm toàn bộ màn hình `SystemParameters.PrimaryScreenWidth` và chặn tương tác chuột ngoài màn hình khóa.
