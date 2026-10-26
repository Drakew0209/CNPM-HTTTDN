# PROMPT KIỂM THỬ TOÀN DIỆN (END-TO-END TEST PROMPT)
**Hệ thống Quản trị Cyber Cafe - CNPM-HTTTDN**

---

## 🎯 MỤC TIÊU
Kiểm thử xuyên suốt từ giao diện (UI) $\rightarrow$ nghiệp vụ (Business Logic) $\rightarrow$ tích hợp (Integration) $\rightarrow$ bảo mật (Security) $\rightarrow$ hiệu năng (Performance) toàn bộ 3 ứng dụng: **Web Admin**, **Backend API**, và **Desktop Client (WPF)**.

---

## 📋 PHẦN 1: KIỂM THỬ WEB ADMIN (React 19 + Ant Design 6)

### 1.1. Xác thực & Phân quyền (Authentication & Authorization)
- [ ] **TC-AUTH-01:** Đăng nhập thành công với 6 vai trò (`owner`, `admin`, `manager`, `cashier`, `staff`, `gamer`).
- [ ] **TC-AUTH-02:** Đăng nhập sai mật khẩu $\rightarrow$ hiển thị thông báo lỗi: *"Tên đăng nhập hoặc mật khẩu không đúng"*.
- [ ] **TC-AUTH-03:** Đăng nhập vai trò Customer (`gamer`) $\rightarrow$ bị chặn truy cập Web Admin, hiển thị thông báo hướng dẫn sử dụng Desktop Client.
- [ ] **TC-AUTH-04:** Đăng nhập vai trò Staff $\rightarrow$ tự động redirect về `/staff/home` (Cổng nhân viên).
- [ ] **TC-AUTH-05:** Đăng nhập vai trò Admin $\rightarrow$ hiển thị menu **"HỆ THỐNG"** (4 mục: *Tài khoản*, *Phân quyền*, *Game*, *Cấu hình*).
- [ ] **TC-AUTH-06:** Đăng nhập vai trò Manager/Cashier $\rightarrow$ hiển thị menu **"MANAGEMENT STUDIO"** (*Phòng máy*, *Thu ngân*, *Kho*, *HRM*, *CRM*, *Báo cáo*).
- [ ] **TC-AUTH-07:** F5/Refresh trang $\rightarrow$ duy trì phiên đăng nhập (`localStorage` + `sessionStorage`).
- [ ] **TC-AUTH-08:** Mở tab mới trong cùng trình duyệt $\rightarrow$ tự động nhận phiên đăng nhập (`sessionStorage`).
- [ ] **TC-AUTH-09:** Token hết hạn (24h) $\rightarrow$ tự động hủy phiên và redirect về trang login.
- [ ] **TC-AUTH-10:** Đăng xuất $\rightarrow$ xóa toàn bộ token, điều hướng về login, chặn truy cập `/workspace`.
- [ ] **TC-AUTH-11:** Thử truy cập trực tiếp URL `/system/*` bằng role Manager $\rightarrow$ trả về `403 Forbidden`.
- [ ] **TC-AUTH-12:** Thử truy cập trực tiếp URL `/management/*` bằng role Admin $\rightarrow$ trả về `403 Forbidden` (phân định vai trò có chủ đích).
- [ ] **TC-AUTH-13:** Brute-force đăng nhập $> 10$ lần/phút/IP $\rightarrow$ kích hoạt Rate Limiting (`429 Too Many Requests`).

### 1.2. Phòng máy & Quản lý phiên (Machines & Sessions)
- [ ] **TC-PC-01:** Sơ đồ lưới máy hiển thị chuẩn 3 trạng thái: 🟢 **Available**, 🔵 **InUse**, 🟠 **Maintenance**.
- [ ] **TC-PC-02:** Bộ lọc máy hoạt động chính xác theo: Khu vực (Standard/VIP), Trạng thái, và Tìm kiếm tên máy.
- [ ] **TC-PC-03:** Nhấp chọn máy $\rightarrow$ mở Drawer chi tiết: Tình trạng Heartbeat, Cước tạm tính, Danh mục lệnh điều khiển.
- [ ] **TC-PC-04:** Bật chế độ "Bảo trì" $\rightarrow$ máy đổi sang **Maintenance**, không cho phép mở phiên mới.
- [ ] **TC-PC-05:** Gửi lệnh "Khóa máy" $\rightarrow$ Client WPF nhận tín hiệu realtime và khóa màn hình tức thì.
- [ ] **TC-PC-06:** Gửi lệnh "Tắt máy / Khởi động lại" $\rightarrow$ Client WPF thực thi lệnh shutdown/restart hệ điều hành Windows.
- [ ] **TC-PC-07:** Chuyển máy (Transfer): Máy PC01 đang chơi $\rightarrow$ chuyển sang PC03 $\rightarrow$ PC01 về **Available**, PC03 thành **InUse**, bảo toàn số dư và thời gian đã chơi.
- [ ] **TC-PC-08:** Cơ chế Heartbeat 5 phút $\rightarrow$ tự động đánh dấu Offline (biểu tượng xám) nếu không nhận tín hiệu định kỳ.
- [ ] **TC-PC-09:** Thay đổi bảng giá giờ chơi realtime $\rightarrow$ máy đang hoạt động giữ nguyên giá cũ, phiên mở mới áp dụng giá mới.

### 1.3. Thu ngân & Ví khách hàng (Billing & Wallet)
- [ ] **TC-POS-01:** Danh sách khách hàng hiển thị đầy đủ: Hạng thành viên (Member/Silver/Gold/Diamond), Số dư ví, Trạng thái hoạt động.
- [ ] **TC-POS-02:** Nạp tiền mặt: Chọn khách hàng $\rightarrow$ Nhập số tiền $\rightarrow$ Ghi chú $\rightarrow$ Lưu $\rightarrow$ Số dư cộng dồn realtime.
- [ ] **TC-POS-03:** Nạp qua VietQR: Tạo mã QR động $\rightarrow$ Cú pháp nạp `NAP {username}` $\rightarrow$ Thu ngân duyệt $\rightarrow$ Số dư cập nhật ngay lập tức.
- [ ] **TC-POS-04:** Áp dụng combo khuyến mãi (Ví dụ: Nạp 50k tặng 15k, 100k tặng 35k) $\rightarrow$ Hệ thống ghi nhận đúng gốc + tiền thưởng.
- [ ] **TC-POS-05:** Hoàn tiền/Rút tiền: Kiểm tra số dư khả dụng (đã trừ cước phiên đang chạy) $\rightarrow$ Chặn giao dịch nếu yêu cầu vượt số dư khả dụng.
- [ ] **TC-POS-06:** Quản lý đơn F&B: Phân loại trạng thái `Pending` / `Preparing` / `Served` / `Cancelled`, hỗ trợ lọc theo máy và khách hàng.
- [ ] **TC-POS-07:** Luồng duyệt đơn: `Pending` $\rightarrow$ `Preparing` $\rightarrow$ `Served` (tự động xuất trừ kho nguyên vật liệu theo BOM).
- [ ] **TC-POS-08:** Hủy đơn hàng: Chặn hủy đơn đã `Served`; Cho phép hủy đơn `Pending` $\rightarrow$ Tự động hoàn tiền vào ví và hoàn lại tồn kho.
- [ ] **TC-POS-09:** Thông báo realtime qua SignalR (toast notification) mỗi khi phát sinh yêu cầu nạp tiền hoặc đơn F&B mới.

