# Bàn giao backend và giới hạn mock

`implementation-contract.md` khóa DTO dùng chung. `openapi.yaml` đặc tả OpenAPI 3.0.3 phản ánh endpoint đang có trong `mock/engine.ts`; `realtime-events.md` là đề xuất SignalR chưa triển khai. Mock này phục vụ phát triển/đánh giá FE, không thay thế backend production hay sổ kế toán.

## Chạy và kết nối

Từ `InternetCafe/apps/admin-web`, chạy `npm run mock:server`. Host chỉ lắng nghe `127.0.0.1:5055`, base `/api/v1`. `GET /health` ở root trả `{data:{status:"ok",source:"mock",persistent:false}}`. Web muốn chia sẻ dữ liệu với WPF đặt `VITE_API_MODE=http` và `VITE_API_BASE_URL=http://127.0.0.1:5055/api/v1`; web MSW dùng bộ nhớ riêng. Khởi động lại host làm mất toàn bộ thay đổi, token và idempotency cache.

Host trả HTTP `Date`, JSON UTF-8, `Cache-Control: no-store`. CORS cho `localhost`/`127.0.0.1` cổng 5173 và 4173, expose `Date`, cho `Authorization` và `Content-Type`. Body JSON tối đa 1 MiB. WPF không cần Origin; các Origin khác bị từ chối. Đây là giới hạn demo loopback, không phải thiết kế triển khai mạng công cộng.

## Quyền đã thực thi

| Vai trò | Đọc | Ghi/hành động |
| --- | --- | --- |
| Admin | accounts, products, categories, suppliers | CRUD tài khoản; không có quyền quản lý toàn cửa hàng |
| Owner/Manager | Mọi collection trừ accounts | Quản lý danh mục, khách, máy, kho, CRM, HR/payroll; duyệt nạp/đơn/nghỉ và kết thúc phiên |
| Cashier | computers, customers, products, categories, orders, topups, transactions, sessions | Tạo/sửa hồ sơ khách; PATCH không đổi tier/status; duyệt nạp, xử lý đơn, lệnh máy, kết thúc phiên; xem báo cáo |
| Staff | employees/schedules/leaves/attendance/payroll của mình; tất cả shifts | Hồ sơ cá nhân; đơn nghỉ Pending của mình; check-in/check-out của mình |
| Customer | Hồ sơ và sessions/orders/topups/transactions/feedback của mình; khảo sát được phát; sản phẩm active; categories/computers đã lọc | Đăng ký, hồ sơ/mật khẩu, phiên của mình, yêu cầu nạp, mua món, hủy đơn Pending của mình, feedback và trả lời khảo sát |

Auth `permissions` gồm `<resource>:read` và các nhóm `management:write`, `operations:write`, `reports:read`, `accounts:write` theo vai trò. Đây là gợi ý cho điều hướng FE; engine vẫn kiểm tra role/ownership/whitelist/state transition cho từng request. Trường `Staff` là vai trò cổng nhân viên, không được hiểu là operator được điều khiển phòng máy. Customer `gamer` có `id=c-001`; Staff `staff` có `id=e-001`. `/me` trả Customer/Employee DTO theo hai vai trò này; các vai trò còn lại trả AuthUser. `/auth/register` trả Customer DTO, sau đó phải login.

## Bất biến nghiệp vụ và khoảng trống cần xử lý

