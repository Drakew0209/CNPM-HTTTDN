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
├── 🚀 chay-toan-bo.bat                   # [1-Click] Khởi chạy toàn bộ hệ thống (DB + Backend + Web + Client)
├── 🚀 chay-demo-2-may.bat                # [1-Click] Khởi chạy kịch bản Demo 2 máy trạm (PC01 + PC02) song song trên 1 Laptop
├── 🧪 chay-playwright-tests.bat          # [1-Click] Khởi chạy toàn bộ 34 Test Cases E2E tự động (Web Admin + App)
├── 🧪 chay-postman-tests.bat             # [1-Click] Khởi chạy toàn bộ 115 Test Cases API Newman / Postman
├── 🛑 dung-toan-bo.bat                   # [1-Click] Đóng và giải phóng toàn bộ tiến trình hệ thống
├── 🚀 chay-web-admin.bat                 # [1-Click] Khởi chạy riêng Web Admin
├── 🚀 chay-backend.bat                   # [1-Click] Khởi chạy riêng Backend API
├── 🚀 chay-desktop-client.bat            # [1-Click] Khởi chạy riêng Máy trạm WPF Client
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
* **`chay-toan-bo.bat`**: **[Khuyên dùng]** Tự động bật SQL Server LocalDB, Backend API, Web Admin và 1 Desktop Client chỉ với 1 click!
* **`chay-demo-2-may.bat`**: **[Dành riêng cho Báo cáo / Thuyết trình]** Tự động mở Web Admin bên trái và 2 máy trạm độc lập (PC01 + PC02) ở chế độ cửa sổ bên phải để demo tương tác 2 chiều thời gian thực trên cùng 1 laptop!
* **`chay-playwright-tests.bat`**: **[Kiểm thử E2E Giao diện]** Tự động chạy toàn bộ **34 Test Cases E2E Playwright** bao phủ toàn bộ Web Admin + App máy trạm, sinh báo cáo HTML trực quan.
* **`chay-postman-tests.bat`**: **[Kiểm thử API]** Tự động chạy toàn bộ **115 Test Cases API Postman / Newman** kiểm tra xác thực, nghiệp vụ ví, F&B, MRP và bảo mật.
* **`dung-toan-bo.bat`**: Đóng và giải phóng toàn bộ các cổng/tiến trình hệ thống khi kết thúc buổi làm việc.
* Hoặc khởi chạy riêng lẻ từng ứng dụng:
  * **`chay-backend.bat`**: Bật API Backend tại `http://0.0.0.0:5055/api/v1` (hỗ trợ kết nối LAN Wi-Fi trường)
  * **`chay-web-admin.bat`**: Mở Web Quản trị tại `http://127.0.0.1:5173`
  * **`chay-desktop-client.bat`**: Mở ứng dụng Máy trạm (hỗ trợ truyền mã máy, ví dụ: `chay-desktop-client.bat PC02 --windowed`)

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

---

## 🎬 Kịch Bản Demo Trình Diễn Báo Cáo (Presentation & Demo Guide)

> Phần hướng dẫn này được thiết kế tối ưu cho sinh viên khi thuyết trình đồ án trước Hội đồng / Giảng viên hướng dẫn, giải quyết triệt để vấn đề **chỉ có 1 chiếc laptop** hoặc **kết nối mạng Wi-Fi trường học**.

### 1. Kiến trúc Thực tế vs. Cơ chế Mô phỏng Demo

* **Trong quán Net thực tế**: Máy chủ đặt tại quầy thu ngân (IP tĩnh LAN). Các máy trạm PC01..PCxx dùng BootROM, tự định danh theo tên máy tính Windows (`ComputerName`) và gửi nhịp tim về máy chủ.
* **Khi thuyết trình (1 Laptop)**: Hệ thống cung cấp cơ chế **Multi-Station Windowed Simulation** cho phép mở đồng thời nhiều máy trạm độc lập ngay trên cùng một màn hình mà không bị chiếm dụng toàn màn hình hay xung đột dữ liệu.

---

### 2. Kịch Bản 1: Trình diễn 2 Máy trạm song song trên 1 Laptop (Khuyên dùng)

Chỉ với 1 chiếc laptop, bạn có thể cho giảng viên thấy khả năng quản trị đa máy trạm thời gian thực (Realtime SignalR):

```powershell
# 1-Click khởi chạy toàn bộ môi trường demo:
chay-demo-2-may.bat
```

#### 📐 Bố cục màn hình đề xuất (Split Screen)
* **Nửa màn hình bên trái**: Trình duyệt mở **Web Admin** (`http://127.0.0.1:5173`) tại trang **Phòng máy** (đăng nhập `admin` hoặc `cashier`).
* **Nửa màn hình bên phải**: Xếp 2 cửa sổ máy trạm Client:
  * Cửa sổ trên: Máy **PC01** (Khu Tiêu chuẩn - 8.000 ₫/h).
  * Cửa sổ dưới: Máy **PC02** (Khu VIP - 15.000 ₫/h).

