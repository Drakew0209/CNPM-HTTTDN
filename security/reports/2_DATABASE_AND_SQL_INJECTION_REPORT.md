# BÁO CÁO KIỂM TRA BẢO MẬT: CƠ SỞ DỮ LIỆU & SQL INJECTION
**Mã phân loại vibe-check:** `2. DATABASE_ACCESS`  
**Dự án:** Hệ thống Quản trị & Vận hành Internet Cafe (`CNPM-HTTTDN`)  
**Ngày kiểm định:** 16/10/2026  
**Trạng thái kiểm tra:** Hoàn tất  

---

## 1. TỔNG QUAN HẠNG MỤC
Hạng mục này kiểm tra toàn bộ các câu truy vấn cơ sở dữ liệu qua ADO.NET (`Microsoft.Data.SqlClient`), việc sử dụng Parameterized Queries, rủi ro SQL Injection qua nối chuỗi, cũng như việc rò rỉ các cột nhạy cảm (`Password_Hash`, `Token_Hash`) ra API.

---

## 2. BẢNG TỔNG HỢP KẾT QUẢ KIỂM TRA

| Mã phát hiện | Hạng mục kiểm tra | Mức độ nghiêm trọng | Trạng thái xác minh | Vị trí (File & Dòng) |
| :--- | :--- | :---: | :---: | :--- |
| **SQL-01** | Kiểm tra Parameterized Query (Phòng chống SQL Injection) | **PASS** | Đã xác minh (Verified) | Toàn bộ repository (`CafeRepository`, `ManagementRepository`, `AuthService`, `BusinessService`) |
| **SQL-02** | Rò rỉ dữ liệu nhạy cảm (Cột Password_Hash / Token_Hash) | **PASS** | Đã xác minh (Verified) | [ManagementRepository.cs:200](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ManagementRepository.cs#L200), [CafeRepository.cs:112](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/CafeRepository.cs#L112) |
| **SQL-03** | Rủi ro tiềm ẩn từ String Interpolation trong Dynamic Update Queries | **LOW** | Nghi vấn kiến trúc (Suspected) | [ManagementService.cs:721, 740, 755](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ManagementService.cs#L721) |
| **SQL-04** | Sử dụng Transaction và Khóa hàng đồng thời (Concurrency Locking) | **PASS** | Đã xác minh (Verified) | [BusinessService.cs:20, 71, 285, 350, 409](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/BusinessService.cs#L20) |

---

## 3. CHI TIẾT CÁC PHÁT HIỆN

### Phát hiện SQL-01: Kiểm tra chống SQL Injection qua Parameterized Query
- **Mức độ:** `PASS` (Đạt chuẩn an toàn xuất sắc)
- **Tập tin liên quan:**
  - `AuthService.cs`
  - `BusinessService.cs`
  - `CafeRepository.cs`
  - `ManagementRepository.cs`
  - `ManagementService.cs`
- **Phân tích:**
  Qua quét toàn bộ hơn 60 câu lệnh `SqlCommand` trong dự án:
  - 100% các giá trị do người dùng truyền vào (`username`, `id`, `amount`, `fullName`, `phone`, `note`, `date`, v.v.) đều được gán thông qua `command.Parameters.AddWithValue(...)`.
  - Không tồn tại bất kỳ câu lệnh nào ghép chuỗi biến người dùng trực tiếp vào SQL dạng `$"SELECT ... WHERE user = '{input}'"`.
  - Các giá trị khóa ID kiểu chuỗi (ví dụ `"pc-001"`, `"order-005"`) đều được phân tích cú pháp chặt chẽ qua hàm `TryId` / `ParseId` thành số nguyên (`int`) trước khi đưa vào SQL Parameter:
  ```csharp
  command.Parameters.AddWithValue("@id", key.Value);
  ```

---

### Phát hiện SQL-02: Kiểm tra rò rỉ cột thông tin nhạy cảm
- **Mức độ:** `PASS` (Đạt chuẩn an toàn)
- **Tập tin liên quan:** [ManagementRepository.cs:199-206](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ManagementRepository.cs#L199-L206)
- **Phân tích:**
  - Cột `Password_Hash` trong `dbo.App_Accounts` và `dbo.Customers` chỉ được SELECT duy nhất trong 2 hàm nội bộ của `AuthService.cs` (hàm `LoginAsync` để so khớp mật khẩu và `ChangePasswordAsync` để xác nhận mật khẩu cũ).
  - Khi API trả về danh sách tài khoản cho Admin (`AccountsAsync`), câu truy vấn chỉ lấy các trường an toàn:
  ```sql
  SELECT Account_ID, Username, Full_Name, Role, Status, Employee_ID
  FROM dbo.App_Accounts
  WHERE Role <> 'Customer'
  ```
  - Cột `Password_Hash` và `Token_Hash` hoàn toàn không bị đưa vào bất kỳ DTO nào trả về frontend hoặc desktop client.

---

### Phát hiện SQL-03: Rủi ro tiềm ẩn từ String Interpolation trong Dynamic Update
- **Mức độ:** `LOW` (Nghi vấn kiến trúc / Architectural drift risk)
- **Tập tin liên quan:** [ManagementService.cs:721, 740, 755](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ManagementService.cs#L721)
- **Bằng chứng mã nguồn:**
  ```csharp
  private async Task<OperationResult<object>> UpdatedAsync(SqlConnection connection, string table, List<string> changes, List<(string, object)> parameters, CancellationToken ct, Func<object> result)
  {
      if (changes.Count == 0) return Validation("Không có dữ liệu để cập nhật.");
      var n = await ExecuteNonQueryAsync(connection, $"UPDATE {table} SET {string.Join(',', changes)} WHERE {KeyColumn(table)}=@id", ct, parameters.ToArray());
      return n == 0 ? NotFound() : OperationResult<object>.Ok(result());
  }
  ```
- **Phân tích rủi ro:**
  - Hiện tại, danh sách `changes` được tạo bởi hàm trợ giúp nội bộ `Add(changes, parameters, "Full_Name", "@name", value)` với tên cột được hardcoded trong code C# (ví dụ `"Full_Name=@name"`), và giá trị người dùng được đưa vào `@name`.
  - Tên bảng `table` lấy từ switch expression nội bộ.
  - Do đó, hiện tại **không thể khai thác SQL Injection** qua hàm này.
  - Tuy nhiên, việc dựng câu lệnh UPDATE bằng string interpolation động tiềm ẩn rủi ro nếu trong tương lai có lập trình viên mở rộng bằng cách truyền tên cột từ `JsonElement` của người dùng mà không có danh sách whitelist kiểm duyệt.
- **Biện pháp khắc phục khuyến nghị:**
  Duy trì danh sách Whitelist các cột được phép cập nhật hoặc sử dụng ORM / Mapping cố định để loại trừ hoàn toàn việc nội suy chuỗi SQL.

---

### Phát hiện SQL-04: Đảm bảo tính toàn vẹn giao dịch và phòng ngừa Race Condition
- **Mức độ:** `PASS` (Đạt chuẩn kỹ thuật cao)
- **Tập tin liên quan:** [BusinessService.cs:20, 71, 285, 350, 409](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/BusinessService.cs#L20)
- **Phân tích:**
  - Tất cả các thao tác thay đổi số dư, bắt đầu/kết thúc phiên chơi, tạo đơn hàng món ăn, và hoàn tiền đều sử dụng mức cô lập tuần tự cao nhất: `IsolationLevel.Serializable`.
  - Các câu lệnh SELECT được gắn kèm gợi ý khóa hàng nghiêm ngặt: `WITH (UPDLOCK, HOLDLOCK)`.
  - Thiết kế này ngăn chặn triệt để các lỗi Race Condition kinh điển như: Đặt 2 đơn hàng đồng thời để trừ tiền 1 lần, hoặc nạp/rút tiền song song (Double Spending).
