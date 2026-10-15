# Đối chiếu yêu cầu & ghi chú trạng thái dự án

Ngày rà soát gốc: 14/10/2026. **Cập nhật khắc phục toàn diện Giai đoạn 1, 2 & 3: 15/10/2026**.
Phạm vi rà soát: toàn bộ `CNPM-HTTTDN` (backend ASP.NET Core 10, web admin React 19 + Ant Design 6, desktop client WPF .NET 10, script CSDL SQL Server, tài liệu).

Ký hiệu kết luận: **ĐẠT** = có và dùng được; **MỘT PHẦN** = có nhưng thiếu/khác yêu cầu; **THIẾU** = không tồn tại trong mã nguồn; **NGOÀI PHẠM VI** = đã được yêu cầu bỏ khỏi đề bài, không tính là khoảng trống.

> **Phạm vi đã điều chỉnh**: chức năng **quản lý lương (payroll)** đã được yêu cầu **bỏ** khỏi đề bài theo chỉ đạo của Giảng viên. Vì vậy mọi mục liên quan tới tính lương — xem lương, in bảng lương tháng/năm, duyệt chi trả lương — được đánh dấu **NGOÀI PHẠM VI** và **không** tính vào khoảng trống cần khắc phục. Chấm công theo ca vẫn nằm trong phạm vi bắt buộc và đã được hoàn thiện.

---

## Phần A — Ghi chú trạng thái sau khắc phục (15/10/2026)

### A.1. Phân hệ MRP & BOM hoàn thiện 100%
Đã bổ sung CSDL (3 bảng: `dbo.Materials`, `dbo.Product_Materials`, `dbo.Material_Requests`), script DDL `Database/MRP_Extension.sql`, tích hợp vào `DatabaseBootstrapper.cs` và seed data tự động. Backend hỗ trợ CRUD NVL, định mức BOM, và nghiệp vụ duyệt xuất kho / sản xuất F&B tự động kiểm tra tồn kho, trừ NVL theo BOM và tăng tồn món ăn thành phẩm. Giao diện Web Admin đã tích hợp đầy đủ 3 module con trong nhóm menu "Sản xuất & Kho (MRP)" với cảnh báo tồn an toàn và báo cáo giá trị kho NVL.

### A.2. Cập nhật chuẩn hóa tài liệu & phiên bản
Đã đồng bộ lại `CNPM-HTTTDN/README.md` khớp 100% với thực tế:
- Backend: ASP.NET Core 10 (`net10.0`), C#, ADO.NET SqlClient kết nối SQL Server LocalDB `(localdb)\MSSQLLocalDB`, SignalR.
- Web Admin: React 19 (`^19.2.0`), Vite, Ant Design 6 (`^6.6.5`) + `@ant-design/icons`, Recharts.
- Client: WPF .NET 10 (`net10.0-windows`), XAML, Kiosk Client, VietQR động.

### A.3. Chấm công thông minh theo ca (Punctuality Compliance)
Nghiệp vụ chấm công trong `ManagementService.cs` (`CheckInCoreAsync` và `CheckOutCoreAsync`) đã tự động đối chiếu thời gian thực tế với khung giờ ca (`Work_Shifts.Start_Time / End_Time`). Hệ thống tự động ghi nhận và gắn tag: **Đúng giờ**, **Đi trễ X phút**, **Về sớm Y phút** và hiển thị trực quan trên Web Admin cũng như Cổng nhân viên (`StaffPage.tsx`).

### A.4. Bổ sung ngày sinh nhân viên & biểu đồ tháp tuổi HRM
`dbo.Employees.Date_Of_Birth` đã được map vào `EmployeeReadDto` ở backend và form cập nhật hồ sơ cá nhân. Tab Báo cáo nhân sự (`ReportsPage.tsx`) hiển thị biểu đồ phân bổ độ tuổi nhân sự thực tế, tổng quỹ lương cơ bản, và cột mức lương trong bảng dữ liệu.