### 1.4. MRP & Kho nguyên vật liệu (MRP & Inventory)
- [ ] **TC-MRP-01:** Danh mục NVL: Mã NVL, Tên, Đơn vị tính, Tồn kho hiện tại, Ngưỡng cảnh báo đặt hàng (Reorder Level), Giá vốn, Nhà cung cấp (NCC).
- [ ] **TC-MRP-02:** Cảnh báo thiếu hàng: Tự động highlight đỏ 🔴 khi số lượng dưới Reorder Level.
- [ ] **TC-MRP-03:** Định mức nguyên vật liệu (BOM): Cấu hình định lượng chuẩn (VD: 1 Trà sữa = 20g trà + 30ml sữa đặc + 50g trân châu).
- [ ] **TC-MRP-04:** Phiếu xuất kho NVL: Nhân viên khởi tạo $\rightarrow$ Quản lý phê duyệt $\rightarrow$ Hệ thống trừ tồn kho NVL tương ứng.
- [ ] **TC-MRP-05:** Lệnh sản xuất F&B: Sau khi duyệt $\rightarrow$ Trừ NVL theo BOM đồng thời tăng số lượng món thành phẩm sẵn bán trong Products.
- [ ] **TC-MRP-06:** Phiếu nhập kho từ NCC: Chọn NCC, NVL, số lượng nhập $\rightarrow$ Tăng tồn kho và ghi nhận biến động giá vốn bình quân.
- [ ] **TC-MRP-07:** Báo cáo Xuất - Nhập - Tồn: Hỗ trợ bộ lọc Tháng/Quý/Năm, tính toán tổng giá trị vốn lưu kho.

### 1.5. HRM - Quản trị Nhân sự & Chấm công (HRM & Attendance)
- [ ] **TC-HRM-01:** Quản lý hồ sơ nhân viên: Thêm/Sửa/Xóa mềm (Soft delete), lưu Họ tên, Ngày sinh, Giới tính, SĐT, Email, Bằng cấp/Kỹ năng.
- [ ] **TC-HRM-02:** Thiết lập ca làm việc: Phân bổ Ca Sáng (06:00 - 14:00), Chiều (14:00 - 22:00), Đêm (22:00 - 06:00), gán nhân sự theo lịch tuần.
- [ ] **TC-HRM-03:** Chấm công vào (Check-in): So khớp giờ thực tế với lịch ca $\rightarrow$ Gán nhãn tự động *"Đúng giờ"* hoặc *"Đi trễ X phút"*.
- [ ] **TC-HRM-04:** Chấm công ra (Check-out): So khớp ca $\rightarrow$ Gán nhãn *"Đúng giờ"* hoặc *"Về sớm Y phút"*.
- [ ] **TC-HRM-05:** Chốt công can thiệp: Trường hợp nhân viên quên check-out, Quản lý/Admin có quyền chốt công thay thế kèm ghi chú.
- [ ] **TC-HRM-06:** Đơn xin nghỉ phép: Hỗ trợ 4 hình thức (`Annual`, `Sick`, `Maternity`, `Resignation`) $\rightarrow$ Quản lý duyệt $\rightarrow$ Đồng bộ lịch ca trạng thái `OnLeave`.
- [ ] **TC-HRM-07:** Điều chuyển / Thăng chức: Thay đổi chức vụ $\rightarrow$ Đồng bộ tức thời phân quyền (Role) trên tài khoản hệ thống.
- [ ] **TC-HRM-08:** Dashboard nhân sự: Báo cáo tháp tuổi lao động, trình độ chuyên môn, thâm niên và ước tính quỹ lương cơ bản.
- [ ] **TC-HRM-09:** Cổng nhân viên (`/staff/home`): Cho phép nhân sự tra cứu ca trực cá nhân, tự chấm công, nộp đơn phép và đổi mật khẩu.

### 1.6. CRM - Chăm sóc Khách hàng & Khảo sát (CRM & Survey)
- [ ] **TC-CRM-01:** Quản lý thông tin khách hàng: Số CCCD/Định danh, Ngày sinh, Sở thích cá nhân, Cấp bậc thành viên.
- [ ] **TC-CRM-02:** Khóa tài khoản (`Banned`) $\rightarrow$ Khách hàng bị chặn tức thời khi cố gắng đăng nhập tại Client WPF.
- [ ] **TC-CRM-03:** Xử lý yêu cầu hỗ trợ: Nhận thông tin gọi nhân viên/phản hồi từ Client qua realtime $\rightarrow$ Phản hồi trực tiếp từ Web Admin.
- [ ] **TC-CRM-04:** Quản lý khảo sát: Soạn thảo câu hỏi (đơn chọn/nhiều lựa chọn), ấn nút Phát hành $\rightarrow$ Đẩy đến các máy đang Active.
- [ ] **TC-CRM-05:** Thu thập phản hồi: Khách gửi câu trả lời tại Client $\rightarrow$ Web Admin tổng hợp biểu đồ tỷ lệ phần trăm theo thời gian thực.
- [ ] **TC-CRM-06:** Thống kê CRM: Phân khúc người dùng theo độ tuổi, nhóm sở thích (Tag Cloud) và tần suất sử dụng dịch vụ.

### 1.7. Báo cáo & Thống kê (Reports)
- [ ] **TC-REP-01:** Bộ lọc thời gian linh hoạt: Tùy chỉnh khoảng ngày hoặc chọn nhanh: *Hôm nay*, *Tháng này*, *Quý này*, *Năm nay*, *Tất cả*.
- [ ] **TC-REP-02:** Biểu đồ doanh thu: Tách biệt doanh thu Giờ chơi vs Dịch vụ F&B vs Khoản hoàn tiền; bảng đối soát tài chính theo tháng.
- [ ] **TC-REP-03:** Thống kê sản phẩm F&B: Bảng xếp hạng Top 10 món bán chạy nhất theo doanh số và giá trị lợi nhuận.
- [ ] **TC-REP-04:** Thống kê nhân sự: Phân bổ ca trực, biến động nhân sự, báo cáo đi muộn/về sớm và chi phí lương dự kiến.
- [ ] **TC-REP-05:** Báo cáo tồn kho NVL: Kiểm tra tồn thực tế, danh sách mặt hàng chạm đáy cảnh báo, tổng giá trị vốn tồn kho.
- [ ] **TC-REP-06:** Xuất dữ liệu: Hỗ trợ trích xuất file CSV chuẩn UTF-8 và chế độ In ấn (Print preview) cho mọi bảng biểu.

