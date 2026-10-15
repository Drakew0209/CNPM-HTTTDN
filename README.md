# Hệ Thống Thông Tin Doanh Nghiệp - Quản Lý Phòng Máy Cyber Cafe (CNPM-HTTTDN)

Dự án đồ án môn học **Công nghệ Phần mềm & Hệ thống Thông tin Doanh nghiệp**. Hệ thống quản trị vận hành toàn diện chuỗi phòng net / cyber cafe hiện đại chuẩn Enterprise, bao gồm cổng Web Admin cho quản lý và nhân viên, API Backend hiệu năng cao kết nối CSDL SQL Server, và ứng dụng Client WPF chạy trực tiếp trên máy trạm người chơi.

---

## 📁 Cấu Trúc Thư Mục Dự Án (Project Structure)

```text
CNPM-HTTTDN/
│
├── 📂 Database/                          # Thiết kế & Script cơ sở dữ liệu
│   ├── InternetCafe Final.sql            # Script khởi tạo cấu trúc CSDL SQL Server (28 bảng)
│   ├── MRP_Extension.sql                 # DDL & Seed phân hệ Quản lý Nguyên vật liệu (MRP & BOM)
│   └── Additional_Seed_Data.sql          # Dữ liệu mẫu (Seed Data) phòng máy, nhân sự, hàng hóa
│
├── 📂 SoDoVaMoTa/                        # Phân tích & Thiết kế hệ thống thông tin
│   ├── Chức năng.txt                     # Bảng phân rã chức năng nghiệp vụ
│   ├── Database_Design.md                # Tài liệu chi tiết thiết kế các bảng & ràng buộc CSDL
│   ├── System_Diagrams.md                # Mô tả các sơ đồ hệ thống
│   ├── Sơ đồ Chức năng (BFD).png         # Sơ đồ cấu trúc chức năng (Business Function Diagram)
│   ├── Sơ đồ Ngữ cảnh (Context Diagram).png # Sơ đồ ngữ cảnh hệ thống
│   ├── Sơ đồ Luồng Dữ liệu (DFD Level 0).png # Sơ đồ luồng dữ liệu mức đỉnh
│   ├── Sơ đồ Lược đồ CSDL.png            # Sơ đồ quan hệ thực thể (ERD)
│   └── SGU - ... - DO AN HTTTDN.docx.pdf # Tài liệu đặc tả đề tài đồ án
│
├── 📂 InternetCafe/                      # Mã nguồn chính của hệ thống (Monorepo)
│   ├── 📂 apps/
│   │   ├── 🌐 admin-web/                 # Cổng Web Quản trị (React 19, TypeScript, Vite, Ant Design 6)
│   │   ├── ⚙️ backend/                   # Backend API (ASP.NET Core 10, ADO.NET SqlClient, SignalR)
│   │   └── 🖥️ desktop-client/            # Ứng dụng Máy trạm Khách hàng (WPF .NET 10, XAML)
│   ├── 📂 contracts/                     # Hợp đồng giao tiếp API (OpenAPI 3.0 / Swagger YAML)
│   ├── 📂 mock/                          # MSW Mock Service Engine phục vụ phát triển giao diện
│   ├── 📂 docs/                          # Tài liệu quy tắc nghiệp vụ & ranh giới tích hợp
│   └── 📂 dist/                          # Thư mục xuất bản gói cài đặt (gitignored)
│
├── 📂 docs/
│   ├── DoiChieuYeuCau.md                 # Bảng đối chiếu yêu cầu & nhật ký khắc phục kiểm thử đồ án
│   └── 📂 client/                        # Trang Web & Tệp ClickOnce Deploy qua GitHub Pages
│
├── 🚀 chay-web-admin.bat                 # [1-Click] Khởi chạy Web Admin trên máy cục bộ
├── 🚀 chay-backend.bat                   # [1-Click] Khởi chạy Backend API
├── 🚀 chay-desktop-client.bat            # [1-Click] Khởi chạy Máy trạm WPF Client
├── 📦 dong-goi-portable.bat              # [1-Click] Đóng gói Client thành file ZIP di động (chạy mọi máy)
├── 🔄 dong-goi-clickonce.bat             # [1-Click] Xuất bản bản cập nhật Client ClickOnce tự động
└── 📄 README.md                          # Tài liệu hướng dẫn sử dụng tổng quan
```

---

## 🛠️ Công Nghệ Sử Dụng (Tech Stack)