### A.5. Báo cáo doanh thu & kho theo Tháng / Quý / Năm
Bổ sung bộ chọn nhanh khoảng thời gian (*Tất cả*, *Hôm nay*, *Tháng này*, *Quý này*, *Năm nay*) và biểu đồ/bảng tổng hợp doanh thu theo từng tháng (`Doanh thu theo tháng`) trong `ReportsPage.tsx`.

### A.6. Hoàn thiện Giai đoạn 3 (Payroll Cleanup, Transition Notes & Kịch bản Demo)
1. **Dọn sạch tàn dư cấu hình Lương**: Đã gỡ bỏ toàn bộ nhãn cột lương cũ (`month`, `bonus`, `deduction`) trong từ điển `columnLabels` của `modules.ts`.
2. **Gắn banner chuyển giao trên hợp đồng & tài liệu cũ**: 4 tài liệu hợp đồng cũ (`implementation-contract.md`, `backend-gaps.md`, `integration-boundary.md`, `business-rules-review.md`) được gắn ghi chú chuyển giao thực tế, khẳng định hệ thống đã chạy trên Backend ASP.NET Core 10 sản xuất và SQL Server LocalDB.
3. **Biên soạn Kịch bản Demo chi tiết**: Đã tạo file [docs/KichBanDemo.md](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/docs/KichBanDemo.md) cung cấp cẩm nang 4 kịch bản biểu diễn thực chiến (MRP, HRM, CRM, Báo cáo) phục vụ buổi vấn đáp bảo vệ đồ án.

### A.7. Khắc phục trải nghiệm vận hành thực tế (Authentication & Port Binding)
1. **Duy trì phiên đăng nhập khi F5**: Nâng cấp `readSession()` và `writeSession()` trong `api.ts` lưu phiên đồng thời vào `localStorage` và `sessionStorage`. Đồng thời bổ sung cơ chế khôi phục token trong Mock Engine. Người dùng F5 tải lại trang hay mở tab mới sẽ không bao giờ bị văng ra màn hình đăng nhập.
2. **Chuẩn hóa cổng Backend 5055**: Đồng bộ cấu hình trong `launchSettings.json` và script [chay-backend.bat](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/chay-backend.bat) cố định lắng nghe tại `http://127.0.0.1:5055`. Khắc phục triệt để lỗi mạng *"Không kết nối được máy chủ"*.
3. **Đồng nhất danh tính tài khoản trải nghiệm**: Chuẩn hóa thông tin tài khoản demo `gamer` / `Demo@123` cố định họ tên là **`Nguyễn Văn An`** trên cả CSDL SQL Server và mã nguồn dự phòng ngoại tuyến (`StandaloneDemoApi.cs`), xóa bỏ hoàn toàn hiện tượng lệch tên khi chạy.

---

## Phần B — Bảng đối chiếu yêu cầu chi tiết

### B.1. Yêu cầu 2 — Giao diện ADMIN

**Yêu cầu**: "Giao diện của Admin server phải tách biệt với giao diện của quản lý nhân sự, quản lý kho, quản lý bán hàng. Admin thêm xóa, phân quyền cho account."

| # | Yêu cầu con | Kết luận | Bằng chứng / trạng thái |
| --- | --- | --- | --- |
| 1 | Giao diện Admin tách biệt khỏi giao diện quản lý | **ĐẠT (Tách theo Role Shell)** | `App.tsx`: khi `role === 'Admin'`, menu được thay hoàn toàn bằng 4 mục `/system/*`, layout nhận class `system-shell`, sidebar ghi "HỆ THỐNG". Quản lý/thu ngân dùng menu `/management/*` riêng biệt, class mặc định, sidebar ghi "MANAGEMENT STUDIO". Route bọc Guard nghiêm ngặt. |
| 2 | Admin thêm account | **ĐẠT** | Module `accounts` trong `modules.ts` có `create: true`; `POST /accounts` → `CreateAccountAsync`. Route UI: `/system/accounts`, chỉ role `Admin`. |
| 3 | Admin xóa account | **ĐẠT (soft delete)** | Module `accounts` có `remove: true`; `DELETE /accounts/{id}` đặt `Status = 'Banned'` (bảo toàn lịch sử). |
| 4 | Admin phân quyền cho account | **ĐẠT** | Trường `role` trong form tạo/sửa account (Admin/Owner/Manager/Cashier/Staff); liên kết mã nhân viên; trang Ma trận quyền `/system/permissions`. |
| 5 | Admin tra cứu sản phẩm / nhà cung cấp | **ĐẠT** | `/system/products`, `/system/suppliers` với chế độ `readonly`. |
| 6 | Admin bị chặn khỏi nghiệp vụ vận hành | **ĐẠT (có chủ đích)** | Bị chặn ở cả giao diện và API Backend (`[Authorize(Roles = Roles.Admin)]`). |

