# Rà soát quy tắc nghiệp vụ

Nguồn rà soát độc lập: [kết quả OMX ngày 03/10/2026](../../.omx/delegations/20261003-124925-omx-exec.md), đối chiếu `implementation-contract.md` và SQL gốc. Tài liệu này phân biệt các rủi ro đã được xử lý trong contract/mock với phần backend vẫn phải triển khai. SQL gốc chưa phải backend production; không có migration SQL trong đợt làm frontend này.

## Tám phát hiện và kết quả xử lý

| Phát hiện OMX | Đã xử lý trong contract/mock/FE | Khoảng trống backend |
| --- | --- | --- |
| ID contract là chuỗi, SQL dùng khóa số | DTO giữ string ID; `gamer → c-001`, `staff → e-001`; API/HTTP tests kiểm tra ánh xạ | Chốt mapping DTO string với PK số/code SQL; không để client phụ thuộc khóa nội bộ |
| Vai trò SQL không đủ sáu vai trò contract | Engine có Admin/Owner/Manager/Cashier/Staff/Customer; token không chứa hash; ban được kiểm tra; workspace và detail lọc theo quyền/ownership | Auth thật, mapping vị trí–role, token lifecycle và chính sách theo cửa hàng; không suy Admin được toàn quyền nghiệp vụ |
| Customers SQL thiếu email | Customer DTO/register/profile giữ email; kiểm thử đăng ký và `/me` xác nhận round-trip; snapshot không có password | Thêm/migrate/map email và validation/uniqueness theo quyết định nghiệp vụ |
| Từ vựng status SQL lệch contract | Enum FE/OpenAPI thống nhất; action chuyển trạng thái và PATCH whitelist chặn sửa tùy ý; payroll Paid và leave đã xử lý bất biến | Mapping SQL Reviewed/Unpaid/Resigned/Suspended/Unpaid-leave/Discontinued vào contract hoặc migration được duyệt |
| Topups/suppliers chưa là bảng độc lập | Workspace luôn có collection; workflow nạp Pending/Approved/Rejected; inventory có supplierId tùy chọn; test HTTP hai client chứng minh cùng state | Persistence topup, supplier, phương thức thanh toán, đối soát và relation inventory |
| Quy tắc tính tiền có thể lệch DECIMAL/trigger SQL | Mock dùng số nguyên VND; giá/total/balance do server tính; reports chỉ Rental + FoodOrder − Refund, TopUp riêng; domain tests chống đếm nạp thành doanh thu | Database constraints, làm tròn, tiền tệ/thuế, chuẩn serialize; đối chiếu công thức trigger với API trước tích hợp |
| Nguy cơ ghi tiền/kho hai lần | Order kiểm tra toàn bộ trước ghi, reserve/debit một lần; idempotency order/topup; refund/approve/transition bảo vệ lặp; test so snapshot lỗi không có ghi dở | Transaction DB, locks/unique constraints, durable idempotency, phối hợp trigger–service không áp hai lần, audit người/lý do |
| Invariant session/máy thiếu phía client | Start yêu cầu máy online/Available và số dư; một active session; frozen rate, chốt giờ có clamp; end lặp trả kết quả cũ và giải phóng máy; tests có billing/ownership | Online heartbeat, device ACK, điều khiển máy thật, timer hết tiền, crash recovery và unique active session trên DB |

## Ranh giới báo cáo

- Doanh thu lấy từ ledger đã ghi nhận, không cộng thêm tổng đơn hoặc session. TopUp là nguồn nạp ví, không phải doanh thu bán hàng. Hoàn tiền tính vào kỳ phát sinh nên kỳ chỉ có hoàn tiền có thể âm.
- Món bán tổng hợp các đơn chưa Cancelled được tạo trong kỳ. Đây là phân tích giỏ món theo thời điểm đặt, khác báo cáo ledger theo thời điểm ghi sổ. Không dùng giá hiện tại của product để tính lại giá món cũ.
- Chọn ngày sẽ tạo nhóm khách có giao dịch trong kỳ cho thống kê tuổi/sở thích/hạng. Không có ngày đăng ký trong Customer DTO nên không trình bày đây là thống kê khách mới. Hạng/sở thích là hồ sơ hiện tại, tuổi tính theo mốc báo cáo.
- Nhập/xuất kho dùng movement trong kỳ; lượng đặt món chưa hủy trình bày riêng. Stock là ảnh chụp hiện tại, không suy ngược lịch sử từ seed thiếu số dư đầu. Không dùng giá bán làm giá vốn kho.
- Nhân sự dùng joinedAt để tính thâm niên; phòng ban/vị trí/trình độ là hồ sơ hiện tại. Employee DTO chưa có dateOfBirth, vì vậy báo cáo tuổi nhân viên phải báo thiếu dữ liệu. Không tự bịa tuổi.
- Payroll lọc theo tháng giao với khoảng ngày. Tổng bảng lương gồm Draft/Approved/Paid; “Đã trả” chỉ Paid. Tổng năm chỉ cộng các tháng trong bộ lọc, không nhân lương tháng hiện tại với 12. Chưa có mô hình chấm công→lương, thuế hoặc thanh toán thật.
- Bộ lọc FE hiện dùng ngày theo timezone trình duyệt qua `filterDates`; engine dùng Asia/Ho_Chi_Minh. Demo kỳ vọng máy sử dụng múi giờ Việt Nam. Backend tích hợp cần chốt timezone để báo cáo không lệch ngày trên máy ở nước ngoài.

## Bằng chứng và phạm vi xác minh

`mock/engine.test.ts` và `mock/server.test.ts` có 19 kiểm thử: auth/RBAC, customer DTO, atomic money/stock, price guard, idempotency/refund/topup, session billing, inventory/payroll, staff leave/attendance, survey privacy, report vượt 100 giao dịch và chia sẻ HTTP state. `src/domain.test.ts` bổ sung kiểm thử công thức doanh thu, nạp ví tách biệt, kỳ hoàn tiền âm, biên ngày local và CSV chống formula injection ở các tiền tố nguy hiểm, quoting/Unicode/thứ tự cột.

Các kiểm thử hiện chứng minh engine một process và các phép biến đổi FE. Chúng không chứng minh transaction nhiều node, xác thực production, payment provider, phần cứng hoặc SignalR. Chi tiết backend còn thiếu tại [backend-gaps.md](../contracts/backend-gaps.md); realtime là đề xuất tại [realtime-events.md](../contracts/realtime-events.md). Không có thao tác tự sửa số dư/kho từ trang báo cáo.
