# Kế Hoạch Phát Triển (AI_DEV_PLAN)

Tài liệu này được sử dụng để đồng bộ luồng làm việc giữa **User**, **Planner AI (Gemini)**, và **Coder AI (OpenCode - GPT-6 Luna)** trong quá trình cải thiện dự án Internet Cafe.

## 1. Vai trò của từng bên
- **User (Bạn)**: Đưa ra yêu cầu, ý tưởng tính năng, kiểm duyệt logic và kết quả. Chuyển giao prompt từ Planner cho OpenCode.
- **Planner AI (Tôi - Gemini)**: Phân tích yêu cầu, kiểm tra source code, thiết kế logic, và soạn thảo **Prompt** chuẩn xác để giao việc cho OpenCode. Cập nhật trạng thái tiến độ vào file này.
- **Coder AI (OpenCode)**: Đọc Prompt được giao, tiến hành viết code, sửa lỗi trực tiếp vào file.

## 2. Quy trình làm việc (Workflow)
1. **Tiếp nhận**: User mô tả tính năng cần thêm hoặc logic cần sửa cho Planner.
2. **Khảo sát**: Planner khảo sát code hiện tại, phân tích tính khả thi và rủi ro.
3. **Thiết kế Prompt**: Planner soạn ra một **Task Prompt** chi tiết (gồm mục tiêu, file cần sửa, logic cần tuân thủ) và lưu vào mục "3. Task Đang Xử Lý" hoặc đưa trực tiếp cho User.
4. **Thực thi**: User copy **Task Prompt** đưa cho OpenCode.
5. **Nghiệm thu**: Sau khi OpenCode hoàn thành, User test thử. Nếu có lỗi, User báo lại Planner để Planner tạo prompt sửa lỗi.
6. **Cập nhật File**: Planner đánh dấu Task là "Hoàn thành" trong file này.

## 3. Template chuẩn của một Task Prompt cho OpenCode
*Mỗi khi có task mới, Planner sẽ tạo prompt theo cấu trúc sau cho OpenCode:*

```markdown
[MỤC TIÊU]: (Mô tả ngắn gọn tính năng cần làm)
[THƯ MỤC LÀM VIỆC CẦN CHÚ Ý]: (Ví dụ: InternetCafe/apps/backend...)
[PHÂN TÍCH LOGIC]: 
- Bước 1: ...
- Bước 2: ...
[CÁC FILE CẦN THAY ĐỔI]: 
- File A: Cần thêm hàm X
- File B: Sửa logic Y
[RÀNG BUỘC]: (Không phá vỡ logic cũ, phải tuân thủ chuẩn code, dùng đúng thư viện...)
```

## 4. Bảng Theo Dõi Tiến Độ (Task Tracker)

### ⏳ Đang phân tích / Lên kế hoạch
*(Trống)*

### 🚀 Đang thực thi (Giao cho OpenCode)
*(Trống)*

### 📋 Tài liệu rà soát
- **[14/10/2026] Đối chiếu yêu cầu & ghi chú trạng thái**: [`docs/DoiChieuYeuCau.md`](docs/DoiChieuYeuCau.md)
  - Phần A: 12 ghi chú trạng thái (tài liệu lỗi thời, sai phiên bản, không có bảng Accounts, MRP trống…).
  - Phần B: đối chiếu từng mục của yêu cầu 2 (Admin), 3 (HRM), 4 (CRM), 5 (MRP) và 14 chức năng bắt buộc.
  - Phần C: danh sách khoảng trống xếp theo P0/P1/P2, kèm C.3 liệt kê mã nguồn payroll còn thừa.
  - **Phạm vi đã điều chỉnh**: chức năng quản lý lương (payroll) đã được yêu cầu bỏ → đánh dấu NGOÀI PHẠM VI, không tính là khoảng trống. **Khoảng trống P0 còn lại: toàn bộ hệ thống MRP + loại đơn nghỉ thai sản.**

### ✅ Đã hoàn thành
1. **[Hoàn thành 07/10/2026] Tính năng**: Nạp tiền trực tiếp vào tài khoản Khách hàng từ Admin.
   - Thêm `POST /customers/{id}/topup`.
   - UI hiển thị Modal nạp tiền tại `admin-web`.

