# Realtime contract đề xuất

Tình trạng hiện tại: mock không chạy SignalR hub, không có WebSocket/SSE và không phát sự kiện thật. `mock/server.ts` phục vụ HTTP cho web và WPF dùng chung một engine trong bộ nhớ. MSW trong trình duyệt dùng engine riêng; muốn demo liên ứng dụng phải cho web dùng HTTP mode. Polling/resync HTTP là cơ chế đồng bộ hiện có. Các tên sự kiện dưới đây là đề xuất để nhóm backend triển khai và duyệt, không phải endpoint đã hoạt động.

## Kết nối và phân quyền dự kiến

Hub dự kiến `/hubs/internetcafe`, nằm ngoài `/api/v1`. Client lấy access token qua cùng dịch vụ đăng nhập, truyền bằng access-token factory của SignalR; không ghi token vào log. Backend xác minh token, trạng thái tài khoản và quyền trước khi gán group. Client không được tự chọn customerId/employeeId hoặc tham gia group của người khác.

| Group do server gán | Vai trò/thành viên | Phạm vi |
| --- | --- | --- |
| `operations` | Owner, Manager, Cashier | Máy, phiên, đơn và yêu cầu nạp |
| `management` | Owner, Manager | CRM, kho, nhân sự và báo cáo |
| `system` | Admin | Tài khoản và cấu hình được cấp quyền |
| `customer:{id}` | Customer có ID tương ứng | Số dư, phiên, đơn, feedback, khảo sát của chính khách |
| `employee:{id}` | Staff có employeeId tương ứng | Lịch, nghỉ phép, chấm công, bảng lương của chính nhân viên |

Admin không tự động nhận dữ liệu doanh thu/nhân sự. Customer không nhận thông tin khách khác trên máy đang dùng. Staff không nhận lương hay đơn nghỉ của đồng nghiệp. Có thể tách thêm group catalog công khai cho người đăng nhập, nhưng vẫn lọc dữ liệu theo `/workspace` và từng endpoint. Danh sách group là chi tiết server; FE không dùng group để tự suy ra quyền.

## Envelope sự kiện dự kiến

```json
{
  "eventId": "evt-uuid",
  "type": "OrderChanged",
  "occurredAt": "2026-10-03T05:00:00.000Z",
  "entityId": "order-123",
  "version": 3,
  "correlationId": "request-uuid",
  "data": { "id": "order-123", "status": "Preparing" }
}
```

`eventId` duy nhất dùng khử trùng; `version` là số phiên bản tăng theo entity, do backend cấp. `correlationId` nối sự kiện với request/log đã commit. `data` chỉ chứa dữ liệu an toàn cho group; client nên invalidate query và lấy DTO chuẩn qua HTTP. Không gửi mật khẩu, token, danh sách lương, danh sách người nhận khảo sát hoặc toàn bộ transaction ledger vào kênh chung. Các trường event/version/correlation chưa có trong DTO mock đã khóa và không được coi là đã triển khai.

## Sự kiện và hành vi client

| Tên đề xuất | Khi phát sau commit | Người nhận | Query cần đồng bộ |
| --- | --- | --- | --- |
| `ComputerChanged` | Đổi trạng thái máy hoặc bắt đầu/kết thúc phiên | operations; bản tối thiểu cho Customer | computers |
| `SessionStarted` / `SessionEnded` | Tạo/hoàn tất phiên | operations và customer tương ứng | sessions, computers, me/customers, transactions, reports |
| `BalanceChanged` | Nạp được duyệt, mua món, hoàn tiền, chốt giờ | operations và customer tương ứng | me/customers, transactions |
| `OrderCreated` / `OrderChanged` | Tạo hoặc chuyển trạng thái đơn | operations và customer sở hữu | orders, products; khi hủy thêm me/transactions/reports |
| `TopupRequested` / `TopupDecided` | Tạo/duyệt/từ chối nạp | operations và customer sở hữu | topups; khi duyệt thêm me/transactions |
| `CatalogChanged` | Đổi sản phẩm/danh mục/tồn | management; catalog đã lọc cho người được đọc | products, categories, inventory |
| `FeedbackChanged` | Góp ý mới hoặc phản hồi quản lý | management và customer sở hữu | feedback |
| `SurveyPublished` / `SurveyClosed` | Phát/đóng khảo sát | management và từng customer được chọn | surveys |
| `SurveyAnswered` | Ghi nhận câu trả lời | management và customer vừa trả lời | surveys; không broadcast câu trả lời |
| `ScheduleChanged` | Phân ca/đổi lịch, duyệt nghỉ làm OnLeave | management và employee sở hữu | schedules, leaves |
| `LeaveChanged` | Tạo/sửa/duyệt/từ chối đơn nghỉ | management và employee sở hữu | leaves, schedules |
| `AttendanceChanged` | Chấm công vào/ra | management và employee sở hữu | attendance, schedules |
| `PayrollChanged` | Sửa/duyệt/trả bảng lương | management và employee sở hữu | payroll |
| `AccountAccessChanged` | Ban, hạ quyền, ngừng nhân viên | system và kết nối của tài khoản chịu tác động | identity; ngắt phiên nếu không còn quyền |

Máy khách không tự trừ tiền hoặc tự cộng kho khi nhận event. Endpoint nghiệp vụ vẫn là nơi thực hiện thay đổi; event chỉ thông báo dữ liệu đã thay đổi. Không phát sự kiện trước khi transaction thành công.

## Mất kết nối và tính nhất quán

1. Sau đăng nhập, GET `/workspace` hoặc các collection đã phân trang; đồng bộ đồng hồ theo HTTP `Date` khi cần hiển thị thời gian phiên.
2. Khi hub sẵn sàng, nhận event, bỏ eventId đã xử lý và bỏ version cũ. Debounce/invalidate theo entity để tránh nhiều request trùng nhau.
3. Khi reconnect hoặc nhận version nhảy cóc, tải lại dữ liệu HTTP có quyền xem. Không suy rằng replay là đầy đủ: mock hiện không có event log; backend phải chốt thời gian giữ event và cơ chế cursor nếu hỗ trợ replay.
4. Dùng backoff có jitter, tạm dừng khi logout, hủy subscription cũ khi đổi tài khoản. Khi token/role đổi, đăng nhập lại hoặc refresh theo contract backend tương lai; mock không có refresh token.
5. Nếu hub chưa có, dùng polling có thời gian hợp lý và refresh sau mutation. Gắn trạng thái kết nối/mốc cập nhật để người dùng biết dữ liệu có thể cũ. HTTP là nguồn dữ liệu chuẩn trong demo.

Backend cần outbox hoặc cơ chế tương đương để commit nghiệp vụ và phát event bền vững; kiểm thử gửi trùng, sai thứ tự, reconnect, quyền thay đổi và tách dữ liệu giữa khách/nhân viên. Chưa có hạng mục nào trong đoạn này được mock coi là bảo đảm production.
