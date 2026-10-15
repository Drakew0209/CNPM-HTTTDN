# Ranh giới triển khai frontend

> [!NOTE]
> **Ghi chú chuyển tiếp (15/10/2026)**: Tài liệu này mô tả ranh giới ban đầu của giai đoạn phát triển giao diện. Hiện tại hệ thống đã được tích hợp hoàn chỉnh (Monorepo) gồm cả Backend ASP.NET Core 10 thật kết nối SQL Server LocalDB, Client WPF Kiosk và Web Admin.

Mã nguồn nằm trong thư mục `InternetCafe`. Hệ thống hỗ trợ cả chế độ mock độc lập (`npm run dev`) và chế độ kết nối Backend SQL Server production (`npm run demo:real`).

Web: React + TypeScript + Vite, Ant Design, TanStack Query, React Router, Recharts, MSW. Desktop: WPF .NET 10, MVVM, Material Design, HttpClient và SignalR qua adapter.

API HTTP dùng `/api/v1`, dữ liệu `{ data: ... }`, lỗi `{ error: { code, message, fields? } }`. Tiền VND số nguyên, thời gian UTC ISO 8601, ngày sinh/ngày công dạng YYYY-MM-DD. Trạng thái máy nghiệp vụ tách khỏi kết nối online/offline.

Server quyết định số dư, đơn giá, tiền phiên, tồn kho, trạng thái đơn, quyền và kết quả thao tác. UI chỉ hiển thị/ước tính. Nạp tiền tách khỏi doanh thu tiêu dùng. Không có cơ chế khóa Windows/native agent trong phạm vi FE.

Mock trình duyệt và mock desktop có thể độc lập. Chỉ công bố đồng bộ liên ứng dụng nếu đã kiểm thử thật với cùng transport. Những endpoint mở rộng so với SQL là đề xuất contract cần nhóm BE xác nhận.
