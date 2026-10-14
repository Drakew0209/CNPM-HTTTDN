# Đối chiếu yêu cầu & ghi chú trạng thái dự án

Ngày rà soát: 14/10/2026. Phạm vi rà soát: toàn bộ `CNPM-HTTTDN` (backend ASP.NET Core, web admin React, desktop client WPF, script CSDL, tài liệu).

Ký hiệu kết luận: **ĐẠT** = có và dùng được; **MỘT PHẦN** = có nhưng thiếu/khác yêu cầu; **THIẾU** = không tồn tại trong mã nguồn; **NGOÀI PHẠM VI** = đã được yêu cầu bỏ khỏi đề bài, không tính là khoảng trống.

> **Phạm vi đã điều chỉnh (14/10/2026)**: chức năng **quản lý lương (payroll)** đã được yêu cầu **bỏ** khỏi đề bài. Vì vậy mọi mục liên quan tới bảng lương — xem lương, in bảng lương tháng/năm, duyệt chi trả lương — được đánh dấu **NGOÀI PHẠM VI** và **không** tính vào khoảng trống cần khắc phục. Chấm công vẫn nằm trong phạm vi.

---

## Phần A — Ghi chú trạng thái (những điểm cần biết trước khi làm tiếp)

### A.1. Tài liệu trong `contracts/` và `docs/` đã lỗi thời

`contracts/implementation-contract.md`, `contracts/backend-gaps.md`, `docs/integration-boundary.md`, `docs/business-rules-review.md` đều được viết cho giai đoạn **chỉ có frontend + mock MSW**, khi "backend sản xuất chưa có". Hiện tại `apps/backend/InternetCafe.Backend` là backend ASP.NET Core thật, kết nối SQL Server LocalDB, có transaction và trigger CSDL.

Hệ quả:

- Các câu như "mock chỉ phục vụ demo", "chưa có backend production", "chưa có migration SQL" không còn đúng và dễ gây hiểu sai cho người đọc mới.
- Bảng phân quyền trong `backend-gaps.md` mô tả engine mock, không mô tả `ManagementRepository`/`CafeRepository` thật.
- `docs/business-rules-review.md` trỏ tới `../../.omx/delegations/...` — thư mục không tồn tại trong repo hiện tại.

**Việc cần làm**: đánh dấu rõ 4 file này là "tài liệu giai đoạn FE-mock" hoặc viết lại theo backend thực tế.

### A.2. Thông tin phiên bản trong `README.md` gốc sai

| Chỗ ghi trong `CNPM-HTTTDN/README.md` | Thực tế trong mã nguồn |
| --- | --- |
| "ASP.NET Core 9.0", ".NET 9.0" | `TargetFramework` là `net10.0` (backend) và `net10.0-windows` (client) |
| "React 18" | `react` và `react-dom` là `^19.2.0` |
| "Tailwind/Vanilla CSS, Lucide Icons" | Ant Design 6 (`antd ^6.6.5`) + `@ant-design/icons`; không có Tailwind, không có Lucide |
| "Entity Framework Core / Dapper" | EF Core SqlServer có trong csproj nhưng **không dùng**; toàn bộ truy cập dữ liệu là `Microsoft.Data.SqlClient` viết tay |
| "SQL Server / SQLite nhúng" | Chỉ có SQL Server LocalDB, không có SQLite |
| "SignalR" (bảng công nghệ) | Có `CafeHub.cs` và client `@microsoft/signalr`, nhưng `VITE_SIGNALR_URL` để trống trong `.env.example` |

`InternetCafe/README.md` (file con) chính xác hơn: ghi đúng .NET SDK 10 và SQL Server LocalDB.

**Việc cần làm**: đồng bộ lại bảng công nghệ ở `README.md` gốc.

### A.3. Hai chế độ chạy dữ liệu song song, dễ nhầm

- `npm run dev` → `VITE_API_MODE=mock` → MSW chạy trong trình duyệt, dữ liệu **riêng từng tab**, không liên quan backend.
- `npm run demo:real` → `VITE_API_MODE=http` → `VITE_API_BASE_URL=http://127.0.0.1:5055/api/v1`, dùng chung với WPF.
- `npm run demo` → mock server loopback (`mock/server.ts`, tsx) + web.

Giao diện vẫn hiển thị nhãn "DEMO" hoặc "SQL SERVER" theo `apiMode`, nhưng footer luôn ghi "Frontend demo · Backend và khóa Windows chưa tích hợp" — **câu này hiện đã sai** khi chạy `demo:real` vì backend đã được tích hợp thật.

### A.4. Không có bảng `Accounts` trong CSDL

`Database/InternetCafe Final.sql` tạo 24 bảng, không có `dbo.Accounts`. Danh sách tài khoản được **suy ra** từ `Employees` (nhân viên, có username/password/role) ghép với `Customers`. Vì vậy:

