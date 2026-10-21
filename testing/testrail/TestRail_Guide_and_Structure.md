# Hướng Dẫn Quản Trị Kiểm Thử Thủ Công (Manual Testing) Với TestRail

Tài liệu này hướng dẫn chi tiết cách tổ chức, thiết kế và quản lý các ca kiểm thử (Test Cases) trên **TestRail** cho hệ sinh thái **InternetCafe Management System**.

---

## 1. Cấu Trúc Phân Cấp Trong TestRail

Trong các dự án phần mềm chuyên nghiệp, TestRail quản trị theo mô hình hình cây 4 cấp:

```
[Milestone: Release v1.0 - CyberCafe Ecosystem]
   └── [Test Plan: Full Regression & Security Sprint 1]
         └── [Test Suite: InternetCafe Core Master Suite]
               ├── [Section 01: Authentication & Security (TC-AUTH)]
               ├── [Section 02: Quản lý Phòng máy (TC-PC)]
               ├── [Section 03: Thu ngân & Tài chính ví (TC-POS)]
               ├── [Section 04: Dịch vụ F&B & Bếp (TC-FB)]
               ├── [Section 05: Phân quyền & Chống IDOR (TC-SEC)]
               ├── [Section 06: Nhân sự & Chấm công (TC-HRM)]
               └── [Section 07: Báo cáo & Hoá đơn (TC-REP)]
```

### Các thuật ngữ cốt lõi:
- **Test Case**: Định nghĩa kịch bản kiểm thử tĩnh (các bước làm, kết quả mong đợi).
- **Section**: Thư mục gom nhóm các test case theo phân hệ chức năng.
- **Test Run**: Một lần thực thi thực tế của một tập hợp các test case tại một thời điểm hoặc trên một phiên bản phần mềm cụ thể.
- **Test Result**: Kết quả thực thi của từng test case trong Run (`Passed`, `Failed`, `Blocked`, `Retest`).

---

## 2. Tiêu Chuẩn Thiết Kế Test Case Chuyên Nghiệp

Một Test Case chuẩn quốc tế cần đảm bảo **nguyên tắc 3C: Clear (Rõ ràng), Concise (Ngắn gọn), Consistent (Nhất quán)**:

1. **Title (Tiêu đề)**: Bắt đầu bằng động từ hành động + mục tiêu + điều kiện (Ví dụ: `TC-AUTH-01: Đăng nhập thành công với vai trò Quản lý (Manager)`).
2. **Preconditions (Điều kiện tiên quyết)**: Trạng thái hệ thống và dữ liệu cần có trước khi test (Ví dụ: `Tài khoản manager/Demo@123 đã tồn tại trong CSDL`).
3. **Test Steps (Các bước thực hiện)**: Đánh số thứ tự 1, 2, 3 rõ ràng, phân tách giữa hành động người dùng và dữ liệu nhập.
4. **Test Data (Dữ liệu thử nghiệm)**: Dữ liệu cụ thể để test (Username, Password, Amount, ProductId...).
5. **Expected Result (Kết quả kỳ vọng)**: Mô tả chính xác giao diện hiển thị gì, mã trạng thái HTTP là gì, dữ liệu CSDL thay đổi thế nào.

---

## 3. Hướng Dẫn Import Bộ Test Cases Vào TestRail

Hệ thống đã chuẩn bị sẵn file [InternetCafe_TestRail_TestCases.csv](file:///testing/testrail/InternetCafe_TestRail_TestCases.csv). Bạn có thể import trực tiếp vào TestRail chỉ trong 3 bước:

1. Đăng nhập vào workspace TestRail của bạn $\rightarrow$ Chọn Project **InternetCafe Management System**.
2. Bấm vào tab **Test Cases** $\rightarrow$ Chọn biểu tượng **Import CSV** (góc trên bên phải).
3. Chọn file `InternetCafe_TestRail_TestCases.csv` và ánh xạ (Column Mapping) như sau:
   - `Section` $\rightarrow$ **Section**
   - `Title` $\rightarrow$ **Title**
   - `Type` $\rightarrow$ **Type**
   - `Priority` $\rightarrow$ **Priority**
   - `Preconditions` $\rightarrow$ **Preconditions**
   - `Steps` $\rightarrow$ **Steps (Separate)**
   - `Expected Result` $\rightarrow$ **Expected Result**
4. Bấm **Import** $\rightarrow$ Hệ thống sẽ tự động tạo cấu trúc cây thư mục gồm 21 Test Cases chuẩn hóa.

---

## 4. Quy Trình Chạy Test Run & Đánh Giá Chất Lượng

Khi bắt đầu một đợt kiểm thử mới (Test Cycle):
1. Bấm **Add Test Run** $\rightarrow$ Đặt tên: `[Regression] Run v1.0.0-RC1 - SQL Server Integration`.
2. Gán người thực hiện (**Assigned To**): Tên Tester của bạn.
3. Khi test từng kịch bản:
   - Nếu kết quả đúng như mô tả $\rightarrow$ Đánh dấu **Passed**.
   - Nếu hệ thống bị văng lỗi, sai logic hoặc vi phạm bảo mật $\rightarrow$ Đánh dấu **Failed**, nhập **Elapsed Time** (thời gian test) và bấm nút **Add Bug** để liên kết mã ticket Jira (ví dụ: `CYBER-104`).
   - Nếu gặp tính năng chưa hoàn thiện làm cản trở test $\rightarrow$ Đánh dấu **Blocked**.
