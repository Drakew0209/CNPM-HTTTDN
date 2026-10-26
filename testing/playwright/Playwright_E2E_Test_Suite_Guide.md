# Hướng Dẫn Kiểm Thử Tự Động Giao Diện E2E Bằng Playwright
## InternetCafe Management System (Web Admin + Desktop Client App)

Tài liệu này đặc tả toàn bộ bộ kiểm thử tự động hóa đầu-cuối (**Playwright End-to-End Test Suite**) bao phủ 100% các phân hệ nghiệp vụ của **Cổng Web Quản trị (Admin Web)** và luồng tương tác thực tế với **Ứng dụng Máy trạm Khách hàng (Desktop Client App)**.

---

## 📊 Tổng Quan Bộ Kiểm Thử (Test Suite Summary)

* **Công nghệ sử dụng**: [Playwright](https://playwright.dev/) (`@playwright/test ^1.58.0`), Node.js, Chromium Headless.
* **Tổng số kịch bản E2E**: **34 Test Cases**.
* **Tỷ lệ Pass**: **100% (34/34 Passed)**.
* **Thời gian thực thi trung bình**: ~1.5 - 2 phút.
* **Báo cáo trực quan**: Sinh tự động báo cáo HTML kèm video/trace tại `InternetCafe/apps/admin-web/playwright-report/index.html`.

---

## 📁 Cấu Trúc Các Tệp Kiểm Thử (`InternetCafe/apps/admin-web/e2e/`)

| Tệp kiểm thử | Số Test Cases | Phân hệ bao phủ |
| :--- | :---: | :--- |
| **`app-client-simulation.spec.ts`** | 4 | **Tích hợp Máy trạm Client App + Web Admin**: Tải ClickOnce, Heartbeat trực tuyến, Chu kỳ mở/đóng phiên chơi khách hàng, Đặt món F&B tại máy trạm và đồng bộ Web Admin theo thời gian thực. |
| **`auth.spec.ts`** | 5 | **Xác thực & Bảo mật Phiên**: Giao diện login, Thử nhanh vai trò, Đăng nhập sai mật khẩu, Chặn khách hàng vào Web Admin, Đăng nhập Quản lý, Đăng xuất an toàn. |
| **`machines.spec.ts`** | 3 | **Quản lý Đội Máy trạm (Fleet)**: Sơ đồ lưới máy, Thẻ trạng thái (Sẵn sàng/Đang chơi/Bảo trì), Tìm kiếm/Lọc máy, Drawer chi tiết nhịp tim & cước tạm tính. |
| **`pos-billing.spec.ts`** | 3 | **Thu ngân & Ví & Dịch vụ**: Danh sách khách hàng & số dư ví, Danh sách đơn dịch vụ F&B, Menu thực đơn món ăn/uống. |
| **`mrp-inventory.spec.ts`** | 5 | **Quản lý Kho & Định mức NVL (MRP & BOM)**: Danh mục Nguyên vật liệu thô (mã, đơn vị, tồn kho an toàn), Bảng định mức công thức món ăn (BOM), Yêu cầu xuất NVL & Sản xuất, Nhật ký nhập/xuất kho, Danh bạ nhà cung cấp. |
| **`hrm-staff.spec.ts`** | 6 | **Quản trị Nhân sự (HRM) & Cổng Nhân viên**: Hồ sơ nhân sự (bộ phận, lương, chức vụ), Khung giờ ca làm việc, Phân lịch ca trực, Bảng chấm công vào/ra, Duyệt đơn nghỉ phép, Giao diện Cổng nhân viên (`/staff/home`). |
| **`crm-feedback.spec.ts`** | 2 | **Chăm sóc Khách hàng (CRM)**: Danh sách khảo sát ý kiến khách hàng, Hộp thư tiếp nhận phản hồi & yêu cầu hỗ trợ tại máy. |
| **`reports.spec.ts`** | 2 | **Báo cáo Tài chính & Hóa đơn**: Báo cáo doanh thu & biểu đồ phân tích (Giờ chơi vs F&B), Tra cứu lịch sử hóa đơn & in phiếu biên nhận. |
| **`system-admin.spec.ts`** | 4 | **Quản trị Hệ thống & Phân quyền RBAC**: Quản lý tài khoản người dùng, Ma trận quyền 5 vai trò, Quản lý kho game (.exe & banner), Kiểm tra bảo mật chặn 403 khi tài khoản không đủ quyền. |

---

## 🚀 Hướng Dẫn Thực Thi Kiểm Thử

### Cách 1: Chạy bằng Script 1-Click (Khuyên dùng)
Nhấp đúp chuột vào tệp script tại thư mục gốc:
👉 **`chay-playwright-tests.bat`**

> Script sẽ tự động:
> 1. Kiểm tra Backend API đang hoạt động (`http://127.0.0.1:5055/health`).
> 2. Kích hoạt Vite Dev Server và thực thi toàn bộ 34 test cases Playwright.
> 3. Xuất kết quả chi tiết từng test case ra màn hình terminal.

### Cách 2: Chạy bằng dòng lệnh (Terminal)
```powershell
cd InternetCafe\apps\admin-web
npm run test:e2e
```

### Cách 3: Mở Báo Cáo HTML Trực Quan (Interactive HTML Report)
Sau khi chạy xong test, xem biểu đồ, thời gian chạy và ảnh chụp màn hình bằng lệnh:
```powershell
cd InternetCafe\apps\admin-web
npx playwright show-report
```

---

## 🎯 Chi Tiết 34 Test Cases E2E Đã Được Hiện Thực

### Phân hệ 10: Tích hợp Máy trạm Client App & Web Admin (`app-client-simulation.spec.ts`)
* `TC-APP-01`: Mở trang phân phối cài đặt ClickOnce (`/client/index.html`), kiểm tra nút cài đặt ứng dụng máy trạm.
* `TC-APP-02`: Gửi nhịp tim Telemetry từ máy trạm giả lập PC01 -> Web Admin tự động nhận diện và cập nhật đèn xanh **Online**.
* `TC-APP-03`: Vòng đời hội viên: Khách hàng đăng nhập mở máy tại PC01 -> Web Admin cập nhật ngay lập tức sang màu xanh dương **Đang sử dụng (InUse)** hiển thị tên khách hàng -> Kết thúc phiên chơi trả máy về **Sẵn sàng (Available)**.
* `TC-APP-04`: Khách hàng ngồi tại máy trạm gọi món F&B qua API -> Web Admin cập nhật đơn hàng mới vào hàng đợi chuẩn bị món.

### Phân hệ 01: Xác thực & Quản lý Phiên (`auth.spec.ts`)
* `TC-AUTH-01`: Kiểm tra giao diện trang đăng nhập, thương hiệu InternetCafe, form đăng nhập và 5 nút thử nhanh vai trò.
* `TC-AUTH-02`: Đăng nhập với mật khẩu sai -> Hiển thị thông báo lỗi 'Tên đăng nhập hoặc mật khẩu không đúng.'.
* `TC-AUTH-03`: Đăng nhập với tài khoản khách hàng `gamer` -> Bị từ chối với hướng dẫn khách sử dụng ứng dụng Desktop Client.
* `TC-AUTH-04`: Đăng nhập vai trò Quản lý -> Điều hướng thành công đến `/management/overview`.
* `TC-AUTH-05`: Quy trình đăng xuất -> Hiển thị hộp thoại xác nhận modal và quay về `/login`.

### Phân hệ 02: Quản lý Phòng máy (`machines.spec.ts`)
* `TC-PC-01`: Vào menu Phòng máy, kiểm tra sơ đồ lưới máy trạm và các thẻ trạng thái (Available, InUse, Maintenance).
* `TC-PC-02`: Tìm kiếm và lọc máy trạm theo từ khóa.
* `TC-PC-03`: Nhấp vào máy trạm để mở Drawer chi tiết với thông số cước, thời gian đã chơi và lệnh điều khiển từ xa.

### Phân hệ 03: Thu ngân & Ví khách hàng (`pos-billing.spec.ts`)
* `TC-POS-01`: Tra cứu danh sách khách hàng, kiểm tra hạng thành viên và số dư ví.
* `TC-POS-02`: Xem danh sách đơn dịch vụ F&B và trạng thái chế biến.
* `TC-POS-03`: Xem thực đơn bán hàng và kiểm tra các món đồ ăn, thức uống đang kinh doanh.

### Phân hệ 05: Quản lý Kho & Định mức NVL (`mrp-inventory.spec.ts`)
* `TC-MRP-01`: Tra cứu kho Nguyên vật liệu (mã NVL, đơn vị tính, tồn kho hiện tại, định mức tồn an toàn).
* `TC-MRP-02`: Tra cứu bảng Định mức công thức sản phẩm (BOM) liên kết món F&B với nguyên vật liệu cấu thành.
* `TC-MRP-03`: Xem danh sách yêu cầu xuất kho NVL và lệnh sản xuất món thành phẩm.
* `TC-MRP-04`: Xem sổ theo dõi biến động nhập - xuất kho hàng hóa.
* `TC-MRP-05`: Xem danh bạ đối tác nhà cung cấp nguyên vật liệu.

### Phân hệ 06: Quản trị Nhân sự & Chấm công (`hrm-staff.spec.ts`)
* `TC-HRM-01`: Quản lý xem danh sách hồ sơ nhân sự, chức vụ, bộ phận và lương cơ bản.
* `TC-HRM-02`: Xem danh mục các ca làm việc trong ngày (Ca sáng, Ca chiều, Ca tối).
* `TC-HRM-03`: Xem bảng phân lịch ca trực của nhân viên theo ngày.
* `TC-HRM-04`: Xem bảng nhật ký chấm công vào ca và ra ca.
* `TC-HRM-05`: Xem hàng đợi duyệt đơn xin nghỉ phép, nghỉ ốm của nhân viên.
* `TC-HRM-06`: Nhân viên đăng nhập vào Cổng cá nhân (`/staff/home`), xem lịch trực ca và giao diện chấm công.

### Phân hệ 07: Chăm sóc Khách hàng CRM (`crm-feedback.spec.ts`)
* `TC-CRM-01`: Quản lý danh sách các cuộc khảo sát ý kiến khách hàng.
* `TC-CRM-02`: Tiếp nhận và xử lý phiếu phản hồi & gọi hỗ trợ từ máy trạm.

### Phân hệ 04: Báo cáo & Hóa đơn (`reports.spec.ts`)
* `TC-REP-01`: Mở báo cáo tài chính, kiểm tra các thẻ chỉ số KPI doanh thu và biểu đồ phân tích.
* `TC-REP-02`: Tra cứu lịch sử hóa đơn dịch vụ và xem chi tiết phiếu thanh toán.

### Phân hệ 08: Quản trị Hệ thống & Phân quyền (`system-admin.spec.ts`)
* `TC-ADM-01`: Admin đăng nhập và quản lý danh sách tài khoản người dùng hệ thống (`/system/accounts`).
* `TC-ADM-02`: Xem ma trận phân quyền 5 vai trò (`/system/permissions`).
* `TC-ADM-03`: Quản lý danh mục Game phòng máy (ảnh poster, đường dẫn file thực thi .exe).
* `TC-ADM-04`: Kiểm tra bảo mật RBAC: Nhân viên thu ngân truy cập trái phép vào trang quản trị tài khoản bị chặn bằng mã lỗi 403 Forbidden.