- API `GET /accounts` trả về dữ liệu dẫn xuất, không phải một thực thể độc lập.
- Không thể tạo một tài khoản không gắn với nhân viên hoặc khách hàng.

### A.5. Vai trò `Admin` bị giới hạn có chủ đích

Trong `App.tsx`, `permission[Admin]` và `backend-gaps.md` đều định nghĩa Admin **chỉ** quản trị tài khoản/phân quyền và tra cứu sản phẩm, nhà cung cấp — không được xử lý tiền, đơn hàng, kho, nhân sự hay báo cáo. Đây là thiết kế tách nhiệm, không phải lỗi, nhưng khác với cách hiểu thông thường "admin là toàn quyền".

### A.6. `payroll` có ở backend nhưng không có giao diện — **NGOÀI PHẠM VI**

- Backend vẫn tồn tại: `POST /payroll/calculate-month` (tính lương tháng từ lịch làm + chấm công), `CreateAsync`/`UpdateAsync` có nhánh `payroll`, `GET /payroll` có phân quyền; `payroll` cũng có trong `/workspace` (`CafeRepository.cs`, dòng ~28).
- Frontend **không có** module `payroll` trong `modules.ts`, không có tab lương trong `StaffPage.tsx`, không có bảng lương trong `ReportsPage.tsx`.

Trước đây đây là khoảng trống lớn nhất giữa backend và frontend. **Nay không còn là khoảng trống**: chức năng quản lý lương đã được yêu cầu bỏ khỏi đề bài. Ghi chú này giữ lại chỉ để giải thích vì sao code backend vẫn còn phần payroll — cần dọn dẹp nếu muốn mã nguồn khớp hoàn toàn với phạm vi mới (xem C.3).

### A.7. `dbo.Employees` chưa có cột ngày sinh

`ReportsPage.tsx` đã xử lý đúng bằng cách báo "Chưa có ngày sinh" thay vì bịa dữ liệu, nhưng yêu cầu thống kê nhân sự theo độ tuổi vì thế không thực hiện được.

### A.8. Chấm công chưa kiểm tra khung giờ ca

`ManagementService` ghi nhận check-in/check-out nhưng không đối chiếu giờ hiện tại với `Work_Shifts.Start_Time/End_Time` để phát hiện đi trễ/về sớm. `backend-gaps.md` cũng tự ghi nhận: "mock chưa kiểm tra giờ bắt đầu/kết thúc ca".

### A.9. Trừ tiền phiên chơi do backend quyết định, desktop chỉ hiển thị

`SessionMonitor.cs` kiểm tra mỗi 5 giây; heartbeat quá `OfflineSessionGraceSeconds` (300s) thì backend tự chốt phiên tại mốc heartbeat cuối và trả máy về `Available`. Client **không** tự trừ tiền. Điều này đúng yêu cầu nhưng có nghĩa: nếu máy trạm mất mạng, phiên vẫn chạy tiếp tới 5 phút trước khi bị chốt.

### A.10. Khóa Windows/Kiosk chưa có thật

`MainWindow` của WPF có `LockApp`/`UnlockApp`/`LockTemporary` nhưng chỉ là lớp phủ trong ứng dụng. Không có hook Win32, không có service chặn Ctrl+Alt+Del/Task Manager. Comment trong `MachinesPage.tsx` và footer cũng xác nhận điều này.

### A.11. Nhãn "Nguồn: mock" còn sót trong báo cáo

`ReportsPage.tsx` hiển thị `<Tag color="gold">Nguồn: mock</Tag>` cố định, kể cả khi đang chạy với SQL Server thật.

### A.12. Khu vực Admin **có** tách giao diện nhưng chung một SPA

`App.tsx` đổi toàn bộ menu, nhãn sidebar ("HỆ THỐNG" / "MANAGEMENT STUDIO" / "CỔNG NHÂN VIÊN"), và thêm class `system-shell`/`staff-shell` để đổi màu `brand-mark` và `brand-accent` trong `styles.css`. Đây là tách ở mức điều hướng và nhận diện, không phải hai ứng dụng riêng biệt.
---

## Phần B — Bảng đối chiếu yêu cầu

### B.1. Yêu cầu 2 — Giao diện ADMIN

**Yêu cầu**: "Giao diện của Admin server phải tách biệt với giao diện của quản lý nhân sự, quản lý kho, quản lý bán hàng. Admin thêm xóa, phân quyền cho account."