| Phần | Đã có trong mock | Backend production cần bổ sung/chốt |
| --- | --- | --- |
| Auth | Demo mật khẩu `Demo@123`, token opaque bộ nhớ, ban kiểm tra lại mỗi request, logout thu hồi các token username | Hash mật khẩu, TLS, secret/key management, expiry/refresh/revocation, rate limit, audit, reset password và CSRF/cookie policy nếu đổi cơ chế |
| RBAC | Lọc collection/detail/workspace phía server; own employee/customer; whitelist PATCH | Authorization policies và row-level checks trong mọi query/transaction; quyết định quyền theo cửa hàng/chi nhánh nếu có |
| Đặt món | Kiểm tra toàn bộ dòng trước ghi; giá server, reserve stock, debit, ledger một lần; expectedUnitPrice/expectedTotal guards | Database transaction, row lock/version, isolation cho nhiều process; nguồn giá/thuế/khuyến mãi và thời điểm chốt giá |
| Hủy/hoàn | Pending hủy; management có thể hủy Preparing; hoàn tiền/kho một lần | Audit người/lý do hủy, idempotent durable refund, quy tắc trả món đã chế biến và đổi/hoàn một phần |
| Nạp tiền | Pending -> Approved/Rejected; Approved cộng balance và TopUp ledger đúng một lần | Đối soát tiền mặt/provider webhook, chữ ký webhook, settlement, tranh chấp, bằng chứng thu tiền; không tin trạng thái FE |
| Idempotency | Body key tùy chọn cho POST orders/topups; scope user+endpoint, canonical payload; payload khác cùng key -> 409 | Kho lưu bền vững, TTL, unique constraints, kết quả replay sau restart, xử lý in-flight đồng thời; chốt header/body convention |
| Phiên máy | Một active session/khách; máy online/Available; khóa giá lúc bắt đầu; kết thúc trả bill và máy Available | Unique constraints active machine/customer, durable frozen price, crash recovery, timer/background stop khi hết tiền; đồng bộ thiết bị |
| Tính giờ | `min(balance,ceil(elapsedMs*hourlyRate/3600000))`, UTC server time; lặp end trả session cũ | Chốt đơn vị làm tròn, grace period, pause, combo/khuyến mãi, xử lý đồng hồ và số dư thay đổi khi mua món; active session seed fallback rate hiện tại |
| Lệnh máy | Chỉ sửa demo state, không gọi OS/network | Agent thiết bị xác thực, command IDs/ACK/timeouts, offline state, quyền điều khiển và audit; không đồng nhất Available với bật máy thành công |
| Kho | Import/Export số dương, chống xuất quá tồn, stock không PATCH trực tiếp | Sổ kho mở đầu/kiểm kê, đơn vị/lô/hết hạn, purchase orders, NCC công nợ và transaction nhiều kho |
| Nhân sự | Hồ sơ, ca qua đêm, phân ca, leave approval -> OnLeave; check-in theo ngày VN/ownership | Chống ca chồng lấn, timezone/chi nhánh, cửa sổ check-in theo giờ, late/overtime, sửa công có audit; mock chưa kiểm tra giờ bắt đầu/kết thúc ca |
| Payroll | Server tính `max(0,baseSalary+bonus-deduction)`; unique employee/month; Draft -> Approved -> Paid; Paid bất biến | Lương theo công, thuế/BHXH, quy trình duyệt nhiều người, chứng từ thanh toán, khóa kỳ, lịch sử sửa và chính sách truy cập |
| CRM | Khách/tier, feedback status/response, survey draft/publish/close, một câu trả lời mỗi khách | Chính sách consent/retention/export, thông báo, gửi survey thực, scoring/segment/tier tự động; bảo vệ PII |
| Báo cáo | Cộng toàn bộ ledger trong khoảng ngày VN; topups tách sales; `source=mock` | Phân quyền báo cáo, kế toán/thuế, trạng thái settlement, hoàn tiền khác kỳ, export lớn; mock netRevenue có thể âm theo kỳ |
| Realtime | HTTP polling/resync dùng chung host | SignalR hub, group authorization, reconnect/version/cursor, durable outbox; xem realtime-events.md |

Trong một engine, `handle()` đồng bộ nên request nghiệp vụ không xen giữa các bước money/stock. Điều này không chứng minh khả năng giao dịch database hoặc hệ thống nhiều node. Seed là ảnh dữ liệu demo, không phải ledger đầy đủ có thể tái tính mọi số dư/tồn từ đầu.

## CRUD và quy tắc xóa

Collection/detail GET áp dụng phân quyền trước trả dữ liệu. Tất cả list hỗ trợ `search`, `status`, `sort`, `order`, `page` (mặc định 1), `pageSize` (mặc định 20, tối đa 100), `from`, `to`. Ngày lọc inclusive theo Asia/Ho_Chi_Minh; rows không có trường ngày sẽ không xuất hiện nếu lọc ngày. `/workspace` không phân trang. `/reports/summary` chỉ công bố `from/to`, tính cả tập dữ liệu thay vì chỉ trang đầu.