---

## 5. Tiêu Chuẩn Nghiệp Vụ Thực Tế Của Quán NET (Business Domain)
*Bất kỳ tính năng nào được code thêm phải đối chiếu với các logic thực tế sau đây để tránh sai sót về mặt nghiệp vụ:*

### 5.1. Mô hình Tài khoản & Tính tiền (Billing System)
- **Tài khoản Hội viên (Member)**: Tạo tài khoản, nạp tiền trước (Prepaid). Tiền trừ dần theo thời gian thực dựa trên **giá của máy đang ngồi**. Khi số dư = 0, máy tự động khóa.
- **Tài khoản Vãng lai (Guest)**: Mua phiếu (ticket) trả tiền trước, hệ thống sinh ra user/pass ngẫu nhiên với số dư tương ứng. Hết tiền tự out, không lưu lịch sử.
- **Đơn giá linh hoạt**: Giá tiền/giờ không cố định cho tài khoản, mà phụ thuộc vào **Khu vực máy** (Zone). VD: Máy khu VIP giá 15k/h, máy thường 8k/h. 
- **Khấu trừ theo Block**: Đôi khi quán trừ tiền theo block (mỗi 15 phút hoặc 30 phút một lần) thay vì từng giây.

### 5.2. Quản lý Máy trạm (Client PC)
- **Trạng thái máy**: 
  - `Available`: Sẵn sàng, đang hiển thị màn hình khóa.
  - `In-Use`: Đang có khách chơi.
  - `Maintenance`: Máy hỏng hoặc đang bảo trì, không cho phép đăng nhập.
- Khách tự đổi máy (Chuyển máy): Khách đang chơi máy A, có thể yêu cầu nhân viên chuyển sang máy B (nếu B rảnh). Tiền/thời gian sẽ tiếp tục trừ theo đơn giá của máy B.

### 5.3. Dịch vụ F&B (Đồ ăn, Thức uống)
- **Gọi đồ tại máy**: Khách dùng App trên Client để order.
- **Thanh toán**: 
  - Trừ thẳng vào **số dư tài khoản** (nếu tiền nạp ban đầu đủ lớn và khách đồng ý).
  - Trả tiền mặt/Chuyển khoản khi nhân viên bưng đồ ra.
- **Tồn kho**: Mọi order hoàn tất phải trừ tồn kho tương ứng. Cần cảnh báo khi sắp hết hàng.

### 5.4. Quản lý Nhân sự & Giao ca (Shift Handover)
- Quán hoạt động 24/7, chia ca (vd: 8h-16h, 16h-24h, 24h-8h).
- **Chốt ca (End of Shift)**: Nhân viên sắp nghỉ phải chốt lại: 
  1. Tổng tiền mặt thực tế trong két. 
  2. Số lượng hàng hóa (nước, mì) thực tế.
  3. Bàn giao cho nhân viên ca tiếp theo xác nhận. Mục đích để quy trách nhiệm nếu mất tiền hoặc mất hàng.

### 5.5. Khuyến mãi (Promotions)
- **Bonus nạp tiền**: Nạp 100k tặng 20k (tiền khuyến mãi thường chỉ dùng để chơi, không dùng để mua đồ ăn).
- **Combo giờ chơi**: Chơi đêm (Night combo) từ 23h - 7h sáng giá cố định 30k.

---

## 6. Technology Stack (Cấu trúc Công Nghệ)
*Coder AI (OpenCode) cần chú ý các công nghệ sau để viết code cho đúng:*
- **Backend API**: `.NET Core (C#)`. Nằm trong thư mục `InternetCafe/apps/backend`.
- **Admin Web (Frontend)**: `React/Vite` (hoặc stack tương đương dùng Node.js/TypeScript). Nằm trong thư mục `InternetCafe/apps/admin-web`.
- **Desktop Client**: `.NET (WPF/WinForms)`. Nằm trong thư mục `InternetCafe/apps/desktop-client`.
- **Database**: `SQL Server`. Các script khởi tạo nằm trong thư mục `Database/`.
- **API Spec**: OpenAPI (Swagger) định nghĩa trong `contracts/openapi.yaml`. Mọi thay đổi API cần cập nhật file này.