| # | Yêu cầu con | Kết luận | Bằng chứng / khoảng trống |
| --- | --- | --- | --- |
| 1 | Giao diện Admin tách biệt khỏi giao diện quản lý | **MỘT PHẦN** | `App.tsx` dòng 40–42: khi `role === 'Admin'`, menu được thay hoàn toàn bằng 4 mục `/system/*`, layout nhận class `system-shell`, sidebar ghi "HỆ THỐNG". Quản lý/thu ngân dùng menu `/management/*` hoàn toàn khác, class mặc định, sidebar ghi "MANAGEMENT STUDIO". Tuy nhiên vẫn là **cùng một ứng dụng React, cùng bundle, cùng URL gốc**, chỉ khác cấu hình menu theo role. Nếu đề bài yêu cầu hai giao diện riêng biệt thật sự (hai app/section độc lập), hiện chưa đạt. |
| 2 | Admin thêm account | **ĐẠT** | Module `accounts` trong `modules.ts` có `create: true`; `POST /{resource}` với `resource = "accounts"` → `CreateAccountAsync` trong `ManagementService.cs`. Route UI: `/system/accounts`, chỉ role `Admin` (`Guard roles={['Admin']}`). |
| 3 | Admin xóa account | **ĐẠT (soft delete)** | Module `accounts` có `remove: true`; `DELETE /accounts/{id}` đặt `Status = 'Banned'` (deactivation, bảo toàn lịch sử) chứ không xóa cứng. |
| 4 | Admin phân quyền cho account | **ĐẠT** | Trường `role` trong form tạo/sửa account, options `Admin/Owner/Manager/Cashier/Staff`; trường `employeeId` liên kết nhân viên (bắt buộc khi role = Staff). Có thêm trang tĩnh `/system/permissions` — "Ma trận quyền" — nhưng trang này **chỉ mô tả** quyền bằng văn bản, không cho chỉnh sửa. |
| 5 | Admin tra cứu sản phẩm / nhà cung cấp | **ĐẠT** | `/system/products`, `/system/suppliers` với `readonly`. |
| 6 | Admin bị chặn khỏi nghiệp vụ vận hành | **ĐẠT (có chủ đích)** | Xem ghi chú A.5. `GET /accounts` trong `Program.cs` gắn `[Authorize(Roles = Roles.Admin)]`. |

**Khoảng trống của nhóm này**

- Chưa có app/tuyến URL tách hẳn cho Admin; chỉ là nhánh menu trong cùng SPA.
- "Ma trận quyền" là trang tĩnh, không phải công cụ cấu hình quyền thực sự.
- Không có audit log cho hành động cấp/thu quyền.

### B.2. Yêu cầu 3 — Mô phỏng hệ thống HRM

#### B.2.a. Người quản lý

| # | Yêu cầu con | Kết luận | Bằng chứng / khoảng trống |
| --- | --- | --- | --- |
| 1 | Thêm một nhân sự mới | **ĐẠT** | Module `employees` (`create: true`) → `CreateEmployeeAsync`. Form gồm họ tên, username, mật khẩu ban đầu, điện thoại, email, bộ phận, chức vụ, trình độ, lương cơ bản, ngày vào làm, trạng thái. |
| 2 | Xóa nhân sự | **ĐẠT (soft delete)** | Module `employees` có `remove: true`; `DeleteAsync` đặt `Status = 'Inactive'`. Bảng `dbo.Employees` và `dbo.Payroll` giữ nguyên lịch sử. |
| 3 | Duyệt đơn từ nhân sự: nghỉ phép, nghỉ việc | **ĐẠT** | Module `leaves` + `POST /leaves/{id}/decision` → `DecideLeaveAsync`. Trường `type` gồm `Annual / Sick / Resignation`. Approve đơn `Annual` sẽ set `Work_Schedules.Status = 'OnLeave'`. |
| 4 | Báo cáo tình hình nhân sự theo tháng: đang làm / nghỉ phép / nghỉ việc | **MỘT PHẦN** | `ReportsPage.tsx` tab `hr` có biểu đồ "Phòng ban", "Vị trí công việc", "Trình độ", "Thâm niên" và metric "Đang làm việc" (theo `Status = Active`). **Không có** bảng thống kê ai đang nghỉ phép/đã nghỉ việc trong tháng, và **không lọc theo tháng** — bộ lọc ngày áp lên `transactions`/`inventory`, không áp lên `employees`. |
| 5 | Báo cáo trình độ, mức lương, thâm niên | **MỘT PHẦN** | Trình độ: ĐẠT (biểu đồ + cột bảng). Thâm niên: ĐẠT (`fullYears(joinedAt)` → nhóm "Dưới 1 năm"…"Từ 5 năm"). **Mức lương: THIẾU** — `baseSalary` không xuất hiện trong cột bảng hay biểu đồ nào của tab nhân sự. |
| 6 | Cấp quyền cho nhân sự (nhân viên → trưởng nhóm thì quyền đổi ra sao) | **THIẾU (với quản lý)** | Module `accounts` (nơi duy nhất gán `role`) có `roles: ['Admin']` và route được bọc `Guard roles={['Admin']}`. Manager/Owner **không** vào được `/system/accounts`, nên không thể cấp quyền. Cũng không có khái niệm "trưởng nhóm" trong `dbo.Positions` lẫn `Roles` (`Admin/Owner/Manager/Cashier/Staff`). |
| 7 | Chấm công cho nhân sự | **ĐẠT** | Module `attendance`; `StaffPage.tsx` có nút "Chấm công vào/ra"; backend `POST /attendance/check-in`, `POST /attendance/{id}/check-out`, `POST /attendance/{id}/admin-checkout` (quản lý sửa công hộ). |
| 8 | ~~Tính lương cho nhân sự~~ | **NGOÀI PHẠM VI** | Đã bỏ theo yêu cầu. Backend còn `POST /payroll/calculate-month` và nhánh `payroll` trong `ManagementService.cs` — không cần nối vào giao diện. |