### 1.8. Quản trị hệ thống (System Admin)
- [ ] **TC-ADM-01:** Quản lý người dùng: Tạo mới, cập nhật thông tin, vô hiệu hóa (Soft ban), gán 1 trong 6 vai trò hệ thống.
- [ ] **TC-ADM-02:** Ma trận phân quyền (RBAC Matrix): 5 vai trò $\times$ $N$ modules với các cờ chi tiết (`Read`, `Write`, `Create`, `Delete`).
- [ ] **TC-ADM-03:** Danh mục Game Launcher: Quản trị thông tin trò chơi (đường dẫn `.exe`, banner, phân loại) $\rightarrow$ Tự động đồng bộ lên Client.
- [ ] **TC-ADM-04:** Cấu hình Combo nạp: Quản lý CRUD các gói ưu đãi nạp tiền (Mệnh giá nạp, Giá trị cộng thêm, Trạng thái kích hoạt).
- [ ] **TC-ADM-05:** Cấu hình trạm máy: Thay đổi mã định danh máy, phân khu phòng máy, đơn giá/giờ và gán trạng thái vận hành.

---

## 📋 PHẦN 2: KIỂM THỬ DESKTOP CLIENT (WPF .NET 10 - Kiosk Mode)

### 2.1. Màn hình khóa & Đăng nhập (Lock Screen & Login)
- [ ] **TC-CLI-01:** Khởi chạy ứng dụng $\rightarrow$ Chế độ Kiosk toàn màn hình (Luôn trên cùng - Topmost, bỏ viền, vô hiệu hóa phím Windows, `Alt+Tab`, `Ctrl+Shift+Esc`, chặn Task Manager).
- [ ] **TC-CLI-02:** Trình chiếu Carousel Banner (3 slide quảng cáo chuyển đổi mỗi 5.5s); nhấp chuột bất kỳ mở form đăng nhập.
- [ ] **TC-CLI-03:** Đăng nhập tài khoản gamer (vd: `gamer`/`Demo@123`) $\rightarrow$ Tự động mở phiên chơi nếu máy đang Available và số dư ví $> 0$.
- [ ] **TC-CLI-04:** Nhập sai mật khẩu $\rightarrow$ Thông báo cảnh báo: *"Tên đăng nhập hoặc mật khẩu không đúng"*.
- [ ] **TC-CLI-05:** Đăng ký hội viên mới tại máy: Kiểm tra hợp lệ Họ tên, SĐT, Email, Ngày sinh, Sở thích (giới hạn tối đa 500 ký tự).
- [ ] **TC-CLI-06:** Đăng nhập khi số dư bằng 0 $\rightarrow$ Chặn mở phiên, thông báo: *"Hết số dư, vui lòng nạp tại quầy"*.
- [ ] **TC-CLI-07:** Đăng nhập với tài khoản bị khóa $\rightarrow$ Chặn truy cập, thông báo: *"Tài khoản đã bị khóa, liên hệ nhân viên"*.
- [ ] **TC-CLI-08:** Phím nóng `F2` $\rightarrow$ Mở hộp thoại cấu hình Demo: Đổi Machine ID (PC01 - PC07), đổi URL API Server, chuyển đổi chế độ Windowed.
- [ ] **TC-CLI-09:** Windowed Mode $\rightarrow$ Cho phép kéo thả, co giãn kích thước, hỗ trợ chia đôi màn hình chạy 2 Client trên cùng 1 thiết bị.
- [ ] **TC-CLI-10:** Phím `Esc` $\rightarrow$ Đóng modal đăng nhập / Hỗ trợ thoát app nếu chưa phát sinh phiên sử dụng.
- [ ] **TC-CLI-11:** Tổ hợp phím khẩn cấp `Ctrl + Shift + F12` $\rightarrow$ Mở hộp thoại xác thực `Kiosk:EmergencyPassword` để tạm dừng Kiosk Mode phục vụ kỹ thuật.
- [ ] **TC-CLI-12:** Khi file config không định nghĩa `EmergencyPassword` $\rightarrow$ Vô hiệu hóa hoàn toàn cơ chế Emergency Bypass.

### 2.2. Phiên chơi & Đồng hồ tính tiền (Session & Billing)
- [ ] **TC-CLI-13:** Đồng hồ realtime: Thời gian đã dùng (`HH:mm:ss`), Thời gian còn lại dự kiến (tính theo công thức: $\text{Balance} - \text{EstimatedRental}$).
- [ ] **TC-CLI-14:** Cảnh báo sắp hết tiền: Khi số dư $\le 10.000$đ hoặc thời gian còn lại $\le 10$ phút $\rightarrow$ Đổi màu cảnh báo (vàng/đỏ) kèm âm thanh nhắc nhở.
- [ ] **TC-CLI-15:** Hết số dư ($\text{Remaining} \le 0$) $\rightarrow$ Tự động kết thúc phiên, khóa màn hình Kiosk và báo *"Hết số dư"*.
- [ ] **TC-CLI-16:** Nhận lệnh Chuyển máy từ Web Admin $\rightarrow$ WPF nhận tín hiệu SignalR: Thông báo *"Phiên chuyển sang PCxx, vui lòng di chuyển"*, đóng phiên tại máy cũ.
- [ ] **TC-CLI-17:** Khóa màn hình tạm thời: Khách nhập mật khẩu tạm $\rightarrow$ Khóa màn hình $\rightarrow$ Nhập đúng mật khẩu tạm để tiếp tục sử dụng.
- [ ] **TC-CLI-18:** Widget thu gọn (Compact Widget): Chuyển giao diện chính thành thanh mini ở góc màn hình, hiển thị giờ và tiền, có nút khôi phục cửa sổ lớn.