---

### B.2. Yêu cầu 3 — Mô phỏng hệ thống HRM

#### B.2.a. Người quản lý

| # | Yêu cầu con | Kết luận | Bằng chứng / trạng thái |
| --- | --- | --- | --- |
| 1 | Thêm một nhân sự mới | **ĐẠT** | Module `employees` (`create: true`) → `CreateEmployeeAsync`. Form gồm họ tên, username, mật khẩu, điện thoại, email, ngày sinh, bộ phận, chức vụ, trình độ, lương cơ bản, ngày vào làm, trạng thái. |
| 2 | Xóa nhân sự | **ĐẠT (soft delete)** | Module `employees` có `remove: true`; `DeleteAsync` đặt `Status = 'Inactive'`. Bảng `dbo.Employees` giữ nguyên lịch sử. |
| 3 | Duyệt đơn từ nhân sự: nghỉ phép, nghỉ việc | **ĐẠT** | Module `leaves` + `POST /leaves/{id}/decision` → `DecideLeaveAsync`. Hỗ trợ duyệt `Annual / Sick / Maternity / Resignation`. Duyệt đơn nghỉ sẽ set `Work_Schedules.Status = 'OnLeave'`. |
| 4 | Báo cáo tình hình nhân sự theo tháng: đang làm / nghỉ phép / nghỉ việc | **ĐẠT** | `ReportsPage.tsx` tab `hr` có metric phân loại "Đang làm việc" vs "Đã thôi việc / Tạm dừng", bảng thống kê nghỉ phép trong kỳ lọc theo tháng/quý/năm, biểu đồ tháp tuổi và thâm niên. |
| 5 | Báo cáo trình độ, mức lương, thâm niên | **ĐẠT** | Trình độ: biểu đồ + cột bảng. Thâm niên: nhóm thâm niên. Mức lương: cột `baseSalary` trong bảng nhân sự và metric Tổng quỹ lương cơ bản tháng. |
| 6 | Cấp quyền cho nhân sự (nhân viên → trưởng nhóm thì quyền đổi ra sao) | **ĐẠT** | Quản lý có thể cập nhật chức vụ nhân viên sang "Trưởng nhóm kỹ thuật / ca" hoặc "Quản lý ca" trong module `employees`. `UpdateEmployeeAsync` tự động đồng bộ nâng vai trò `App_Accounts.Role` tương ứng. |
| 7 | Chấm công cho nhân sự | **ĐẠT (Nâng cao)** | Module `attendance`; `StaffPage.tsx` chấm công vào/ra. Tự động đối chiếu giờ ca `Work_Shifts` để ghi nhận **Đi trễ**, **Về sớm**, **Đúng giờ**. Quản lý có modal can thiệp chốt ca hộ khi nhân viên quên chấm công ra. |
| 8 | ~~Tính lương cho nhân sự~~ | **NGOÀI PHẠM VI** | Đã bỏ theo yêu cầu của GV. |

#### B.2.b. Nhân viên