#### B.2.b. Nhân viên

| # | Yêu cầu con | Kết luận | Bằng chứng / khoảng trống |
| --- | --- | --- | --- |
| 1 | Xem, sửa thông tin của chính mình | **ĐẠT** | `/staff/home` (`StaffPage.tsx`), tab "Hồ sơ & bảo mật". `GET /me`, `PATCH /me` (fullName, phone, email, qualification), `POST /me/password`. |
| 2 | Nộp đơn xin nghỉ phép, nghỉ ốm, thai sản, nghỉ việc | **MỘT PHẦN** | Nút "Gửi đơn nghỉ" trong `StaffPage.tsx`. Loại đơn khả dụng: `Annual`, `Sick`, `Resignation`. **THIẾU loại "nghỉ thai sản"** (`Maternity`) — cả UI, `labels` trong `domain.ts`, và `dbo.Leave_Requests.Leave_Type` đều không có. |
| 3 | ~~Xem cách tính lương và lương mỗi tháng của chính mình~~ | **NGOÀI PHẠM VI** | Đã bỏ theo yêu cầu. `StaffPage.tsx` hiện có 3 tab: "Lịch làm & chấm công", "Đơn nghỉ của tôi", "Hồ sơ & bảo mật" — đúng phạm vi mới, không cần bổ sung tab lương. |
| 4 | ~~In bảng lương theo tháng~~ | **NGOÀI PHẠM VI** | Đã bỏ theo yêu cầu. |
| 5 | ~~In bảng lương theo năm~~ | **NGOÀI PHẠM VI** | Đã bỏ theo yêu cầu. |

**Tổng kết HRM (sau khi điều chỉnh phạm vi)**: 5/9 mục trong phạm vi ĐẠT, 3 MỘT PHẦN, 1 THIẾU (`Maternity`); 3 mục lương NGOÀI PHẠM VI. Trọng tâm còn lại là **báo cáo nhân sự theo tháng**, **cấp quyền cho nhân sự**, và **loại đơn nghỉ thai sản**.
### B.3. Yêu cầu 4 — Mô phỏng hệ thống CRM

Mục tiêu đề bài: "đánh giá được quá trình quản lý khách hàng của doanh nghiệp. Độ hài lòng của khách hàng, thăm dò khách hàng khi chuẩn bị ra mắt sản phẩm mới."

#### B.3.a. Người quản lý

| # | Yêu cầu con | Kết luận | Bằng chứng / khoảng trống |
| --- | --- | --- | --- |
| 1 | Thêm một khách hàng mới | **ĐẠT** | Module `customers` (`create: true`, `roles: ops` = Owner/Manager/Cashier) → `CreateCustomerAsync`. Form: họ tên, username, mật khẩu ban đầu, CCCD/CMND, điện thoại, email, ngày sinh, sở thích, hạng. |
| 2 | Xóa khách hàng | **ĐẠT (soft delete)** | Module `customers` có `remove: true`; `DeleteAsync` → `SetStatusAsync(..., "Inactive")`. Lịch sử giao dịch/phiên được giữ. |
| 3 | Khóa tài khoản khách hàng | **ĐẠT** | Sửa khách hàng và đặt `status = 'Banned'`; `StatusTag` hiển thị "Đã khóa". `AuthService`/`ApiTokenAuthenticationHandler` từ chối token khi tài khoản bị ban (`ACCOUNT_BANNED`). |
| 4 | Tiếp nhận phản hồi của khách hàng | **ĐẠT** | Module `feedback` (chỉ `mgr`), sửa được `status` (Pending/Processing/Resolved) và `response`. |
| 5 | Báo cáo về khách hàng, tỷ lệ độ tuổi, sở thích | **ĐẠT** | `ReportsPage.tsx` tab "Khách hàng": biểu đồ "Hạng thành viên", "Nhóm tuổi" (Dưới 18 / 18–24 / 25–34 / 35+), "Sở thích" (tách theo dấu `,`/`;`), bảng hồ sơ + chi tiêu thuần + nạp trong kỳ. |
| 6 | Tạo bảng khảo sát và gửi đến tài khoản khách hàng | **ĐẠT** | Module `surveys` (`create: true`) → `CreateSurveyAsync`; `POST /surveys/{id}/publish` với danh sách `customerIds` → `PublishSurveyCoreAsync`. Khảo sát hiện ra ở tab "Khảo sát" của WPF. |
| 7 | Thống kê kết quả khảo sát | **ĐẠT** | `ResourcePage.tsx` dòng 148: khối "Kết quả khảo sát" hiển thị `số khách đã trả lời / tổng số được mời` và đếm số lượt chọn cho từng option của từng câu hỏi. Tuy nhiên **chỉ xem được trong modal chi tiết của một khảo sát**, chưa có báo cáo tổng hợp trong `ReportsPage`. |