### 2.3. Đặt món F&B (Ordering)
- [ ] **TC-CLI-19:** Menu F&B: Phân loại theo Danh mục (Đồ uống, Thức ăn, Đồ ăn nhanh, Combo), hỗ trợ tìm kiếm nhanh theo tên món.
- [ ] **TC-CLI-20:** Thao tác giỏ hàng: Tăng/giảm số lượng, kiểm tra tồn kho tối đa 99 và không vượt quá số lượng tồn khả dụng trong kho.
- [ ] **TC-CLI-21:** Giới hạn giỏ hàng tối đa 30 món/lần đặt; tính toán chính xác tổng tiền dự kiến.
- [ ] **TC-CLI-22:** Gửi đơn đặt món $\rightarrow$ Truyền kèm `expectedUnitPrice`, `expectedTotal` $\rightarrow$ Backend kiểm tra khớp giá, tồn kho và số dư ví.
- [ ] **TC-CLI-23:** Giá thay đổi trong lúc đặt $\rightarrow$ Backend từ chối với mã `PRICE_CHANGED` $\rightarrow$ Client tự làm mới menu và báo người dùng.
- [ ] **TC-CLI-24:** Kiểm tra số dư ví: Nếu $(\text{Balance} - \text{IncurredRental}) < \text{TotalOrder}$ $\rightarrow$ Trả mã `INSUFFICIENT_BALANCE`.
- [ ] **TC-CLI-25:** Theo dõi trạng thái đơn realtime: Nhận cập nhật `Pending` $\rightarrow$ `Preparing` $\rightarrow$ `Served` thông qua badge màu trên tab Đơn hàng.
- [ ] **TC-CLI-26:** Hủy đơn `Pending` từ phía khách $\rightarrow$ Tự động hoàn tiền vào ví và cập nhật lại số lượng tồn kho.
- [ ] **TC-CLI-27:** Nút "Giục bếp": Gửi nhanh phản hồi realtime dạng `[Giục bếp] Đơn hàng #xxx` tới thu ngân.

### 2.4. Nạp tiền & VietQR (Topup & VietQR)
- [ ] **TC-CLI-28:** Tab Nạp tiền: Cung cấp tùy chọn Nạp tiền mặt tại quầy hoặc Chuyển khoản qua mã VietQR.
- [ ] **TC-CLI-29:** Ràng buộc hạn mức nạp: Validate giá trị nhập từ $1.000$đ đến $10.000.000$đ.
- [ ] **TC-CLI-30:** Chọn Combo nạp định sẵn (Đêm, Sinh Viên, Chiến Thần, Đại Gia) $\rightarrow$ Tự động điền số tiền và ghi chú tương ứng.
- [ ] **TC-CLI-31:** Tạo VietQR động: Tích hợp API `api.vietqr.io` sinh mã QR chứa số tiền, cú pháp chuẩn `NAP {username}`, tên chủ tài khoản thụ hưởng.
- [ ] **TC-CLI-32:** Tiện ích QR: Hỗ trợ sao chép cú pháp chuyển khoản, sao chép liên kết ảnh QR hoặc mở quét QR trên trình duyệt.
- [ ] **TC-CLI-33:** Yêu cầu nạp tiền mặt $\rightarrow$ Gửi yêu cầu với nội dung `[Tiền mặt tại quầy]` $\rightarrow$ Hiển thị trạng thái chờ thu ngân xác nhận.
- [ ] **TC-CLI-34:** Nhận thông báo SignalR realtime khi nạp thành công: *"Nạp tiền thành công! Số dư mới: xx.xxx đ"*.

### 2.5. Khảo sát & Phản hồi (Survey & Feedback)
- [ ] **TC-CLI-35:** Tab Khảo sát: Liệt kê các cuộc khảo sát trạng thái `Published` mà tài khoản hiện tại chưa tham gia.
- [ ] **TC-CLI-36:** Thực hiện khảo sát: Bắt buộc điền đủ các câu hỏi bắt buộc $\rightarrow$ Gửi thành công $\rightarrow$ Hiển thị thông điệp cảm ơn và ẩn khảo sát.
- [ ] **TC-CLI-37:** Hỗ trợ & Trợ giúp: Soạn thảo phản ánh (Tiêu đề + Nội dung) hoặc chọn nhanh mẫu "Gọi nhân viên hỗ trợ tại máy".
- [ ] **TC-CLI-38:** Phản hồi từ nhân viên: Hiển thị ngay trên danh sách hỗ trợ với nhãn "Đã phản hồi".

### 2.6. Game Launcher & Lucky Wheel
- [ ] **TC-CLI-39:** Tab Trò chơi: Hiển thị danh mục game đồng bộ từ server (VALORANT, League of Legends, CS2, PUBG...).
- [ ] **TC-CLI-40:** Khởi động VALORANT: Quét tự động client Riot qua Registry và toàn bộ ổ đĩa cố định $\rightarrow$ Khởi chạy an toàn qua Riot Client Services.
- [ ] **TC-CLI-41:** Khởi chạy game khác: Thực thi qua `Process.Start(executable)` $\rightarrow$ Chuyển sang cơ chế mô phỏng nếu không tìm thấy file thực thi trên máy demo.
- [ ] **TC-CLI-42:** Khi game bắt đầu chạy $\rightarrow$ Ứng dụng tự động thu về dạng Compact Widget để không chắn tầm nhìn người chơi.
- [ ] **TC-CLI-43:** Vòng quay may mắn: Mặc định cấp 1 lượt/ngày; nạp từ 50k hoặc tích lũy đủ giờ chơi $\rightarrow$ Tặng thêm lượt quay.
- [ ] **TC-CLI-44:** Trúng thưởng từ vòng quay: Phần thưởng tiền mặt/giờ chơi/voucher $\rightarrow$ Cộng trực tiếp vào tài khoản người chơi realtime.

### 2.7. Hồ sơ & Cài đặt (Profile & Settings)
- [ ] **TC-CLI-45:** Tra cứu và chỉnh sửa thông tin cá nhân: Họ tên, Số điện thoại, Email, Ngày sinh, Sở thích.
- [ ] **TC-CLI-46:** Đổi mật khẩu: Kiểm tra mật khẩu cũ (8 - 100 ký tự), mật khẩu mới khác mật khẩu cũ, khớp mật khẩu xác nhận.
- [ ] **TC-CLI-47:** Bật/Tắt hiệu ứng âm thanh mang phong cách Cyberpunk trong ứng dụng.
- [ ] **TC-CLI-48:** Xem lịch sử hoạt động: Lịch sử phiên chơi, Lịch sử nạp tiền, Lịch sử gọi đồ F&B, Lịch sử giao dịch ví và các khảo sát đã hoàn tất.

---

## 📋 PHẦN 3: KIỂM THỬ BACKEND API (ASP.NET Core 10 + SQL Server)

