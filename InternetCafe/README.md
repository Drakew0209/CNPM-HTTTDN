# InternetCafe frontend demo

Thư mục này chứa frontend đã triển khai theo kế hoạch bàn giao: web quản trị React và ứng dụng WPF cho khách hàng. Các API hiện tại là mock cục bộ, chạy trong bộ nhớ; khởi động lại mock server sẽ xóa dữ liệu mô phỏng.

## Yêu cầu

- Node.js 20 trở lên và npm
- .NET SDK 10 (để chạy desktop WPF trên Windows)

## Chạy web

Mở PowerShell tại `InternetCafe\apps\admin-web`.

```powershell
npm install
npm run dev
```

Mở http://127.0.0.1:5173. Chế độ này dùng MSW trong trình duyệt, phù hợp để xem và thao tác riêng trong một tab.

## Chạy demo dùng chung web và desktop

Mở PowerShell tại `InternetCafe\apps\admin-web` và chạy:

```powershell
npm run demo
```

Web chạy tại http://127.0.0.1:5173; mock API dùng chung chạy tại http://127.0.0.1:5055/api/v1.

Để mở WPF khi mock API đang chạy, mở PowerShell thứ hai tại `InternetCafe\apps\desktop-client`:

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

Frontend này là bản demo cục bộ. Quy tắc tiền, tồn kho, phân quyền và phí phiên ở môi trường thật vẫn phải do backend quyết định.
