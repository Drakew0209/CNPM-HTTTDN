# InternetCafe local application

Thư mục này chứa web quản trị React, ứng dụng WPF cho khách hàng và backend ASP.NET Core kết nối SQL Server LocalDB. Backend dùng database `InternetCafeDB` tại `(localdb)\MSSQLLocalDB`; dữ liệu được lưu bền qua lần khởi động lại.

## Yêu cầu

- Node.js 20 trở lên và npm
- .NET SDK 10 (để chạy backend và desktop WPF trên Windows)
- SQL Server LocalDB, với schema gốc đã được tạo trong database `InternetCafeDB`

## Chạy web

Mở PowerShell tại `InternetCafe\apps\admin-web`.

```powershell
npm install
npm run dev
```

Mở http://127.0.0.1:5173. Chế độ này dùng MSW trong trình duyệt, phù hợp để xem và thao tác riêng trong một tab.

## Chạy ứng dụng với SQL Server thật

Mở PowerShell tại `InternetCafe\apps\admin-web` và chạy:

```powershell
npm run demo:real
```

Web chạy tại http://127.0.0.1:5173; backend SQL Server chạy tại http://127.0.0.1:5055/api/v1.

Để mở WPF khi backend SQL Server đang chạy, mở PowerShell thứ hai tại `InternetCafe\apps\desktop-client`:

```powershell
dotnet run --project InternetCafe.Client/InternetCafe.Client.csproj
```

## Tài khoản demo

| Tên đăng nhập | Vai trò |
| --- | --- |
| `manager` | Quản lý |
| `cashier` | Thu ngân |
| `staff` | Nhân viên |
| `admin` | Quản trị hệ thống |
| `owner` | Chủ quán |
| `gamer` | Khách hàng (dùng desktop) |

Mật khẩu cho toàn bộ tài khoản: `Demo@123`.

## Kiểm tra nhanh

Trong `InternetCafe\apps\admin-web`:

```powershell
npm test
npm run build
```

Trong `InternetCafe\apps\desktop-client`:

```powershell
dotnet build InternetCafe.Client/InternetCafe.Client.csproj
```

## Kiểm tra backend

```powershell
Invoke-RestMethod http://127.0.0.1:5055/health
```

Kết quả `source: sql-server` xác nhận API đang dùng SQL Server thay vì mock. Backend seed an toàn chỉ bổ sung dữ liệu cho bảng trống; không xóa dữ liệu sẵn có khi khởi động.

Phiên chơi đang hoạt động được kiểm tra mỗi 5 giây. Nếu máy không gửi heartbeat liên tục trong 5 phút, backend tự chốt phiên tại thời điểm heartbeat cuối và trả máy về trạng thái sẵn sàng. Giá trị này cấu hình tại `Backend:OfflineSessionGraceSeconds` trong `apps/backend/InternetCafe.Backend/appsettings.json`.

## Chạy mock khi chỉ cần phát triển giao diện

`npm run demo` vẫn chạy mock trong bộ nhớ. Dùng `demo:real` khi cần web, WPF và dữ liệu SQL Server dùng chung một backend.

## Vai trò và chức năng

| Vai trò | Chức năng trong ứng dụng |
| --- | --- |
| `Admin` | Quản lý tài khoản và phân quyền; tra cứu sản phẩm, nhà cung cấp. Không xử lý vận hành, đơn hàng hoặc tiền. |
| `Owner` | Quản lý toàn bộ vận hành quán, phòng máy, F&B, kho, CRM, nhân sự và báo cáo. |
| `Manager` | Quản lý phòng máy, F&B, kho, CRM, nhân sự và báo cáo. |
| `Cashier` | Theo dõi phòng máy; duyệt yêu cầu nạp tiền; xử lý đơn dịch vụ; xem khách hàng, thực đơn và báo cáo. |
| `Staff` | Chỉ dùng không gian cá nhân: hồ sơ, lịch làm, chấm công, nghỉ phép và lương của chính mình. |
| `Customer` | Dùng ứng dụng desktop để xem hồ sơ, ví/số dư, phiên chơi; nạp tiền, đặt món, gửi phản hồi và trả lời khảo sát. |


## Tóm tắt

### Mở web quản lý

```powershell
cd InternetCafe\apps\admin-web
npm install            # Chỉ cần chạy lần đầu
npm run demo           # Mock: dữ liệu trong bộ nhớ

# Hoặc chạy web với SQL Server thật
npm run demo:real
```

Chỉ chạy một trong hai lệnh `demo` hoặc `demo:real` tại một thời điểm. Dùng `Ctrl+C` để dừng lệnh đang chạy trước khi đổi chế độ.

### Mở app User

```powershell
cd InternetCafe\apps\desktop-client
dotnet run --project InternetCafe.Client/InternetCafe.Client.csproj
```

App User dùng chung backend SQL Server khi `npm run demo:real` đang chạy.