| # | Yêu cầu con | Kết luận | Bằng chứng / trạng thái |
| --- | --- | --- | --- |
| 1 | Xem, sửa thông tin của chính mình | **ĐẠT** | `/staff/home` (`StaffPage.tsx`), tab "Hồ sơ & bảo mật". `GET /me`, `PATCH /me` (họ tên, điện thoại, email, ngày sinh, trình độ), `POST /me/password`. |
| 2 | Nộp đơn xin nghỉ phép, nghỉ ốm, thai sản, nghỉ việc | **ĐẠT** | Nút "Gửi đơn nghỉ" trong `StaffPage.tsx`. Hỗ trợ đầy đủ 4 loại đơn: Thường niên (`Annual`), Nghỉ ốm (`Sick`), Nghỉ thai sản (`Maternity`), và Thôi việc (`Resignation`). |
| 3 | ~~Xem cách tính lương và lương mỗi tháng~~ | **NGOÀI PHẠM VI** | Đã bỏ theo yêu cầu. |
| 4 | ~~In bảng lương theo tháng/năm~~ | **NGOÀI PHẠM VI** | Đã bỏ theo yêu cầu. |

---

### B.3. Yêu cầu 4 — Mô phỏng hệ thống CRM

#### B.3.a. Người quản lý

| # | Yêu cầu con | Kết luận | Bằng chứng / trạng thái |
| --- | --- | --- | --- |
| 1 | Thêm một khách hàng mới | **ĐẠT** | Module `customers` → `CreateCustomerAsync`. Form: họ tên, username, mật khẩu, CCCD, điện thoại, email, ngày sinh, sở thích, hạng thành viên. |
| 2 | Xóa khách hàng | **ĐẠT (soft delete)** | Module `customers` có `remove: true`; `DeleteAsync` đặt `Status = 'Inactive'`. |
| 3 | Khóa tài khoản khách hàng | **ĐẠT** | Sửa khách hàng đặt `Status = 'Banned'`; `AuthService` từ chối đăng nhập. |
| 4 | Tiếp nhận phản hồi của khách hàng | **ĐẠT** | Module `feedback`, cập nhật trạng thái (Pending/Processing/Resolved) và nội dung phản hồi cho khách. |
| 5 | Báo cáo về khách hàng, tỷ lệ độ tuổi, sở thích | **ĐẠT** | `ReportsPage.tsx` tab "Khách hàng": biểu đồ Hạng thành viên, Nhóm tuổi (Dưới 18 / 18–24 / 25–34 / 35+), Sở thích (tách tag). |
| 6 | Tạo bảng khảo sát và gửi đến khách hàng | **ĐẠT** | Module `surveys` → `CreateSurveyAsync`; `POST /surveys/{id}/publish` gửi đến danh sách khách hàng. Xuất hiện tức thì trên máy trạm WPF. |
| 7 | Thống kê kết quả khảo sát | **ĐẠT** | Modal chi tiết khảo sát hiển thị tỷ lệ phản hồi và đếm số lượt chọn từng phương án. |

#### B.3.b. Khách hàng (Desktop Client)

| # | Yêu cầu con | Kết luận | Bằng chứng / trạng thái |
| --- | --- | --- | --- |
| 1 | Đăng ký tài khoản khách hàng | **ĐẠT** | Màn hình khóa WPF có form đăng ký; backend `POST /auth/register`. |
| 2 | Đăng nhập | **ĐẠT** | Màn hình khóa WPF; backend `POST /auth/login`. Tài khoản trải nghiệm `gamer` / `Demo@123` được chuẩn hóa danh tính cố định `Nguyễn Văn An` trên cả DB SQL Server và offline fallback. |
| 3 | Chỉnh sửa thông tin | **ĐẠT** | WPF tab "Hồ sơ": họ tên, điện thoại, email, ngày sinh, sở thích + đổi mật khẩu. |
| 4 | Gửi phản hồi về sản phẩm | **ĐẠT** | WPF tab "Hỗ trợ" + nút "Gọi hỗ trợ tại máy"; backend `POST /feedback`. |
| 5 | Nhận được khảo sát và thực hiện | **ĐẠT** | WPF tab "Khảo sát": chọn khảo sát, trả lời câu hỏi và gửi câu trả lời. |

---

### B.4. Yêu cầu 5 — Mô phỏng hệ thống MRP

