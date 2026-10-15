# Kịch Bản Thuyết Trình & Hướng Dẫn Demo Đồ Án (Cheat Sheet)

Tài liệu này hướng dẫn chi tiết từng bước thao tác để trình diễn toàn bộ các tính năng "ăn điểm" nhất của hệ thống **Cyber Cafe Enterprise** trước Hội đồng / Giảng viên.

---

## 🚀 Bước 0: Chuẩn Bị Khởi Chạy Nhanh

Bạn chỉ cần nhấp đúp 3 file `.bat` tại thư mục gốc:
1. Nhấp đúp **`chay-backend.bat`**: Khởi chạy API Backend tại port `5055` (tự động kết nối SQL Server LocalDB, tạo schema 28 bảng và seed data mẫu).
2. Nhấp đúp **`chay-web-admin.bat`**: Mở Web Admin tại `http://127.0.0.1:5173` (kết nối trực tiếp Backend SQL Server thật).
3. Nhấp đúp **`chay-desktop-client.bat`**: Mở ứng dụng Máy trạm WPF Client.

---

## 🎯 Kịch Bản 1: Trải Nghiệm Khách Hàng Trên Máy Trạm (Desktop Client - WPF)

### 1.1. Đăng nhập & Mở khóa máy trạm
* Trên màn hình khóa WPF Client, đăng nhập bằng tài khoản:
  * **Tên đăng nhập**: `gamer`
  * **Mật khẩu**: `Demo@123`
* **Điểm nhấn**: Giao diện mở khóa chuyển sang Dashboard người chơi, hiển thị số dư ví, thời gian chơi còn lại được tính cước theo giây.

### 1.2. Nạp tiền ví qua mã VietQR động
* Trên Client, bấm **"Nạp tiền"** → nhập số tiền (ví dụ: `50,000 VND`) → chọn hình thức **Chuyển khoản (VietQR)**.
* Ứng dụng tự động render mã QR VietQR động kèm đúng số tiền và nội dung chuyển khoản.
* **Điểm nhấn realtime**: Ngay khi khách gửi yêu cầu nạp tiền, trên màn hình Web Admin của thu ngân sẽ xuất hiện **thông báo Toast nảy lên ở góc trái dưới màn hình** kèm hiệu ứng chuyển động và nút duyệt nhanh. Thu ngân bấm **"Duyệt"** → Số dư trên Desktop Client của khách lập tức được cộng thêm 50,000đ.

### 1.3. Đặt món ăn & Nước uống F&B từ máy trạm
* Khách chuyển sang tab **"Dịch vụ F&B"** trên máy trạm → chọn Mì cay trứng xúc xích hoặc Cà phê sữa → bấm **"Đặt món"**.
* Tiền được trừ tạm ứng từ ví và chuyển vào danh sách đơn đang chuẩn bị (`Preparing`).

### 1.4. Làm khảo sát thăm dò ý kiến khách hàng (CRM)
* Khách vào tab **"Khảo sát"** → chọn bài khảo sát đang mở (ví dụ: *Khảo sát nhu cầu thêm món ăn đêm*) → chọn đáp án và bấm **"Gửi câu trả lời"**.

### 1.5. Kiểm chứng Bảo vệ Máy trạm (Kiosk Lockdown)
* Khi máy ở trạng thái khóa / trong phiên, các phím điều hướng hệ thống (Phím Windows, Alt+Tab, Alt+F4, Ctrl+Esc) đều bị hook Win32 chặn.
* Trình quản lý tác vụ `taskmgr` nếu cố bật sẽ tự động bị đóng và đưa màn hình Kiosk lên trên cùng.
* **Thoát khẩn cấp**: Nhấn tổ hợp phím **`Ctrl + Shift + F12`** → nhập mật khẩu quản trị `Demo@123` để mở hộp thoại bypass khẩn cấp.

---