#### B.3.b. Khách hàng

| # | Yêu cầu con | Kết luận | Bằng chứng / khoảng trống |
| --- | --- | --- | --- |
| 1 | Đăng ký tài khoản khách hàng | **ĐẠT** | WPF có `ToggleRegistration` + `RegisterCommand`; backend `POST /auth/register` (`RegisterRequest` gồm username, password, fullName, phone, email, dateOfBirth, hobbies). |
| 2 | Đăng nhập | **ĐẠT** | WPF màn hình khóa đăng nhập; backend `POST /auth/login`. Tài khoản `Customer` **bị chặn** ở web admin (`App.tsx` báo "Tài khoản khách sử dụng ứng dụng desktop"). |
| 3 | Chỉnh sửa thông tin | **ĐẠT** | WPF tab "Hồ sơ": họ tên, điện thoại, email, ngày sinh, sở thích + đổi mật khẩu. Backend `PATCH /me`, `POST /me/password`. |
| 4 | Gửi phản hồi về sản phẩm | **ĐẠT** | WPF tab "Hỗ trợ": `FeedbackSubject` + `FeedbackContent` + nút "Gửi hỗ trợ"; nút "Gọi hỗ trợ" ở tab Tổng quan tự điền sẵn `"Hỗ trợ tại {MachineName}"`. Backend `POST /feedback`. |
| 5 | Nhận được khảo sát và thực hiện | **ĐẠT** | WPF tab "Khảo sát": combobox chọn khảo sát, render câu hỏi + options, nút "Gửi câu trả lời"; backend `POST /surveys/{id}/responses` (một lần mỗi khách, `DUPLICATE_RESPONSE` nếu lặp). |

**Tổng kết CRM**: 12/12 mục có, trong đó 11 ĐẠT và 1 hạn chế về vị trí hiển thị (thống kê khảo sát chỉ nằm trong modal chi tiết). Đây là module hoàn thiện nhất.

**Khoảng trống phụ của CRM**

- Không có "điểm thưởng/tiền cộng tài khoản khi trả lời khảo sát" dù `Chức năng.txt` mô tả ("nhận điểm thưởng hoặc tiền cộng thêm vào tài khoản") — hiện chỉ ghi nhận câu trả lời.
- Không có lịch sử tương tác CRM gộp (một khách: phản hồi + khảo sát + giao dịch + phiên) trong cùng một màn hình; phải tra từng module.

### B.4. Yêu cầu 5 — Mô phỏng hệ thống MRP

Mục tiêu đề bài: "quản lý quá trình sản xuất ra sản phẩm của doanh nghiệp. Quá trình lưu kho nguyên vật liệu."

> **Kết luận tổng: THIẾU TOÀN BỘ.** Không có bảng, endpoint, service hay màn hình nào cho MRP.

Bằng chứng cụ thể:

- `Database/InternetCafe Final.sql` tạo 24 bảng. **Không có** `Materials`, `Bill_Of_Materials`, `BOM`, `Production_Orders`, `Production_Plans` hay bất kỳ bảng nào liên quan nguyên vật liệu/thành phẩm. Truy vấn `Select-String -Pattern 'Materials|BOM|Production|Bill_Of'` trên file SQL trả về **0 kết quả**.
- Bảng duy nhất liên quan kho là `dbo.Inventory_Transactions`, gắn trực tiếp với `Product_ID` (món ăn/uống bán cho khách), **không** có khái niệm nguyên vật liệu đầu vào hay thành phẩm đầu ra.
- `ManagementService.cs` chỉ có `AppendInventoryAsync` cho `resource = "inventory"`; `CreateAsync`/`UpdateAsync` switch **không có** nhánh nào cho MRP.
- `Program.cs` **không có** endpoint MRP. Các endpoint gần nhất là `GET /inventory`, `GET /suppliers`, `POST /{resource}` với `resource = "inventory" | "suppliers"`.
- `modules.ts` chỉ có module `inventory` với `type: ['Import','Export']` — đây là nhập/xuất **hàng bán**, không phải nhập nguyên vật liệu và xuất thành phẩm.