### 3.1. API Contract & Validation
- [ ] **TC-API-01:** Bảo vệ phân quyền: Mọi endpoint yêu cầu header `Authorization: Bearer <token>` (ngoại trừ `/auth/login`, `/auth/register`, `/health`).
- [ ] **TC-API-02:** Kiểm soát Parameterized Queries 100%: Tuyệt đối không nối chuỗi raw SQL trong mã nguồn xử lý DB.
- [ ] **TC-API-03:** Validation dữ liệu đầu vào: Range limits, Required fields, Enum match, độ dài chuỗi ký tự, định dạng chuẩn ISO ngày tháng.
- [ ] **TC-API-04:** Idempotency Key: `POST /orders`, `/topups` gửi cùng idempotency key nhưng khác payload body $\rightarrow$ Trả mã `409 IDEMPOTENCY_CONFLICT`.
- [ ] **TC-API-05:** Idempotency Key: Gửi trùng key và trùng payload $\rightarrow$ Trả về kết quả đã xử lý trước đó mà không thực thi lại (`200 OK Replay`).
- [ ] **TC-API-06:** Chuẩn hóa phân trang: Hỗ trợ đầy đủ tham số `page`, `pageSize`, `sort`, `order`, `search`, `fromDate`, `toDate`.
- [ ] **TC-API-07:** Cấu trúc Response chuẩn: Dạng `{ data: ..., meta: { total, page, pageSize } }` hoặc định dạng lỗi `{ error: { code, message, fields } }`.

### 3.2. Business Logic - Nghiệp vụ cốt lõi
- [ ] **TC-BIZ-01:** Mở phiên (Start Session): Khách có $\text{Balance} > 0$, máy trạng thái `Available`, không có phiên chạy dở $\rightarrow$ Khởi tạo thành công.
- [ ] **TC-BIZ-02:** Mở phiên khi $\text{Balance} \le 0$ $\rightarrow$ Báo lỗi `INSUFFICIENT_BALANCE`.
- [ ] **TC-BIZ-03:** Mở phiên khi máy đang `Maintenance` hoặc `InUse` $\rightarrow$ Báo lỗi `COMPUTER_UNAVAILABLE`.
- [ ] **TC-BIZ-04:** Mở phiên khi tài khoản đang đăng nhập ở một máy khác $\rightarrow$ Chặn với mã `ACTIVE_SESSION`.
- [ ] **TC-BIZ-05:** Kết thúc phiên (End Session): Tính cước chính xác theo giây (làm tròn trần Ceiling), khấu trừ Balance, ghi nhận transaction loại `Rental`.
- [ ] **TC-BIZ-06:** Kết thúc phiên khi tiền không đủ trả trọn vẹn: Khấu trừ toàn bộ số dư còn lại về mức $0$, không để số dư âm.
- [ ] **TC-BIZ-07:** Nghiệp vụ Chuyển máy (Transfer): Chỉ tài khoản Operator/Admin được thực thi; máy đích phải ở trạng thái `Available` và khác máy hiện tại.
- [ ] **TC-BIZ-08:** Logic chuyển phiên: Bảo lưu `Start_Time`, `Start_Balance`, số tiền đã dùng $\rightarrow$ Áp dụng giá mới theo `Applied_Hourly_Rate` của máy đích.
- [ ] **TC-BIZ-09:** Tạo đơn F&B: Ràng buộc phải có phiên active trên máy đặt hàng, kiểm tra đủ tồn kho, khóa giá niêm yết và số dư khả dụng.
- [ ] **TC-BIZ-10:** Hủy đơn F&B hợp lệ: Hoàn tiền ví khách hàng + Hoàn tồn kho NVL theo công thức BOM + Hoàn tồn kho món trong Products.
- [ ] **TC-BIZ-11:** Nạp tiền mặt: Trạng thái `Pending` $\rightarrow$ Thu ngân duyệt $\rightarrow$ Tăng Balance và tạo log transaction `TopUp`.
- [ ] **TC-BIZ-12:** Nạp VietQR: Khách tạo yêu cầu $\rightarrow$ Thu ngân nhận thông báo realtime $\rightarrow$ Phê duyệt $\rightarrow$ Kích hoạt cộng tiền.
- [ ] **TC-BIZ-13:** Rút tiền/Hoàn tiền: Ràng buộc số tiền rút phải $\le (\text{Balance} - \text{IncurredRental})$, ghi nhận giao dịch `Withdraw`.
- [ ] **TC-BIZ-14:** Xác thực Heartbeat: Khách hàng chỉ được gửi heartbeat cho chính máy thuộc phiên của mình $\rightarrow$ Trả về `403 Forbidden` nếu gửi máy khác.
- [ ] **TC-BIZ-15:** Tự động dọn phiên treo (CloseExpiredSessions): Máy mất tín hiệu quá 300s hoặc số dư về $\le 0$ $\rightarrow$ Backend tự động đóng phiên.

### 3.3. Transaction Safety & Đồng thời (Concurrency)
- [ ] **TC-TXN-01:** Áp dụng mức cô lập giao dịch `IsolationLevel.Serializable` kết hợp gợi ý khóa `UPDLOCK, HOLDLOCK` cho các nghiệp vụ tài chính và phiên.
- [ ] **TC-TXN-02:** Hai request cùng mở một máy trong cùng một thời điểm $\rightarrow$ Đúng 1 request thành công, request còn lại báo `COMPUTER_UNAVAILABLE` (xử lý xung đột Unique Index 2601/2627).
- [ ] **TC-TXN-03:** Hai request nạp tiền gửi đồng thời cùng một idempotency key $\rightarrow$ 1 request xử lý, 1 request nhận kết quả replay an toàn.
- [ ] **TC-TXN-04:** Vừa gửi đơn F&B vừa đóng phiên cùng lúc $\rightarrow$ Không xảy ra trừ tiền 2 lần (double-charge) và không trừ sai tồn kho.
- [ ] **TC-TXN-05:** Trigger `TR_Sync_Customer_Balance`: Mọi biến động `TopUp`, `Refund`, `Rental`, `FoodOrder`, `Withdraw` đều đồng bộ chuẩn xác vào số dư `Customers.Balance`.
- [ ] **TC-TXN-06:** Cột `Balance_Before` trong bảng Transactions: Chỉ kích hoạt ghi nhận cho giao dịch `TopUp` qua window function.

### 3.4. SignalR Realtime Hub
- [ ] **TC-SIG-01:** Thiết lập kết nối thành công tới `/hubs/internetcafe` truyền `access_token` qua Query String (giao thức WebSocket ưu tiên).
- [ ] **TC-SIG-02:** Sự kiện `SessionStarted` $\rightarrow$ Web Admin cập nhật ngay trạng thái máy sang `InUse` mà không cần tải lại trang.
- [ ] **TC-SIG-03:** Sự kiện `OrderCreated` $\rightarrow$ Giao diện thu ngân phát âm thanh/toast và thêm đơn mới vào bảng F&B.
- [ ] **TC-SIG-04:** Sự kiện `TopupRequested` $\rightarrow$ Thu ngân nhận thông báo có yêu cầu nạp tiền mới cần xử lý.
- [ ] **TC-SIG-05:** Sự kiện `BalanceChanged` $\rightarrow$ Client WPF cập nhật lập tức số dư hiển thị trên đồng hồ phiên.
- [ ] **TC-SIG-06:** Sự kiện `ComputerChanged` $\rightarrow$ Web Admin cập nhật realtime màu sắc trạng thái máy trên sơ đồ lưới.
- [ ] **TC-SIG-07:** Lệnh điều khiển `RemoteCommand` (Shutdown/Restart) $\rightarrow$ Client WPF nhận lệnh và thực thi đúng kịch bản.
- [ ] **TC-SIG-08:** Sự kiện `SurveyPublished` $\rightarrow$ Tab Khảo sát của Client WPF tự động tải khảo sát mới.