## 📦 Kịch Bản 2: Hoạch Định Nhu Cầu Nguyên Vật Liệu & Sản Xuất (MRP & BOM)
> *Đây là phân hệ cốt lõi chứng minh tính hoàn thiện của một Hệ thống Thông tin Doanh nghiệp hiện đại.*

* Đăng nhập Web Admin bằng tài khoản Quản lý:
  * **Tên đăng nhập**: `manager`
  * **Mật khẩu**: `Demo@123`
* Vào menu **"Sản xuất & Kho (MRP)"**:

### 2.1. Quản lý Kho Nguyên vật liệu (`materials`)
* Trình diễn bảng danh mục NVL: Hạt cà phê, sữa đặc, trứng gà, xúc xích, mì gói, đường,...
* Có đầy đủ: Đơn vị tính (g, ml, quả, gói), đơn giá vốn, tồn kho thực tế và định mức tồn an toàn (`Reorder Level`).
* Các nguyên vật liệu chạm ngưỡng tồn tối thiểu sẽ tự động có tag cảnh báo màu đỏ/cam.

### 2.2. Bảng Định mức Công thức Sản phẩm (`bom` - Bill of Materials)
* Mở tab **Định mức công thức (BOM)**:
  * Món *Cà phê sữa đá*: tiêu hao `25g` hạt cà phê xay + `40ml` sữa đặc.
  * Món *Mì cay đặc biệt*: tiêu hao `1 gói` mì + `1 quả` trứng gà + `1 cây` xúc xích.
* Giảng viên thấy rõ cơ chế định lượng sản xuất F&B thực tế của doanh nghiệp.

### 2.3. Lệnh sản xuất F&B tự động trừ kho NVL theo BOM
* Mở tab **Yêu cầu xuất NVL & Sản xuất**:
  * Tạo yêu cầu: Chọn mục đích **Sản xuất món** (`Product`) → Món: *Cà phê sữa đá* → Số lượng: `5 ly`.
  * Quản lý bấm **"Duyệt lệnh sản xuất"**.
  * **Hệ quả tự động**:
    1. Hệ thống tự động kiểm tra kho NVL có đủ nguyên liệu không.
    2. Tự động tính toán và trừ chính xác `125g` hạt cà phê và `200ml` sữa đặc trong kho nguyên vật liệu.
    3. Tự động cộng thêm `5 ly` vào số lượng món hoàn thành trong menu thực đơn bán hàng.

---

## 👥 Kịch Bản 3: Quản Trị Nhân Sự & Chấm Công Thông Minh (HRM)

### 3.1. Chấm công theo ca có kiểm tra tuân thủ giờ
* Đăng nhập bằng tài khoản Nhân viên:
  * **Tên đăng nhập**: `staff`
  * **Mật khẩu**: `Demo@123`
* Hệ thống tự động chuyển về **Cổng nhân viên** (`/staff/home`):
  * Bấm **"Chấm công vào"**: Hệ thống tự động so khớp giờ hiện tại với giờ bắt đầu ca trong CSDL:
    * Nếu vào trễ hơn 5 phút: Gắn nhãn **`Đi trễ X phút`** (màu cam đỏ).
    * Nếu vào đúng giờ: Gắn nhãn **`Đúng giờ`** (màu xanh lá).
  * Bấm **"Chấm công ra"**: Nếu về sớm, hệ thống gắn nhãn **`Về sớm Y phút`**.

### 3.2. Nộp đơn xin nghỉ thai sản (`Maternity`)
* Nhân viên vào tab "Đơn nghỉ của tôi" → bấm **"Gửi đơn nghỉ"**.
* Chọn loại đơn: **Nghỉ thai sản** (`Maternity`) → nhập khoảng thời gian và lý do → Gửi.
* Quản lý đăng nhập vào module `leaves` bấm **"Duyệt đơn"** → Lịch làm việc trong khoảng thời gian này tự động chuyển sang trạng thái `OnLeave` (Nghỉ phép).