#### 📋 Các bước biểu diễn thuyết trình mẫu (10 Bước ghi điểm trọn vẹn):

| Bước | Thao tác thực hiện | Hiện tượng quan sát được (Điểm cộng thuyết trình) |
| :---: | :--- | :--- |
| **B1** | Quan sát Web Admin ban đầu | Cả 2 ô **PC01** và **PC02** đều sáng đèn xanh **Online** (Sẵn sàng phục vụ), phản hồi nhịp tim heartbeat tự động. |
| **B2** | Đăng nhập tại máy **PC01** với tài khoản `gamer` (Mật khẩu: `Demo@123`) | Client PC01 tự động bắt đầu tính giờ chơi. **Ngay lập tức**, ô PC01 trên Web Admin chuyển sang màu xanh dương **Đang sử dụng (InUse)**, hiển thị tên khách *Nguyễn Văn An* và đồng hồ tính cước nhảy theo từng giây qua SignalR. |
| **B3** | Đăng nhập tại máy **PC02** với tài khoản `user02` (Mật khẩu: `Demo@123`) | Cả 2 máy cùng hoạt động song song độc lập. PC02 tự động áp dụng mức cước phòng VIP (15.000 ₫/h) cao hơn PC01. |
| **B4** | Bấm **"Cửa sổ gọn"** trên máy PC01 | Client PC01 tự động thu nhỏ về thanh widget nổi góc trên bên phải màn hình để người chơi chơi game/lướt web, không che màn hình. |
| **B5** | Nạp tiền ví bằng **VietQR động** | Tại Client, khách bấm **"Nạp tiền"** -> Nhập số tiền -> Hệ thống sinh mã VietQR động kèm cú pháp chuyển khoản. Trên Web Admin, Thu ngân nhận thông báo nạp tiền tức thời và duyệt tiền vào tài khoản. |
| **B6** | Đặt món F&B từ máy trạm | Trên PC02, khách vào Menu chọn nước ngọt / mì trứng -> bấm Đặt món. Trên Web Admin của thu ngân lập tức hiện chuông báo đơn món mới cần chuẩn bị (`Pending → Preparing → Served`). |
| **B7** | Thu ngân thực hiện **Chuyển máy** từ Web | Trên Web Admin, bấm vào ô PC01 -> Chọn **"Chuyển máy cho khách"** -> Chọn chuyển sang **PC03**. Máy PC01 tự động khóa lại và thông báo chuyển máy, máy PC03 sáng đèn tiếp tục phiên chơi mà không mất số dư. |
| **B8** | Khảo sát ý kiến (CRM Survey) & Phản hồi | Khách làm khảo sát ý kiến tại máy trạm hoặc gửi phản hồi góp ý. Web Admin phân hệ CRM cập nhật biểu đồ thống kê kết quả bình chọn theo thời gian thực. |
| **B9** | Quản lý kho MRP & Xuất kho NVL | Vào phân hệ **Kho & MRP** trên Web Admin, quản lý xem định mức BOM các món ăn, phê duyệt lệnh xuất kho nguyên vật liệu (trừ tự động theo công thức). |
| **B10** | Lệnh điều khiển từ xa (**Remote Command**) | Trên Web Admin, chọn máy -> Gửi lệnh **"Khóa máy"** hoặc **"Khởi động lại (Restart)"** -> Client lập tức nhận lệnh từ xa và khóa màn hình theo thời gian thực. |

---

### 3. Kịch Bản 2: Trình diễn Đa máy qua Mạng LAN / Wi-Fi Trường học

Khi có thêm một laptop khác (của bạn bè hoặc máy chấm thi) kết nối vào cùng mạng Wi-Fi trường:

1. **Trên Laptop chủ (Server)**:
   * Mở CMD chạy lệnh `ipconfig` để lấy địa chỉ IPv4 (ví dụ: `192.168.1.88`).
   * Chạy file `chay-toan-bo.bat` (Backend tự động lắng nghe trên mọi card mạng `0.0.0.0:5055` và cấu hình CORS LAN cho phép kết nối an toàn).
2. **Trên Laptop máy trạm (Client)**:
   * Mở app Desktop Client.
   * Nhấn phím **`F2`** (hoặc nhấp nút **`CẤU HÌNH (F2)`** trên màn hình khóa):
     * **Mã máy trạm**: Chọn `PC02` (hoặc PC03..).
     * **Địa chỉ máy chủ**: Nhập `http://192.168.1.88:5055` (IP của laptop chủ).
     * Bấm **Áp dụng ngay**.
   * Máy trạm sẽ lập tức kết nối về máy chủ của bạn và hoạt động 100% như phòng net thực thụ.