### 3.5. An toàn thông tin & Bảo mật (Security)
- [ ] **TC-SEC-01:** SQL Injection: Toàn bộ truy vấn đều dùng Parameterized Query $\rightarrow$ Vượt qua kiểm thử mã độc chèn SQL.
- [ ] **TC-SEC-02:** Phòng chống XSS: Web Admin không sử dụng `dangerouslySetInnerHTML`, kiểm tra an toàn render HTML.
- [ ] **TC-SEC-03:** Phòng chống IDOR: `GET /{resource}/{id}` triển khai Scoped Workspace Read Model $\rightarrow$ Truy cập trái quyền trả `404 Not Found` (không lộ `403`).
- [ ] **TC-SEC-04:** Rate Limiting: Endpoint nhạy cảm `/auth/login`, `/auth/register` giới hạn nghiêm ngặt 10 req/phút/IP (thuật toán Fixed Window).
- [ ] **TC-SEC-05:** Cấu hình Security Headers: Kiểm tra sự hiện diện của `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy`.
- [ ] **TC-SEC-06:** Chính sách CORS: Chỉ cho phép nguồn tin cậy (`127.0.0.1:5173`, `localhost:5173` và dải IP nội bộ RFC1918).
- [ ] **TC-SEC-07:** Băm mật khẩu: Mật khẩu được mã hóa an toàn bằng PBKDF2 (ASP.NET Core Identity), tuyệt đối không ghi log mật khẩu rõ.
- [ ] **TC-SEC-08:** Quản lý Token: Sử dụng CSPRNG 256-bit, cơ chế sliding expiration 24 giờ và hỗ trợ thu hồi token chủ động.
- [ ] **TC-SEC-09:** Kiosk Emergency Password: Tuyệt đối không hardcode trong mã nguồn, lấy duy nhất từ cấu hình `Kiosk:EmergencyPassword`.
- [ ] **TC-SEC-10:** Rà soát Git: File `.gitignore` ở root phải loại trừ triệt để `.env`, `.env.*`, `appsettings.*.json`.

---

## 📋 PHẦN 4: KIỂM THỬ TÍCH HỢP (INTEGRATION TESTS)

### 4.1. Kịch bản luồng End-to-End
- [ ] **TC-E2E-01 (Luồng Khách hàng):** Khách đăng ký hội viên $\rightarrow$ Đăng nhập tại Client WPF $\rightarrow$ Nạp tiền qua VietQR $\rightarrow$ Thu ngân duyệt $\rightarrow$ Mở máy bắt đầu chơi $\rightarrow$ Đặt món F&B $\rightarrow$ Thu ngân chuẩn bị và phục vụ món $\rightarrow$ Khách kết thúc phiên $\rightarrow$ Kiểm tra hóa đơn chi tiết.
- [ ] **TC-E2E-02 (Luồng Nhân sự):** Nhân viên thực hiện Check-in ca $\rightarrow$ Phục vụ ca $\rightarrow$ Gửi đơn xin nghỉ phép $\rightarrow$ Quản lý duyệt đơn $\rightarrow$ Nhân viên về và được chốt công hộ $\rightarrow$ Xuất báo cáo tổng hợp công.
- [ ] **TC-E2E-03 (Luồng Quản trị Kho F&B):** Quản lý khai báo NVL mới $\rightarrow$ Thiết lập định mức BOM cho sản phẩm $\rightarrow$ Nhân viên đề xuất xuất kho sản xuất $\rightarrow$ Quản lý duyệt $\rightarrow$ Tự động trừ tồn NVL và tăng tồn món bán trên Menu.
- [ ] **TC-E2E-04 (Luồng Đánh giá & Khảo sát):** Quản trị viên thiết lập bảng khảo sát mới $\rightarrow$ Phát hành $\rightarrow$ Khách hàng thực hiện khảo sát tại Client WPF $\rightarrow$ Web Admin tổng hợp dữ liệu thống kê trực quan.
- [ ] **TC-E2E-05 (Kiểm thử mô phỏng đa máy):** Thực thi kịch bản `chay-demo-2-may.bat` $\rightarrow$ Khởi chạy đồng thời PC01 + PC02 dạng Windowed và Web Admin chia đôi màn hình.
- [ ] **TC-E2E-06 (Kiểm thử kết nối mạng LAN):** Laptop A chạy Server (`chay-toan-bo.bat`) $\rightarrow$ Laptop B mở Client (nhấn `F2` trỏ URL về `http://<IP_Laptop_A>:5055`) $\rightarrow$ Vận hành thông suốt.

### 4.2. Chế độ Ngoại tuyến & Đóng gói (Offline/Standalone Mode)
- [ ] **TC-OFF-01:** Client WPF khi mất kết nối mạng hoặc không có backend $\rightarrow$ Tự chuyển sang chế độ Standalone Demo (`SmartCafeApi`).
- [ ] **TC-OFF-02:** Chế độ Standalone: Đăng nhập bằng tài khoản mẫu `gamer`/`Demo@123` $\rightarrow$ Tải đầy đủ dữ liệu mẫu (Danh mục món, Combo, Game, Lịch sử phiên).
- [ ] **TC-OFF-03:** Chế độ Standalone: Đặt món, Nạp tiền, Làm khảo sát, Mở game $\rightarrow$ Các chức năng mô phỏng hoạt động mượt mà.
- [ ] **TC-OFF-04:** Kiểm tra gói nén Portable: Chạy `dong-goi-portable.bat` $\rightarrow$ Giải nén và chạy thành công trên máy tính Windows sạch (không cài sẵn .NET SDK/Runtime).
- [ ] **TC-OFF-05:** Cập nhật ClickOnce: Chạy `dong-goi-clickonce.bat` $\rightarrow$ Xuất bản lên GitHub Pages $\rightarrow$ Client nhận thông báo và tự động cập nhật phiên bản mới.

---

## 📋 PHẦN 5: KIỂM THỬ HỒI QUY (REGRESSION) - TEST SUITES TỰ ĐỘNG