| Thành phần | Công nghệ chính | Ghi chú |
| :--- | :--- | :--- |
| **Web Admin** | React 19 (`^19.2.0`), TypeScript, Vite, Ant Design 6 (`^6.6.5`), Recharts | Dashboard trực quan, Realtime Monitor, Quản lý kho MRP, HRM, CRM, Báo cáo |
| **Backend API** | ASP.NET Core 10 (`net10.0`), C#, SignalR, ADO.NET SqlClient | Kết nối SQL Server LocalDB, kiến trúc transaction an toàn, RESTful API |
| **Desktop Client** | WPF (Windows Presentation Foundation), .NET 10 (`net10.0-windows`), XAML | Màn hình khóa Kiosk, đặt món F&B, VietQR động, khảo sát ý kiến, phản hồi |
| **Cơ sở dữ liệu** | Microsoft SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) | Tự động kiểm tra và khởi tạo schema + dữ liệu mẫu khi backend khởi chạy |
| **Phân phối** | ClickOnce + Portable ZIP Self-Contained (Win-x64) | Tự động kiểm tra và nâng cấp phiên bản qua GitHub Pages |

---

## 🌟 Các Phân Hệ Nghiệp Vụ Chính (Core Modules)

### 1. Vận hành & Dịch vụ Phòng máy (Cyber Operations - ERP Core)
* **Sơ đồ phòng máy trực quan**: Theo dõi thời gian thực trạng thái máy (Available, InUse, Maintenance), kiểm soát kết nối heartbeat.
* **Quản lý phiên chơi & Thu ngân**: Mở phiên, tính cước giờ chơi theo giây, hỗ trợ **chuyển máy trạm** tức thời, tự động khóa khi hết tiền.
* **Nạp tiền & Ví khách hàng**: Nạp trực tiếp tại quầy, nạp qua mã VietQR động trên máy trạm với thông báo đẩy realtime cho thu ngân.
* **Thực đơn & Đơn dịch vụ F&B**: Gọi món từ máy trạm, quy trình chế biến (`Pending → Preparing → Served`), tự động giữ hàng và hoàn tiền/kho khi hủy.
* **Quản lý Hóa đơn**: Xem, tra cứu và in phiếu thanh toán hóa đơn theo mã/khách/máy (`InvoicesPage`).

### 2. Hoạch định Nhu cầu Nguyên vật liệu & Sản xuất (MRP & BOM)
* **Kho nguyên vật liệu (NVL)**: Quản lý danh mục nguyên vật liệu pha chế, đơn vị tính, đơn giá vốn và định mức tồn kho an toàn (`Reorder Level`).
* **Định mức công thức sản phẩm (BOM)**: Thiết lập tỷ lệ tiêu hao NVL cấu thành cho từng món ăn/đồ uống F&B.
* **Lệnh xuất kho & Sản xuất F&B**: Nhân viên đề xuất xuất kho NVL hoặc lệnh sản xuất món; Quản lý phê duyệt tự động kiểm tra đủ tồn kho, trừ NVL theo BOM và cộng số lượng món thành phẩm vào menu bán hàng.

### 3. Quản trị Nguồn Nhân lực (HRM)
* **Hồ sơ nhân sự**: Quản lý thông tin cá nhân, ngày sinh, bộ phận, chức vụ, trình độ và hợp đồng lao động.
* **Phân ca & Chấm công thông minh**: Phân chia ca làm việc; hệ thống chấm công **tự động đối chiếu khung giờ ca** (`Work_Shifts`) để gắn cờ tuân thủ: *Đúng giờ*, *Đi trễ X phút*, *Về sớm Y phút*.
* **Đơn nghỉ phép**: Hỗ trợ đầy đủ các loại đơn: Nghỉ phép thường niên (`Annual`), Nghỉ ốm (`Sick`), Nghỉ thai sản (`Maternity`), và Nguyện vọng thôi việc (`Resignation`).
* **Thăng chức & Phân quyền**: Quản lý/Chủ quán có thể cập nhật vị trí nhân viên (nhân viên → Trưởng nhóm ca / Quản lý ca), hệ thống tự động đồng bộ vai trò tài khoản (`App_Accounts.Role`).

### 4. Quản trị Quan hệ Khách hàng (CRM)
* **Hội viên & Phân hạng**: Quản lý thông tin khách, hạng thành viên (`Member`, `Silver`, `Gold`, `Diamond`), ngày sinh, sở thích.
* **Khảo sát ý kiến**: Soạn thảo bảng hỏi thăm dò (ra mắt menu/dịch vụ mới), gửi đến tài khoản khách hàng, tổng hợp thống kê kết quả từng phương án.
* **Hỗ trợ & Phản hồi**: Khách hàng gửi phản hồi hoặc bấm "Gọi hỗ trợ tại máy", ban quản lý tiếp nhận và phản hồi tức thời.

