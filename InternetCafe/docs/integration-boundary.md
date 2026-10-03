# Ranh giới triển khai frontend

Mã nguồn nằm trong `InternetCafe` của checkout nhánh `FE`. Backend sản xuất chưa có; mock chỉ phục vụ demo và kiểm thử frontend. Không chỉnh sửa SQL/tài liệu nhóm ở thư mục cha.

Web: React + TypeScript + Vite, Ant Design, TanStack Query, React Router, Recharts, MSW. Desktop: WPF .NET 10, MVVM, Material Design, HttpClient và SignalR qua adapter.

API HTTP dùng `/api/v1`, dữ liệu `{ data: ... }`, lỗi `{ error: { code, message, fields? } }`. Tiền VND số nguyên, thời gian UTC ISO 8601, ngày sinh/ngày công dạng YYYY-MM-DD. Trạng thái máy nghiệp vụ tách khỏi kết nối online/offline.

Server quyết định số dư, đơn giá, tiền phiên, tồn kho, trạng thái đơn, quyền và kết quả thao tác. UI chỉ hiển thị/ước tính. Nạp tiền tách khỏi doanh thu tiêu dùng. Không có cơ chế khóa Windows/native agent trong phạm vi FE.

Mock trình duyệt và mock desktop có thể độc lập. Chỉ công bố đồng bộ liên ứng dụng nếu đã kiểm thử thật với cùng transport. Những endpoint mở rộng so với SQL là đề xuất contract cần nhóm BE xác nhận.