### 5.1. Playwright E2E Web Tests (34 Test Cases)
```bash
cd InternetCafe/apps/admin-web
npm run test:e2e
# Hoặc thực thi qua batch script:
.\chay-playwright-tests.bat
```
*Kỳ vọng: **34/34 PASS** (Thời gian chạy xấp xỉ 1.5 - 2 phút).*

| Tập tin Test | Số lượng TC | Phân hệ nghiệp vụ |
| :--- | :---: | :--- |
| `app-client-simulation.spec.ts` | 4 | Tích hợp Client App Simulation + Web Admin |
| `auth.spec.ts` | 5 | Xác thực tài khoản & Phân quyền bảo mật |
| `machines.spec.ts` | 3 | Quản lý đội máy tính phòng net |
| `pos-billing.spec.ts` | 3 | Thu ngân, Nạp ví, Đơn hàng F&B |
| `mrp-inventory.spec.ts` | 5 | Quản trị Kho NVL & Định mức BOM |
| `hrm-staff.spec.ts` | 6 | Quản lý nhân sự & Cổng nhân viên |
| `crm-feedback.spec.ts` | 2 | Khảo sát khách hàng & Tiếp nhận phản hồi |
| `reports.spec.ts` | 2 | Báo cáo tài chính, Hóa đơn doanh thu |
| `system-admin.spec.ts` | 4 | Quản trị hệ thống, Tài khoản & Ma trận RBAC |

### 5.2. Postman / Newman API Tests (115 Test Cases)
```bash
npx newman run testing/postman/InternetCafe_API.postman_collection.json \
  -e testing/postman/InternetCafe_Local.postman_environment.json \
  -r htmlextra,cli
# Hoặc thực thi script tự động:
.\chay-postman-tests.bat
```
*Kỳ vọng: **115/115 PASS**.*

---

## 📋 PHẦN 6: KIỂM THỬ HIỆU NĂNG & TẢI (PERFORMANCE & STRESS)

- [ ] **TC-PERF-01:** Thời gian phản hồi endpoint Backend `/health` $< 50\text{ ms}$.
- [ ] **TC-PERF-02:** Thời gian tải toàn bộ dữ liệu khởi động `/workspace` $< 300\text{ ms}$.
- [ ] **TC-PERF-03:** 50 Client WPF gửi heartbeat đồng thời mỗi 5 phút $\rightarrow$ Mức sử dụng CPU/RAM của Backend giữ ổn định, không có hiện tượng rò rỉ bộ nhớ.
- [ ] **TC-PERF-04:** Bắn tải 100 requests/giây vào `/auth/login` $\rightarrow$ Cơ chế Rate Limiting kích hoạt chuẩn xác, ứng dụng không bị crash.
- [ ] **TC-PERF-05:** SignalR kết nối đồng thời 100 clients broadcast sự kiện $\rightarrow$ Độ trễ truyền tin đến toàn bộ máy $< 200\text{ ms}$.
- [ ] **TC-PERF-06:** Cơ sở dữ liệu chứa $10.000$ phiên sử dụng và $50.000$ giao dịch $\rightarrow$ Tốc độ truy vấn báo cáo tài chính $< 1\text{ giây}$.
- [ ] **TC-PERF-07:** Tốc độ khởi chạy ứng dụng Client WPF: Khởi động lạnh (Cold start) $< 3\text{ giây}$, Khởi động ấm (Warm start) $< 1\text{ giây}$.
- [ ] **TC-PERF-08:** Web Admin First Contentful Paint (FCP) đạt dưới $2\text{ giây}$ trên trình duyệt tiêu chuẩn.

---

## 📋 PHẦN 7: KIỂM THỬ TRƯỜNG HỢP BIÊN & ĐẶC BIỆT (EDGE CASES)

- [ ] **TC-EDGE-01:** Ngày nhuận (29/02): Kiểm tra tính chính xác của Ngày sinh (DOB), xếp lịch ca làm việc và tính chu kỳ công tháng 2.
- [ ] **TC-EDGE-02:** Ca làm việc vắt qua nửa đêm (22:00 hôm trước $\rightarrow$ 06:00 hôm sau): Check-in lúc 23:00, Check-out lúc 05:00 tính đúng thời lượng làm việc.
- [ ] **TC-EDGE-03:** Xử lý lệch múi giờ: Chuyển đổi chuẩn hóa UTC $\leftrightarrow$ Giờ địa phương Việt Nam (UTC+7) trên các mốc thời gian phiên, giao dịch và báo cáo.
- [ ] **TC-EDGE-04:** Dữ liệu có dấu tiếng Việt (Họ tên: *Nguyễn Văn An*, món: *Trà sữa trân châu hoàng kim*): Không lỗi font, hiển thị tốt trên UI và lưu UTF-8 trong DB.
- [ ] **TC-EDGE-05:** Biên độ số tiền lớn: Giao dịch nạp $10.000.000$đ, doanh thu tích lũy đạt $999.999.999$đ $\rightarrow$ Kiểu dữ liệu `decimal(18,2)` hoạt động chính xác, không tràn số.
- [ ] **TC-EDGE-06:** Xung đột định danh: Khi đổi tên máy trạm bằng phím `F2` trùng với một mã máy đang hoạt động khác $\rightarrow$ Server từ chối và thông báo lỗi xung đột.
- [ ] **TC-EDGE-07:** Mất kết nối mạng đột ngột: Sau 30s không nhận heartbeat, Server chuyển máy sang Offline; khi có mạng lại, Client tự động kết nối lại (`auto-reconnect`).
- [ ] **TC-EDGE-08:** Máy chủ khởi động lại: Client WPF tự động bắt lại kết nối SignalR và đồng bộ lại trạng thái phiên hiện hành mà không bắt người dùng đăng nhập lại.
- [ ] **TC-EDGE-09:** Cạnh tranh đặt hàng: 2 khách hàng cùng bấm đặt món khi tồn kho chỉ còn đúng 1 sản phẩm $\rightarrow$ Đúng 1 đơn thành công, đơn còn lại nhận thông báo `OUT_OF_STOCK`.
- [ ] **TC-EDGE-10:** Xóa mềm dữ liệu: Khi xóa Khách hàng / Nhân viên / Tài khoản $\rightarrow$ Chỉ đổi cờ trạng thái (`Inactive`/`Banned`), giữ toàn vẹn quan hệ khóa ngoại và lịch sử giao dịch.

---

## 📋 PHẦN 8: CHECKLIST TRIỂN KHAI & BẢO VỆ DEMO (DEPLOYMENT READINESS)