### 5. Báo cáo & Thống kê Quản trị
* **Bộ lọc linh hoạt**: Lọc theo khoảng ngày tự chọn hoặc chọn nhanh các mốc: *Tất cả*, *Hôm nay*, *Tháng này*, *Quý này*, *Năm nay*.
* **Báo cáo doanh thu**: Biểu đồ phân tích doanh thu theo ngày và tổng hợp theo tháng (Giờ chơi, F&B, Hoàn tiền, Doanh thu thuần, Dòng tiền nạp/rút).
* **Báo cáo món bán chạy**: Xếp hạng sản phẩm theo số lượng đặt và giá trị đóng góp.
* **Thống kê nhân sự & độ tuổi**: Biểu đồ cơ cấu phòng ban, trình độ, thâm niên, tháp độ tuổi nhân sự thực tế và tổng quỹ lương cơ bản.
* **Báo cáo giá trị kho NVL**: Theo dõi tồn kho thực tế, cảnh báo NVL dưới định mức và tổng giá trị vốn lưu kho.

---

## ⚡ Hướng Dẫn Khởi Chạy Nhanh (Quick Start)

### Cách 1: Khởi chạy bằng kịch bản 1-Click (Khuyên dùng)
Bạn chỉ cần nhấp đúp trực tiếp vào các file `.bat` tại thư mục gốc:
* **`chay-backend.bat`**: Bật API Backend tại `http://127.0.0.1:5055/api/v1` (tự động khởi tạo DB LocalDB)
* **`chay-web-admin.bat`**: Mở Web Quản trị tại `http://127.0.0.1:5173`
* **`chay-desktop-client.bat`**: Mở ứng dụng Máy trạm Desktop Client

### Cách 2: Khởi chạy bằng dòng lệnh (Terminal)

#### 1. Khởi chạy Backend API
```powershell
cd InternetCafe\apps\backend\InternetCafe.Backend
dotnet run    # Chạy trên port 5055, kết nối SQL Server LocalDB
```

#### 2. Khởi chạy Web Admin
```powershell
cd InternetCafe\apps\admin-web
npm install       # Chỉ cần chạy lần đầu
npm run demo:real # Chạy kết nối với Backend SQL Server thật
```

#### 3. Khởi chạy Desktop Client
```powershell
cd InternetCafe\apps\desktop-client
dotnet run --project InternetCafe.Client/InternetCafe.Client.csproj
```

---

## 👥 Danh Sách Tài Khoản Thử Nghiệm (Demo Accounts)

Tất cả tài khoản hệ thống dùng chung mật khẩu mặc định: **`Demo@123`**

| Tên đăng nhập | Vai trò (Role) | Chức năng trong hệ thống |
| :--- | :--- | :--- |
| **`owner`** | Chủ phòng máy | Toàn quyền kiểm soát hệ thống, xem báo cáo doanh thu, cấu hình giá, nhân sự |
| **`admin`** | Quản trị viên IT | Quản trị tài khoản, danh mục thiết bị, phân quyền người dùng hệ thống |
| **`manager`** | Quản lý | Theo dõi phòng máy, duyệt xuất kho MRP, kiểm duyệt chấm công và doanh thu |
| **`cashier`** | Thu ngân | Quản lý phiên máy, duyệt nạp tiền, xử lý đơn đồ ăn & nước uống (F&B) |
| **`staff`** | Nhân viên | Xem ca làm việc, chấm công vào/ra theo ca, gửi đơn xin nghỉ (phép/ốm/thai sản) |
| **`gamer`** | Khách hàng | Đăng nhập trên máy trạm WPF, theo dõi số dư, nạp tiền ví, đặt món trực tuyến |

---

## 📦 Đóng Gói & Phân Phối Ứng Dụng Khách Hàng (Deployment)

Hệ thống hỗ trợ 2 cơ chế phân phối phần mềm máy trạm:

### 1. Bản Portable ZIP (Dành cho máy lạ không có mạng hoặc máy chấm thi)
* Chạy file: **`dong-goi-portable.bat`**
* Kết quả sinh ra: `InternetCafe/dist/InternetCafe-Client-Portable.zip`
* **Ưu điểm**: Được đóng gói theo dạng *Self-Contained (Win-x64)* kèm sẵn .NET Runtime. Máy tính bất kỳ chỉ cần giải nén và mở `InternetCafe.Client.exe` là chạy được ngay, **không cần cài thêm bất kỳ phần mềm nào**, tự động bật chế độ Offline Demo mượt mà nếu máy chủ API chưa bật.

### 2. Bản Tự Động Cập Nhật (ClickOnce Auto-Update qua GitHub Pages)
* Chạy file: **`dong-goi-clickonce.bat`**
* Người dùng chỉ cần tải file cài đặt nhẹ (~500KB) từ trang GitHub Pages:
  👉 **`https://drakew0209.github.io/CNPM-HTTTDN/client/`**
* **Ưu điểm**: Khi bạn cập nhật code và chạy lại `dong-goi-clickonce.bat`, tất cả các máy trạm của khách hàng khi mở lên sẽ tự động tải và cập nhật phiên bản mới nhất mà không cần cài lại thủ công.