#### B.4.a. Người quản lý kho

| # | Yêu cầu con | Kết luận | Ghi chú |
| --- | --- | --- | --- |
| 1 | Tạo phiếu nhập kho tăng số lượng nguyên vật liệu | **MỘT PHẦN (lệch bản chất)** | Có phiếu `Import` và nó tăng `Products.Stock_Quantity`, nhưng đối tượng là **sản phẩm bán**, không phải nguyên vật liệu. |
| 2 | Phê duyệt phiếu xuất nguyên vật liệu từ nhân viên | **THIẾU** | Không có luồng "đề nghị → phê duyệt". `inventory` cho phép Manager tạo phiếu `Export` trực tiếp, không qua duyệt. |
| 3 | Phê duyệt phiếu xuất thành phẩm | **THIẾU** | Không tồn tại khái niệm thành phẩm. |
| 4 | Báo cáo tồn/xuất/lưu theo tháng, quý, năm cho vật liệu và sản phẩm | **MỘT PHẦN** | Có báo cáo tồn nhập/xuất theo kỳ cho `products` trong tab "Nhập · xuất · tồn". Chỉ theo **khoảng ngày tự chọn** — không có preset tháng/quý/năm, và không có dữ liệu vật liệu. |
| 5 | Tạo công thức hình thành sản phẩm (BOM) — "1 cái bàn = 1 mặt bàn + 4 chân bàn" | **THIẾU** | Không có bảng BOM, không có UI, không có endpoint. |
| 6 | Đề nghị số lượng thành phẩm cần và gửi cho nhân viên | **THIẾU** | Không có. |

#### B.4.b. Nhân viên

| # | Yêu cầu con | Kết luận | Ghi chú |
| --- | --- | --- | --- |
| 1 | Nhận yêu cầu, kiểm tra thiếu nguyên vật liệu, đề nghị xuất nguyên vật liệu | **THIẾU** | Vai trò `Staff` trong hệ thống hiện tại là **cổng nhân sự cá nhân** (hồ sơ, lịch làm, chấm công, nghỉ phép), không có bất kỳ chức năng kho nào. `StaffPage.tsx` khẳng định điều này. |
| 2 | Lập đề nghị xuất kho nguyên vật liệu | **THIẾU** | Không có. |
| 3 | Dựa vào nguyên vật liệu đã có, tạo nên thành phẩm | **THIẾU** | Không có. |
| 4 | Lập phiếu đề nghị xuất thành phẩm để bán, gửi quản lý | **THIẾU** | Không có. |

**Tổng kết MRP**: 0/10 mục ĐẠT; 2 mục "MỘT PHẦN" chỉ vì có phiếu kho F&B trùng tên gọi, không phải MRP thật. Toàn bộ yêu cầu 5 cần làm mới từ CSDL trở lên.
### B.5. Đối chiếu 14 chức năng bắt buộc

