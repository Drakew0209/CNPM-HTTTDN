# Hệ Thống Thông Tin Doanh Nghiệp - Quản Lý Phòng Máy Cyber Cafe (CNPM-HTTTDN)

Dự án đồ án môn học **Công nghệ Phần mềm & Hệ thống Thông tin Doanh nghiệp**. Hệ thống quản trị vận hành toàn diện chuỗi phòng net / cyber cafe hiện đại, bao gồm cổng Web Admin cho nhân viên/quản lý, API Backend hiệu năng cao, và ứng dụng Client WPF chạy trực tiếp trên máy trạm người chơi.

---

## 📁 Cấu Trúc Thư Mục Dự Án (Project Structure)

```text
CNPM-HTTTDN/
│
├── 📂 Database/                          # Thiết kế & Script cơ sở dữ liệu
│   ├── InternetCafe Final.sql            # Script khởi tạo cấu trúc CSDL SQL Server/SQLite
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
│   │   ├── 🌐 admin-web/                 # Cổng Web Quản trị (React, Vite, TypeScript)
│   │   ├── ⚙️ backend/                   # Backend API (ASP.NET Core 9.0, SignalR, EF Core)
│   │   └── 🖥️ desktop-client/            # Ứng dụng Máy trạm Khách hàng (WPF .NET 9.0)
│   ├── 📂 contracts/                     # Hợp đồng giao tiếp API (OpenAPI 3.0 / Swagger YAML)
│   ├── 📂 mock/                          # MSW Mock Service Engine phục vụ phát triển giao diện
│   ├── 📂 docs/                          # Tài liệu quy tắc nghiệp vụ & ranh giới tích hợp
│   └── 📂 dist/                          # Thư mục xuất bản gói cài đặt (gitignored)
│
├── 📂 docs/
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
| **Web Admin** | React 18, TypeScript, Vite, Tailwind/Vanilla CSS, Lucide Icons | Dashboard trực quan, Realtime Monitor, Quản lý kho, Nhân sự, Báo cáo |
| **Backend API** | ASP.NET Core 9.0, C#, SignalR, Entity Framework Core / Dapper | Hỗ trợ cả SQL Server LocalDB và SQLite nhúng, RESTful API |
| **Desktop Client** | WPF (Windows Presentation Foundation), .NET 9.0, XAML | Giao diện Kiosk Lock Screen, Mini Widget, Smart Offline Fallback |
| **Cơ sở dữ liệu** | SQL Server / SQLite | Đồng bộ tự động schema và seed dữ liệu ban đầu |
| **Phân phối** | ClickOnce + Portable ZIP Self-Contained | Tự động kiểm tra và nâng cấp phiên bản qua GitHub Pages |

---

## ⚡ Hướng Dẫn Khởi Chạy Nhanh (Quick Start)

### Cách 1: Khởi chạy bằng kịch bản 1-Click (Khuyên dùng)
Bạn chỉ cần nhấp đúp trực tiếp vào các file `.bat` tại thư mục gốc:
* **`chay-backend.bat`**: Bật API Backend tại `http://127.0.0.1:5055/api/v1`
* **`chay-web-admin.bat`**: Mở Web Quản trị tại `http://127.0.0.1:5173`
* **`chay-desktop-client.bat`**: Mở ứng dụng Máy trạm Desktop Client

### Cách 2: Khởi chạy bằng dòng lệnh (Terminal)

#### 1. Khởi chạy Web Admin
```powershell
cd InternetCafe\apps\admin-web
npm install   # Chỉ cần chạy lần đầu
npm run dev   # Mở http://127.0.0.1:5173
```

#### 2. Khởi chạy Backend API
```powershell
cd InternetCafe\apps\backend\InternetCafe.Backend
dotnet run    # Chạy trên port 5055
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
| **`admin`** | Quản trị viên IT | Quản trị tài khoản, danh mục thiết bị, quyền truy cập hệ thống |
| **`manager`** | Quản lý | Theo dõi phòng máy, nhập xuất kho, kiểm duyệt chấm công và doanh thu ca |
| **`cashier`** | Thu ngân | Quản lý phiên máy, duyệt nạp tiền, xử lý đơn đồ ăn & nước uống (F&B) |
| **`staff`** | Nhân viên | Xem ca làm việc, chấm công, bảng lương cá nhân, gửi đơn xin nghỉ |
| **`gamer`** | Khách hàng | Đăng nhập trên máy trạm WPF, theo dõi số dư, nạp tiền ví, đặt món trực tuyến |

---

## 📦 Đóng Gói & Phân Phối Ứng Dụng Khách Hàng (Deployment)

Hệ thống hỗ trợ 2 cơ chế phân phối phần mềm máy trạm:

### 1. Bản Portable ZIP (Dành cho máy lạ không có mạng hoặc máy chấm thi)
* Chạy file: **`dong-goi-portable.bat`**
* Kết quả sinh ra: `InternetCafe/dist/InternetCafe-Client-Portable.zip` (~67MB)
* **Ưu điểm**: Được đóng gói theo dạng *Self-Contained (Win-x64)* kèm sẵn .NET Runtime. Máy tính bất kỳ chỉ cần giải nén và mở `InternetCafe.Client.exe` là chạy được ngay, **không cần cài thêm bất kỳ phần mềm nào**, tự động bật chế độ Offline Demo mượt mà nếu máy chủ API chưa bật.

### 2. Bản Tự Động Cập Nhật (ClickOnce Auto-Update qua GitHub Pages)
* Chạy file: **`dong-goi-clickonce.bat`**
* Người dùng chỉ cần tải file cài đặt nhẹ (~500KB) từ trang GitHub Pages:
  👉 **`https://drakew0209.github.io/CNPM-HTTTDN/client/`**
* **Ưu điểm**: Khi bạn cập nhật code và chạy lại `dong-goi-clickonce.bat`, tất cả các máy trạm của khách hàng khi mở lên sẽ tự động tải và cập nhật phiên bản mới nhất mà không cần cài lại thủ công.