### 3.3. Thống kê tháp tuổi, thâm niên & quỹ lương
* Đăng nhập tài khoản `manager` hoặc `owner` → vào mục **Báo cáo vận hành** → tab **"Nhân sự & tiền lương"**:
  * Hiển thị **Biểu đồ nhóm tuổi nhân sự** dựa trên ngày sinh thực tế (`dateOfBirth`).
  * Hiển thị metric **Tổng quỹ lương cơ bản tháng** và phân loại nhân sự đang làm việc vs đã thôi việc.
  * Cột Lương cơ bản xuất hiện đầy đủ trong bảng dữ liệu nhân sự.

### 3.4. Thăng chức & phân quyền động
* Vào module **Hồ sơ nhân sự** → sửa chức vụ của một nhân viên từ Kỹ thuật viên thành **Trưởng nhóm kỹ thuật / ca** hoặc **Quản lý ca**.
* Hệ thống tự động đồng bộ nâng vai trò `Role` của tài khoản nhân viên từ `Staff` lên `Manager`.

---

## 📊 Kịch Bản 4: Quản Lý Phiên Chơi, Chuyển Máy & Hóa Đơn

### 4.1. Chuyển máy trạm (Transfer Session)
* Trên trang **Phòng máy** (`MachinesPage`), chọn một máy đang có khách chơi (ví dụ PC01).
* Bấm **"Chuyển máy"** → chọn máy đích còn trống (ví dụ PC02) → Xác nhận.
* Khách được chuyển sang PC02 ngay lập tức, tiền cước và thời gian chơi được bảo lưu liên tục.

### 4.2. Quản lý & In ấn Hóa đơn (`InvoicesPage`)
* Vào mục **Quản lý hóa đơn**:
  * Tra cứu theo 3 loại: Hóa đơn Nạp tiền ví, Hóa đơn Dịch vụ F&B, Hóa đơn Giờ chơi phòng máy.
  * Bấm **"Xem chi tiết & In hóa đơn"**: Mở phiếu thu chuẩn hóa và gọi lệnh in trực tiếp từ trình duyệt (`window.print()`).

### 4.3. Báo cáo Doanh thu đa kỳ (Tháng / Quý / Năm)
* Vào mục **Báo cáo vận hành** → tab **Doanh thu**:
  * Bấm thử các nút chọn nhanh: **Hôm nay**, **Tháng này**, **Quý này**, **Năm nay**.
  * Quan sát biểu đồ **Tổng hợp doanh thu theo tháng** (`Doanh thu theo tháng`) phân tích chi tiết dòng tiền: Doanh thu giờ chơi, Doanh thu F&B, Hoàn tiền và Doanh thu thuần.
  * Có nút bấm xuất file **CSV** hoặc **In báo cáo** trực tiếp.

---

## 🛡️ Bảng Tổng Kết Tài Khoản Demo

| Vai trò | Tên đăng nhập | Mật khẩu | Phạm vi thao tác chính |
| :--- | :--- | :--- | :--- |
| **Chủ quán (Owner)** | `owner` | `Demo@123` | Toàn quyền quản trị vận hành, xem mọi báo cáo tài chính |
| **Quản trị IT (Admin)** | `admin` | `Demo@123` | Quản trị tài khoản, danh mục thiết bị, phân quyền hệ thống |
| **Quản lý (Manager)** | `manager` | `Demo@123` | Duyệt kho MRP & BOM, duyệt đơn nghỉ, chấm công, xem báo cáo |
| **Thu ngân (Cashier)** | `cashier` | `Demo@123` | Điều khiển phòng máy, duyệt nạp tiền VietQR, xuất đơn F&B |
| **Nhân viên (Staff)** | `staff` | `Demo@123` | Chấm công vào/ra theo ca, gửi đơn nghỉ phép/thai sản |
| **Khách chơi (Gamer)** | `gamer` | `Demo@123` | Đăng nhập máy trạm WPF, nạp tiền ví, gọi món, khảo sát |