| # | Chức năng yêu cầu | Kết luận | Nơi thực hiện | Ghi chú |
| --- | --- | --- | --- | --- |
| 1 | Quản lý tài khoản: đăng nhập và phân quyền người dùng | **ĐẠT** | `AuthService.cs`, `ApiTokenAuthenticationHandler.cs`, `POST /auth/login`, `POST /auth/logout`, `POST /auth/register`; module `accounts` + `/system/accounts` + `/system/permissions` | 6 vai trò: Admin, Owner, Manager, Cashier, Staff, Customer. Token mang role/identity; kiểm tra quyền lại mỗi request. |
| 2 | Quản lý phòng máy và máy trạm | **ĐẠT** | `MachinesPage.tsx`; `GET /computers`, `PATCH /computers/{id}`, `POST /computers/{id}/command`, `POST /machines/{id}/heartbeat`; `SessionMonitor.cs` | Sơ đồ máy dạng lưới, lọc theo khu vực/trạng thái/kết nối; bật bảo trì, đổi giá, đổi khu vực; heartbeat 5 phút. |
| 3 | Quản lý thuê máy | **ĐẠT** | `POST /sessions/start`, `POST /sessions/{id}/end`, `POST /sessions/{id}/transfer`; module `sessions` | Có cả **chuyển máy** (`handleTransfer` trong `MachinesPage.tsx`), khoá giá lúc bắt đầu, chốt tiền phía server. |
| 4 | Quản lý nạp tiền và ví khách hàng | **ĐẠT** | `POST /topups` (khách), `POST /topups/{id}/decision` (duyệt), `POST /customers/{id}/topup` (nạp tại quầy), `POST /customers/{id}/refund`; `TopupToastNotification.tsx` | Có VietQR ở WPF, thông báo realtime cho thu ngân, tách TopUp khỏi doanh thu. |
| 5 | Quản lý sản phẩm ăn uống | **ĐẠT** | Module `products` + `categories` (`CreateProductAsync`…); `GET/POST/PATCH/DELETE /products`, `/categories` | Có ảnh, giá, tồn, trạng thái đang bán. |
| 6 | Quản lý đơn hàng dịch vụ | **ĐẠT** | Module `orders` (`Pending → Preparing → Served / Cancelled`); `POST /orders`, `POST /orders/{id}/transition` | Đơn gắn `computerId` + `customerId`; hủy đơn có hoàn tiền và hoàn kho. |
| 7 | Quản lý nhập hàng | **MỘT PHẦN** | Module `inventory` (`Import`/`Export`), `suppliers`, `AppendInventoryAsync` | Có nhập/xuất và nhà cung cấp, nhưng: **không có phiếu đặt hàng (purchase order)**, **không có luồng phê duyệt**, **không có giá vốn / công nợ NCC**, số lượng nhập không neo theo hoá đơn. |
| 8 | Quản lý khách hàng và hội viên | **ĐẠT** | Module `customers`; `dbo.Membership_Tier`; báo cáo tab "Khách hàng" | Hạng `Member/VIP/Silver/Gold/Diamond`, CCCD, ngày sinh, sở thích, số dư, khoá tài khoản. |
| 9 | Quản lý nhân viên và chức vụ | **MỘT PHẦN** | Module `employees` (`department`, `position`); `dbo.Departments`, `dbo.Positions` | Có hồ sơ, bộ phận, chức vụ. **Thiếu**: danh mục `Positions`/`Departments` không có màn hình CRUD riêng (chỉ là danh sách cứng trong `modules.ts`: "Vận hành/Kỹ thuật/Thu ngân", "Quản lý ca/Kỹ thuật viên/Nhân viên thu ngân/Nhân viên phục vụ"), và không có khái niệm "trưởng nhóm"/thăng chức kèm đổi quyền. |
| 10 | Quản lý ca làm việc và chấm công | **MỘT PHẦN** | Module `shifts`, `schedules`, `attendance`; `POST /attendance/check-in`, `/check-out`, `/admin-checkout` | Có ca, phân lịch, chấm công, sửa công bởi quản lý. **Thiếu**: không kiểm tra giờ vào/ra so với khung ca (không phát hiện trễ/sớm), không có tăng ca, không có khoá sửa công kèm audit. |
| 11 | Tiếp nhận phản hồi và yêu cầu hỗ trợ | **ĐẠT** | Module `feedback`; WPF tab "Hỗ trợ" + nút "Gọi hỗ trợ"; `POST /feedback` | Khách gửi, quản lý đổi trạng thái và trả lời. |
| 12 | Khảo sát ý kiến khách hàng | **ĐẠT** | Module `surveys`; `POST /surveys/{id}/publish`, `POST /surveys/{id}/responses`; WPF tab "Khảo sát"; thống kê trong `ResourcePage.tsx` | Soạn câu hỏi, chọn người nhận, khách trả lời một lần, có đếm kết quả từng phương án. |
| 13 | Quản lý hoá đơn | **ĐẠT** | `InvoicesPage.tsx`; `GET /invoices`, `GET /invoices/{id}` (chỉ Owner/Manager/Cashier) | Hỗ trợ 3 loại: nạp tiền, dịch vụ F&B, giờ chơi. Có in (`window.print()` dòng 117) và tra cứu theo mã/loại/khách/máy. |
| 14 | Báo cáo và thống kê hoạt động | **ĐẠT** | `ReportsPage.tsx`; `GET /reports/summary`, `GET /reports/analytics` | 4 tab: Doanh thu & món bán, Khách hàng, Nhập·xuất·tồn (chỉ Owner/Manager), Nhân sự & tiền lương (chỉ Owner/Manager). Có biểu đồ, xuất CSV, in. Xem giới hạn ở B.2.a mục 4–5. |

**Tổng kết 14 chức năng**: 10 ĐẠT, 4 MỘT PHẦN, 0 THIẾU.

---

## Phần C — Tổng hợp khoảng trống & thứ tự ưu tiên đề xuất

### C.1. Bảng tổng hợp nhanh

