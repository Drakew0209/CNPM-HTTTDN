# Ma Trận Truy Vết Yêu Cầu (Requirement Traceability Matrix - RTM)

Bảng ma trận này liên kết chặt chẽ từ **Yêu Cầu Nghiệp Vụ (User Story)** $\rightarrow$ **Kịch Bản Test Manual (TestRail)** $\rightarrow$ **Kiểm Thử API (Postman)** $\rightarrow$ **Tự Động Hóa Giao Diện (Playwright E2E)** $\rightarrow$ **Quản Lý Lỗi (Jira)**.

---

| STT | Mã Yêu Cầu (User Story) | Mô tả tính năng | TestRail Test Case ID | Postman Request Path | Playwright E2E Spec | Jira Defect ID | Trạng Thái |
| :---: | :--- | :--- | :--- | :--- | :--- | :---: | :---: |
| **01** | `US-AUTH-01` | Đăng nhập hệ thống theo 5 phân quyền RBAC | `TC-AUTH-01`, `TC-AUTH-02` | `POST /auth/login` | `e2e/auth.spec.ts` | — | **PASSED** |
| **02** | `US-AUTH-02` | Bảo vệ form đăng nhập trước SQL Injection | `TC-AUTH-04` | `POST /auth/login` (SQLi payload) | `e2e/auth.spec.ts` | — | **PASSED** |
| **03** | `US-AUTH-03` | Chống tấn công Stored XSS trong Profile | `TC-AUTH-05` | `PATCH /me` (XSS payload) | — | — | **PASSED** |
| **04** | `US-PC-01` | Theo dõi sơ đồ phòng máy và trạng thái trực quan | `TC-PC-01` | `GET /computers` | `e2e/machines.spec.ts` | — | **PASSED** |
| **05** | `US-PC-02` | Điều khiển máy trạm từ xa: Bảo trì / Sẵn sàng | `TC-PC-02`, `TC-PC-03` | `POST /computers/{id}/command` | `e2e/machines.spec.ts` | — | **PASSED** |
| **06** | `US-POS-01` | Thu ngân nạp tiền vào ví hội viên | `TC-POS-01` | `POST /customers/{id}/topup` | `e2e/pos-billing.spec.ts` | `CYBER-102` | **PASSED** |
| **07** | `US-POS-02` | Chặn nạp tiền số âm và gian lận cước | `TC-POS-02` | `POST /customers/{id}/topup` (-50k) | — | — | **PASSED** |
| **08** | `US-POS-03` | Rút tiền ví hội viên và kiểm soát số dư | `TC-POS-03`, `TC-POS-04` | `POST /customers/{id}/refund` | — | — | **PASSED** |
| **09** | `US-FB-01` | Khách hàng đặt món F&B từ máy đang chơi | `TC-FB-01` | `POST /orders` | `e2e/pos-billing.spec.ts` | — | **PASSED** |
| **10** | `US-FB-02` | Chặn đặt món với số lượng $\le 0$ (Cart Tampering) | `TC-FB-03` | `POST /orders` (Qty: -5, 0) | — | `CYBER-101` | **PASSED** |
| **11** | `US-SEC-01` | Chặn Customer truy cập API Quản trị hệ thống | `TC-SEC-01` | `GET /accounts` (Customer Token) | — | — | **PASSED** |
| **12** | `US-SEC-02` | Chống rò rỉ dữ liệu khách hàng khác (IDOR) | `TC-SEC-02` | `GET /customers/c-002` (Customer Token) | — | `CYBER-103` | **PASSED** |
| **13** | `US-HRM-01` | Nhân viên chấm công vào ca làm việc | `TC-HRM-01`, `TC-HRM-02` | `POST /attendance/check-in` | `e2e/auth.spec.ts` | — | **PASSED** |
| **14** | `US-REP-01` | Báo cáo doanh thu tài chính tổng hợp & biểu đồ | `TC-REP-01` | `GET /reports/summary` | `e2e/reports.spec.ts` | — | **PASSED** |
| **15** | `US-REP-02` | Tra cứu danh sách và chi tiết hoá đơn dịch vụ | `TC-REP-02` | `GET /invoices`, `GET /invoices/{id}` | `e2e/reports.spec.ts` | — | **PASSED** |

---

### Ý nghĩa của RTM trong dự án chuyên nghiệp:
1. **Không bỏ sót tính năng**: Đảm bảo 100% User Stories trong bản thiết kế đều có ít nhất một Test Case tương ứng kiểm tra.
2. **Đánh giá độ bao phủ kiểm thử (Test Coverage)**: Xác định rõ tính năng nào đã được tự động hoá qua API (Postman) và giao diện (Playwright).
3. **Theo dõi độ ổn định (Defect Traceability)**: Khi có lỗi phát sinh trên Jira, biết ngay lỗi đó thuộc User Story nào và Test Case nào cần cập nhật.