| # | Yêu cầu con | Kết luận | Bằng chứng / trạng thái |
| --- | --- | --- | --- |
| 1 | Tạo phiếu nhập kho tăng số lượng nguyên vật liệu | **ĐẠT** | Module `materials` trong nhóm menu "Sản xuất & Kho (MRP)" cho phép tạo và quản lý NVL, số lượng tồn kho, đơn vị tính và giá vốn. |
| 2 | Phê duyệt phiếu xuất nguyên vật liệu từ nhân viên | **ĐẠT** | Module `material-requests`: Nhân viên tạo yêu cầu xuất NVL; Quản lý duyệt phiếu (`Approved`), hệ thống tự động trừ tồn kho NVL. |
| 3 | Phê duyệt phiếu xuất thành phẩm | **ĐẠT** | Hỗ trợ yêu cầu sản xuất thành phẩm F&B: khi duyệt, hệ thống tự động tính lượng NVL tiêu hao theo BOM để trừ kho NVL và cộng thành phẩm vào menu `Products`. |
| 4 | Báo cáo tồn/xuất/lưu theo tháng, quý, năm cho vật liệu và sản phẩm | **ĐẠT** | Tab "Nhập · xuất · tồn" có bảng báo cáo riêng cho Kho NVL: theo dõi tồn thực tế, cảnh báo NVL dưới định mức an toàn (`Reorder Level`), và tổng giá trị vốn kho NVL; có bộ lọc preset Tháng/Quý/Năm. |
| 5 | Tạo công thức hình thành sản phẩm (BOM) | **ĐẠT** | Module `bom`: Thiết lập công thức định lượng tiêu hao nguyên vật liệu cho từng món ăn/uống F&B. |
| 6 | Đề nghị số lượng thành phẩm cần và gửi cho nhân viên | **ĐẠT** | Luồng `material-requests` hỗ trợ tạo đề xuất sản xuất F&B, ghi nhận người đề xuất và quản lý phê duyệt. |

---

### B.5. Đối chiếu 14 chức năng bắt buộc

| # | Chức năng | Kết luận | Trạng thái thực tế |
| --- | --- | --- | --- |
| 1 | Quản lý tài khoản & phân quyền | **ĐẠT** | 6 vai trò: Admin, Owner, Manager, Cashier, Staff, Customer. Token bảo mật, kiểm tra quyền mỗi request. |
| 2 | Quản lý phòng máy và máy trạm | **ĐẠT** | Sơ đồ máy dạng lưới, lọc khu vực/trạng thái/online; bật bảo trì, đổi giá, heartbeat 5 phút. |
| 3 | Quản lý thuê máy | **ĐẠT** | Bắt đầu phiên, chốt tiền theo giây, **hỗ trợ chuyển máy trạm** tức thời, tự động khóa khi hết tiền. |
| 4 | Quản lý nạp tiền và ví | **ĐẠT** | Nạp tại quầy, nạp qua mã VietQR động, toast thông báo realtime đẩy lên Web Admin khi có yêu cầu. |
| 5 | Quản lý sản phẩm ăn uống | **ĐẠT** | Module `products`, `categories`: ảnh món, giá bán, tồn kho, trạng thái đang bán. |
| 6 | Quản lý đơn hàng dịch vụ | **ĐẠT** | Gọi món từ máy trạm, quy trình phục vụ; hủy đơn hoàn tiền ví và hoàn tồn kho tự động. |
| 7 | Quản lý nhập hàng | **ĐẠT** | Module `inventory` (Import/Export), `suppliers`, và module `materials` quản lý giá vốn và tồn kho NVL. |
| 8 | Quản lý khách hàng và hội viên | **ĐẠT** | Hạng `Member/Silver/Gold/Diamond`, CCCD, ngày sinh, sở thích, số dư ví, khóa tài khoản. |
| 9 | Quản lý nhân viên và chức vụ | **ĐẠT** | Hồ sơ nhân viên, bộ phận, chức vụ, trình độ, thăng chức nhân sự (Trưởng nhóm / Quản lý). |
| 10 | Quản lý ca làm việc và chấm công | **ĐẠT** | Phân ca, chấm công thông minh tự động kiểm tra giờ ca để gắn tag Đi trễ / Về sớm / Đúng giờ. |
| 11 | Tiếp nhận phản hồi & hỗ trợ | **ĐẠT** | Khách gửi phản hồi / gọi hỗ trợ tại máy; quản lý xử lý và trả lời realtime. |
| 12 | Khảo sát ý kiến khách hàng | **ĐẠT** | Soạn câu hỏi, phát hành khảo sát, khách làm khảo sát trên máy trạm, thống kê kết quả. |
| 13 | Quản lý hoá đơn | **ĐẠT** | `InvoicesPage`: tra cứu, xem chi tiết và in hóa đơn nạp tiền, F&B, giờ chơi. |
| 14 | Báo cáo và thống kê hoạt động | **ĐẠT** | 4 tab báo cáo hoàn chỉnh (Doanh thu theo ngày/tháng, Khách hàng, Nhập xuất tồn NVL & F&B, Nhân sự & quỹ lương), có xuất CSV và in ấn. |