| Nhóm yêu cầu | ĐẠT | MỘT PHẦN | THIẾU | Mức độ hoàn thành |
| --- | --- | --- | --- | --- |
| Yêu cầu 2 — Giao diện Admin | 4 | 2 | 0 | ~75% |
| Yêu cầu 3 — HRM | 5 | 3 | 1 | ~65% (3 mục lương ngoài phạm vi) |
| Yêu cầu 4 — CRM | 11 | 1 | 0 | ~95% |
| Yêu cầu 5 — MRP | 0 | 2 | 8 | ~5% |
| 14 chức năng bắt buộc | 10 | 4 | 0 | ~80% |

### C.2. Khoảng trống xếp theo mức ưu tiên

**P0 — Bắt buộc phải có theo đề bài, đang thiếu hẳn**

1. **Toàn bộ hệ thống MRP** (yêu cầu 5): cần thêm bảng CSDL (nguyên vật liệu, BOM/định mức, đề nghị xuất, phê duyệt, thành phẩm), endpoint, service, và 2 nhóm màn hình (quản lý kho phê duyệt, nhân viên lập đề nghị). Đây là khối lượng lớn nhất và là khoảng trống nghiêm trọng nhất.
2. **Loại đơn nghỉ thai sản** (`Maternity`) trong `Leave_Requests` + form gửi đơn.

**P1 — Có nhưng lệch yêu cầu, ảnh hưởng điểm đánh giá**

3. **Tách giao diện Admin thật sự** khỏi khu vực quản lý (hiện chỉ khác menu trong cùng SPA).
4. **Báo cáo nhân sự theo tháng** (đang làm / nghỉ phép / nghỉ việc) — hiện chỉ có số đếm trạng thái hiện tại, không lọc theo tháng.
5. **Cấp quyền cho nhân sự bởi quản lý** và mô hình thăng chức (nhân viên → trưởng nhóm).
6. **Luồng nhập hàng có phê duyệt** và giá vốn nhà cung cấp.

**P2 — Chất lượng & nhất quán**

7. Đồng bộ `README.md` gốc với phiên bản thật (.NET 10, React 19, Ant Design 6, không Tailwind/Lucide/SQLite/Dapper).
8. Đánh dấu hoặc viết lại 4 tài liệu `contracts/`/`docs/` theo backend thật.
9. Bỏ nhãn cứng "Nguồn: mock" trong `ReportsPage.tsx` khi `apiMode === 'http'`.
10. Sửa câu footer "Backend và khóa Windows chưa tích hợp" khi chạy `demo:real`.
11. Bổ sung `dateOfBirth` cho `dbo.Employees` nếu muốn thống kê tuổi nhân sự.
12. Kiểm tra khung giờ ca khi chấm công (trễ/sớm), và audit khi quản lý sửa công.
13. Thêm màn hình CRUD cho `Departments`/`Positions` thay vì danh sách cứng trong `modules.ts`.

---

## C.3. Việc dọn dẹp mã nguồn do bỏ chức năng lương

Chức năng quản lý lương đã bị bỏ khỏi đề bài nhưng **mã nguồn backend vẫn còn nguyên**. Nếu nhóm muốn mã nguồn khớp hoàn toàn với phạm vi mới, đây là danh sách phần thừa (không bắt buộc phải xoá, và xoá có thể gây rủi ro nên cần cân nhắc):

| Vị trí | Nội dung còn lại |
| --- | --- |
| `Database/InternetCafe Final.sql` | Bảng `dbo.Payroll` |
| `Services/ManagementService.cs` | `CreatePayrollAsync`, `UpdatePayrollAsync`, `CalculateMonthlyPayrollCoreAsync`, `PayrollObject`, hai nhánh `"payroll"` trong switch, `CalculateMonthlyPayrollAsync` |
| `Services/ManagementRepository.cs` | `PayrollAsync`, `PayrollReadDto`, `Payroll` trong record workspace, `PayrollStatus` |
| `Services/CafeRepository.cs` | `payroll = managementWorkspace.Payroll`, truy vấn `payrollExpense` trong `/reports/analytics` |
| `Program.cs` | `GET /payroll`, `POST /payroll/calculate-month` |
| `apps/admin-web/src/modules.ts` | Nhãn cột thừa: `month`, `bonus`, `deduction` trong `columnLabels` |
| `apps/desktop-client` | Không bị ảnh hưởng (chỉ dành cho khách hàng) |

> **Khuyến nghị**: **giữ nguyên** phần payroll ở backend. Nó không hiển thị ở giao diện nên không ảnh hưởng phạm vi báo cáo, còn xoá thì phải sửa cả CSDL, service, repository, endpoint và có thể làm hỏng `/reports/analytics` (biến `estimatedProfit` đang trừ `payrollExpense`). Nếu cần, chỉ nên **ẩn/xoá nhãn cột thừa** trong `modules.ts`.