Tạo/sửa/xóa trực tiếp chỉ dùng cho các module ghi rõ trong OpenAPI. `orders`, `topups`, `transactions`, `sessions`, `attendance` dùng action, không CRUD tùy ý. `inventory` chỉ append; feedback không xóa; payroll không xóa. Xóa customers/accounts/employees/products/suppliers là deactivation. Các danh mục/ca có tham chiếu, máy có lịch sử phiên và lịch có chấm công được bảo vệ. Chỉ xóa survey Draft; leave đã duyệt/từ chối bất biến. Trường không nằm trong whitelist trả 422, bao gồm balance/total/stock trong PATCH không được phép.

## Mã lỗi và ví dụ

```json
{"error":{"code":"VALIDATION_ERROR","message":"Dữ liệu không hợp lệ.","fields":{"amount":"amount phải là số nguyên từ 1000 đến 10000000."}}}
```

```json
{"error":{"code":"PRICE_CHANGED","message":"Tổng tiền đã thay đổi. Vui lòng xác nhận giá mới."}}
```

| HTTP | Mã có trong engine/host | Hành vi FE |
| --- | --- | --- |
| 400 | INVALID_JSON | Kiểm tra body JSON |
| 401 | INVALID_CREDENTIALS, UNAUTHENTICATED | Hiển thị lỗi login hoặc yêu cầu đăng nhập lại |
| 403 | FORBIDDEN, ACCOUNT_BANNED; host ORIGIN_NOT_ALLOWED | Thông báo không có quyền/bị khóa; không tự retry |
| 404 | NOT_FOUND | Bản ghi không còn hoặc không thuộc quyền; refresh danh sách |
| 405 | METHOD_NOT_ALLOWED | Dùng đúng collection/action được đặc tả |
| 409 | USERNAME_EXISTS, ACTIVE_SESSION, COMPUTER_UNAVAILABLE, NO_ACTIVE_SESSION, INSUFFICIENT_BALANCE, OUT_OF_STOCK, PRICE_CHANGED | Refresh dữ liệu hiện tại và để người dùng điều chỉnh/xác nhận |
| 409 | ALREADY_PROCESSED, DUPLICATE_TRANSACTION, IDEMPOTENCY_CONFLICT, INVALID_TRANSITION | Không gửi lặp mù; kiểm tra kết quả hành động và key/payload |
| 409 | HAS_HISTORY, IN_USE, SELF_LOCK, LEAVE_FINAL, PAYROLL_FINAL, PAYROLL_EXISTS, SCHEDULE_EXISTS, EMPLOYEE_ON_LEAVE | Hiển thị xung đột ràng buộc nghiệp vụ |
| 409 | SURVEY_PUBLISHED, SURVEY_CLOSED, DUPLICATE_RESPONSE, DUPLICATE_CHECK_IN, OUTSIDE_SCHEDULE | Refresh trạng thái survey/lịch và ngăn hành động không còn phù hợp |
| 413 | PAYLOAD_TOO_LARGE | Body vượt 1 MiB ở host |
| 415 | UNSUPPORTED_MEDIA_TYPE | Gửi Content-Type application/json |
| 422 | VALIDATION_ERROR, INVALID_PASSWORD | Hiển thị lỗi fields khi có; giữ dữ liệu biểu mẫu |
| 500 | MOCK_INTERNAL_ERROR | Báo lỗi và cho thử lại; không coi nghiệp vụ đã thành công |

## Bằng chứng kiểm thử và điều kiện tích hợp

`mock/engine.test.ts` kiểm tra auth/register/profile/password, lọc RBAC, nguyên tử order fail, reserve/refund stock, idempotency, price guards, insufficient balance, chuyển trạng thái, top-up đúng một lần, rate freeze/billing clamp, inventory/payroll, staff attendance/leave, privacy survey, pagination và report vượt 100 giao dịch. `mock/server.test.ts` gọi HTTP thật trên cổng loopback ngẫu nhiên: hai client thấy cùng yêu cầu nạp, cashier duyệt làm customer thấy số dư mới; Date/CORS và lỗi JSON/media/auth được kiểm tra.

Chạy từ `apps/admin-web`: `npm test`. Tạo lại OpenAPI: `npx tsx ../../contracts/generate-openapi.ts`. Chạy kiểm thử không chứng minh production auth/payment, điều khiển phần cứng, database transaction hoặc SignalR vì các thành phần đó chưa có. Nhóm backend cần implement API cùng DTO/envelope, map đầy đủ mã lỗi/quyền và chạy các scenario tương đương trên persistence thật trước khi FE chuyển base URL.