---

### 4. Bảng Phím Tắt & Tiện Ích Hỗ Trợ Trình Diễn (Hotkeys)

| Phím tắt / Lệnh | Vị trí áp dụng | Chức năng hỗ trợ thuyết trình |
| :--- | :--- | :--- |
| **`F2`** | Màn hình khóa Client | Mở nhanh menu **Cấu hình Demo** (Đổi mã máy trạm, đổi IP máy chủ, bật/tắt Windowed mode mà không cần sửa code). |
| **`--windowed` / `-w`** | Dòng lệnh khởi động | Mở Client dạng cửa sổ thông thường (cho phép co giãn và chia màn hình dễ dàng). |
| **`Esc`** | Màn hình khóa Client | Đóng/mở nhanh hộp đăng nhập hoặc thoát nhanh ứng dụng máy trạm. |
| **`Ctrl + Shift + F12`** | Toàn hệ thống Client | **Thoát khẩn cấp (Emergency Bypass)**: Nhập mật khẩu quản trị (`Admin@123`) để tạm gỡ chế độ khóa Kiosk trong trường hợp cần thao tác hệ điều hành. |

---

### 5. 💡 Câu Hỏi Phản Biện Thường Gặp Của Hội Đồng & Cách Trả Lời Thuyết Phục

> [!TIP]
> Nắm vững các câu trả lời dưới đây sẽ giúp bạn bảo vệ đồ án đạt điểm tối đa trước các câu hỏi kỹ thuật hóc búa của Giảng viên:

* **Câu 1: "Tại sao phần mềm trên máy trạm biết nó đang là máy nào (PC01 hay PC02)?"**
  * *Trả lời*: Trong môi trường phòng net thực tế, các máy trạm sử dụng hệ thống BootROM. Tên máy tính Windows (`ComputerName`) được cấu hình cố định theo vị trí bàn ngồi (ví dụ: PC-01, PC-02...). Khi phần mềm khởi động, Client tự động đọc `Environment.MachineName` để gửi định danh lên Server. Trong buổi thuyết trình, nhóm xây dựng thêm tham số `--machine PCxx` và phím tắt `F2` để giả lập linh hoạt nhiều máy trạm khác nhau trên cùng một laptop.
* **Câu 2: "Tiền giờ được tính toán ở đâu? Khách hàng có thể can thiệp sửa thời gian hoặc số tiền trên máy trạm không?"**
  * *Trả lời*: Toàn bộ logic nghiệp vụ tính cước, trừ tiền và đóng phiên được thực hiện hoàn toàn ở **phía máy chủ (Backend API & SQL Server)** theo nguyên tắc *Server-side Authority*. Client chỉ gửi định kỳ tín hiệu nhịp tim (Heartbeat) và nhận dữ liệu cước hiển thị về qua SignalR. Dù khách hàng có tắt Client hay đổi giờ hệ thống trên máy trạm thì khi mở lại hoặc kết thúc phiên, máy chủ vẫn đối chiếu mốc `Start_Time` trên CSDL để trừ tiền chính xác.
* **Câu 3: "Khi khách gọi món F&B, hệ thống quản lý nguyên vật liệu như thế nào?"**
  * *Trả lời*: Hệ thống tích hợp phân hệ **Hoạch định Nhu cầu Nguyên vật liệu (MRP & BOM)**. Mỗi món ăn/thức uống bán ra (ví dụ: Cà phê sữa, Mì trứng) đều được gắn một bảng định mức công thức (`Bill_Of_Materials`). Khi đơn hàng hoàn tất hoặc khi quản lý duyệt lệnh sản xuất/xuất kho, hệ thống tự động kiểm tra tồn kho an toàn và khấu trừ chính xác từng gram cà phê, mililit sữa đặc hoặc gói mì trong kho nguyên vật liệu.
* **Câu 4: "Tại sao khi chạy demo lại dùng chế độ cửa sổ (`--windowed`) thay vì khóa Kiosk toàn màn hình?"**
  * *Trả lời*: Ứng dụng Client được tích hợp cơ chế bảo mật Kiosk toàn màn hình (chặn WinKey, Alt+Tab, Task Manager). Tuy nhiên, vì điều kiện phòng bảo vệ chỉ có 1 màn hình laptop, nhóm đã thiết kế cờ `--windowed` để có thể xếp song song cả Web Admin thu ngân và 2 máy trạm cùng lúc, giúp Hội đồng nhìn thấy rõ ràng tương tác 2 chiều thời gian thực khi khách đăng nhập hay nạp tiền. Khi triển khai thực tế tại quán, phần mềm sẽ tự động bật 100% Kiosk toàn màn hình.