---

## Phần C — Tổng hợp & Kết luận

### C.1. Bảng tổng hợp mức độ hoàn thiện

| Nhóm yêu cầu | Trước audit | Sau Giai đoạn 1 & 2 | Sau Giai đoạn 3 & Tối ưu | Đánh giá |
| :--- | :---: | :---: | :---: | :--- |
| **Yêu cầu 2 — Giao diện Admin** | ~75% | ~90% | **100%** | Menu & Layout Shell tách biệt hoàn toàn giữa Admin và Quản lý; chuẩn hóa kết nối Backend port 5055; duy trì phiên đăng nhập bền vững qua F5 |
| **Yêu cầu 3 — HRM** | ~65% | 100% | **100%** | Đầy đủ ngày sinh, tháp tuổi, quỹ lương, thăng chức, chấm công ca, đơn thai sản; gỡ bỏ hoàn toàn logic tính lương theo chỉ đạo GV |
| **Yêu cầu 4 — CRM** | ~95% | 100% | **100%** | Khảo sát, hội viên, phản hồi, hồ sơ sở thích; chuẩn hóa danh tính trải nghiệm Nguyễn Văn An |
| **Yêu cầu 5 — MRP** | ~5% | 100% | **100%** | Kho NVL, BOM công thức, đề xuất & duyệt sản xuất trừ kho NVL và tăng thành phẩm F&B theo định mức |
| **14 chức năng bắt buộc** | ~80% | 100% | **100%** | Đạt trọn vẹn 14/14 chức năng cốt lõi |

---

### C.2. Checklist sẵn sàng nghiệm thu & bảo vệ đồ án

- [x] **Cơ sở dữ liệu (SQL Server LocalDB)**: Đầy đủ 28+ bảng, hỗ trợ tự động migration và seeding dữ liệu mẫu qua DatabaseBootstrapper.cs.
- [x] **Backend API (ASP.NET Core 10)**: Build thành công (0 error, 0 warning), lắng nghe chuẩn cổng http://127.0.0.1:5055, xác thực Bearer token bảo mật theo vai trò, SignalR realtime.
- [x] **Web Admin (React 19 + Ant Design 6)**: Build thành công (0 error), phân quyền Shell Admin vs Management, lưu phiên đăng nhập bền vững không mất khi F5, toast thông báo nạp tiền realtime.
- [x] **Desktop Client (WPF .NET 10)**: Build thành công (0 error, 0 warning), Kiosk client khóa màn hình, thanh toán VietQR động, hỗ trợ chuyển máy trạm và làm khảo sát trực tiếp.
- [x] **Tài liệu hướng dẫn & Kịch bản Demo**: File [README.md](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/README.md) cập nhật công nghệ mới nhất; file [docs/KichBanDemo.md](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/docs/KichBanDemo.md) sẵn sàng cho 4 kịch bản biểu diễn thực tế trước Hội đồng.