- [ ] **DEP-01:** File `chay-toan-bo.bat` $\rightarrow$ 1-Click tự động hóa: SQL LocalDB + Backend API (Port 5055) + Web Admin (Port 5173) + Client Desktop (PC01).
- [ ] **DEP-02:** File `dung-toan-bo.bat` $\rightarrow$ Dừng sạch toàn bộ tiến trình liên quan, giải phóng port 5055 và 5173.
- [ ] **DEP-03:** File `chay-demo-2-may.bat` $\rightarrow$ Tự động bố cục màn hình: Web Admin nửa trái, PC01 và PC02 dạng Windowed nửa phải.
- [ ] **DEP-04:** Bộ file script khởi chạy độc lập: `chay-backend.bat`, `chay-web-admin.bat`, `chay-desktop-client.bat` hoạt động ổn định.
- [ ] **DEP-05:** Script đóng gói `dong-goi-portable.bat` $\rightarrow$ Tạo file `InternetCafe-Client-Portable.zip` cấu hình Win-x64 Self-contained.
- [ ] **DEP-06:** Script xuất bản `dong-goi-clickonce.bat` $\rightarrow$ Đẩy bản build ClickOnce lên GitHub Pages và xác nhận link cài đặt online.
- [ ] **DEP-07:** File `README.md` cập nhật đầy đủ: Thông tin công nghệ (.NET 10, React 19, Ant Design 6), danh sách tài khoản demo mẫu và hướng dẫn cài đặt.
- [ ] **DEP-08:** Tài liệu `docs/KichBanDemo.md`: Đầy đủ 4 kịch bản trình diễn chuyên sâu (MRP, HRM, CRM, Báo cáo) kèm 10 bước chuẩn khi bảo vệ đồ án.
- [ ] **DEP-09:** File kịch bản CSDL `Database/InternetCafe Final.sql`: Chứa đầy đủ 28 bảng dữ liệu, chạy mượt mà trên môi trường SQL Server LocalDB mới.
- [ ] **DEP-10:** Rà soát tệp nhạy cảm: Bảo đảm các file cấu hình mật khẩu nội bộ không bị commit lên kho mã nguồn.

---

## 📊 BÁO CÁO KẾT QUẢ MONG ĐỢI

| Hạng mục kiểm thử | Tổng số Test Cases | Tỷ lệ Đạt (Pass Rate) kỳ vọng | Thời lượng ước tính |
| :--- | :---: | :---: | :---: |
| **Playwright E2E** | 34 | 100% (34/34) | ~ 2 phút |
| **Postman API** | 115 | 100% (115/115) | ~ 3 phút |
| **Manual UI/UX** | ~ 200 | 100% Critical & High | ~ 4 - 6 giờ |
| **Integration E2E** | 6 | 100% | ~ 30 phút |
| **Performance** | 8 | Đạt ngưỡng quy định | ~ 15 phút |
| **Edge Cases** | 10 | 100% | ~ 30 phút |
| **Deployment / Demo**| 10 | 100% | ~ 15 phút |

---

## 🛠️ CÁC LỆNH VẬN HÀNH VÀ THỰC THI KIỂM THỬ

```bash
# 1. Khởi động toàn bộ môi trường hệ thống
.\chay-toan-bo.bat

# 2. Chạy Playwright E2E Tests (Web Admin + Client Simulation)
.\chay-playwright-tests.bat
# Xem báo cáo HTML chi tiết sau khi chạy xong:
cd InternetCafe/apps/admin-web && npx playwright show-report

# 3. Chạy kiểm thử tự động API bằng Newman
.\chay-postman-tests.bat
# Hoặc chạy lệnh Newman trực tiếp xuất báo cáo trực quan:
npx newman run testing/postman/InternetCafe_API.postman_collection.json \
  -e testing/postman/InternetCafe_Local.postman_environment.json \
  -r htmlextra,cli

# 4. Biên dịch và kiểm thử Backend
cd InternetCafe/apps/backend/InternetCafe.Backend
dotnet build          # Đạt: 0 errors
dotnet test           # Chạy unit tests nếu có

# 5. Biên dịch Desktop Client WPF
cd InternetCafe/apps/desktop-client
dotnet build          # Đạt: 0 errors, 0 warnings

# 6. Kiểm tra mã nguồn Web Admin
cd InternetCafe/apps/admin-web
npm run typecheck     # TypeScript: 0 errors
npm run build         # Vite production build thành công
npm run test          # Vitest unit tests

# 7. Khởi chạy kịch bản Demo 2 máy trên 1 màn hình
.\chay-demo-2-may.bat

# 8. Dừng và giải phóng toàn bộ tài nguyên
.\dung-toan-bo.bat
```

---

## 📝 MẪU BÁO CÁO LỖI (BUG REPORT TEMPLATE)

```markdown
**Bug ID:** TC-XXX-##
**Module:** [Web Admin / Desktop Client / Backend API / Database]
**Severity:** [Blocker / Critical / Major / Minor / Trivial]
**Priority:** [P1-Immediate / P2-High / P3-Medium / P4-Low]
**Môi trường kiểm thử:** Windows 11, Chrome 120+, SQL LocalDB, .NET 10, React 19

**Các bước tái hiện (Steps to Reproduce):**
1. ...
2. ...
3. ...

**Kết quả thực tế (Actual Result):**
- ...

**Kết quả mong đợi (Expected Result):**
- ...

**Bằng chứng (Evidence):**
- Đính kèm Screenshot / Video / Console Log / Request & Response payload

**Phân tích nguyên nhân gốc rễ (Root Cause Analysis):**
- ...

**Đề xuất hướng xử lý (Suggested Fix):**
- ...
```

---

## ✅ TIÊU CHÍ NGHIỆM THU ĐẠT (ACCEPTANCE CRITERIA)

Dự án được đánh giá là **PASSED** khi đáp ứng đủ các điều kiện sau:
1. ✅ **34/34** kịch bản Playwright E2E đạt trạng thái **PASS**.
2. ✅ **115/115** kịch bản Postman API đạt trạng thái **PASS**.
3. ✅ **0 Build Errors** trên toàn bộ các thành phần: Backend API, Web Admin và Desktop Client.
4. ✅ Kịch bản `chay-demo-2-may.bat` chạy mượt mà, phân chia layout rõ ràng.
5. ✅ Kịch bản demo mạng LAN giữa 2 laptop kết nối thông suốt.
6. ✅ Gói nén Portable ZIP chạy độc lập thành công trên máy tính chưa cài .NET.
7. ✅ Cơ chế ClickOnce tự động cập nhật qua GitHub Pages hoạt động chính xác.
8. ✅ Không tồn tại lỗ hổng bảo mật cấp độ Critical/High.
9. ✅ Hoàn thành đầy đủ **14/14 chức năng cốt lõi** theo tài liệu `DoiChieuYeuCau.md`.
10. ✅ Bộ tài liệu kỹ thuật (`README.md`, `KichBanDemo.md`, `DoiChieuYeuCau.md`) được cập nhật đồng bộ với mã nguồn hiện tại.