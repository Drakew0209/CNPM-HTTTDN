# BÁO CÁO KIỂM TRA BẢO MẬT: PHÂN QUYỀN & KIỂM SOÁT TRUY CẬP (ACCESS CONTROL & IDOR)
**Mã phân loại vibe-check:** `4. ACCESS_CONTROL`  
**Dự án:** Hệ thống Quản trị & Vận hành Internet Cafe (`CNPM-HTTTDN`)  
**Ngày kiểm định:** 16/10/2026  
**Trạng thái kiểm tra:** Hoàn tất  

---

## 1. TỔNG QUAN HẠNG MỤC
Hạng mục này đánh giá việc ngăn chặn các lỗ hổng phân quyền ngang (IDOR - Insecure Direct Object References), leo thang đặc quyền (Privilege Escalation), và bảo vệ các nghiệp vụ trọng yếu (tạo đơn hàng, nạp/rút tiền, bắt đầu/kết thúc phiên chơi máy).

---

## 2. BẢNG TỔNG HỢP KẾT QUẢ KIỂM TRA

| Mã phát hiện | Hạng mục kiểm tra | Mức độ nghiêm trọng | Trạng thái xác minh | Vị trí (File & Dòng) |
| :--- | :--- | :---: | :---: | :--- |
| **ACL-01** | Tuyến đọc chi tiết chung `GET /{resource}/{id}` chống IDOR | **PASS** | Đã xác minh (Verified) | [Program.cs:161-177](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs#L161-L177) |
| **ACL-02** | Ràng buộc quyền sở hữu phiên chơi và đơn hàng (Start, End, Order) | **PASS** | Đã xác minh (Verified) | [BusinessService.cs:16, 82, 407, 430](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/BusinessService.cs#L16) |
| **ACL-03** | Chống tự hạ quyền hoặc tự khóa tài khoản Admin (Self-Lockout Prevention) | **PASS** | Đã xác minh (Verified) | [ManagementService.cs:456, 717](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ManagementService.cs#L456) |
| **ACL-04** | Endpoint Heartbeat thiếu xác thực mã máy trạm | **MEDIUM** | Nghi vấn bảo mật (Suspected) | [BusinessService.cs:537-546](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/BusinessService.cs#L537-L546) |
| **ACL-05** | Phân quyền vai trò theo tuyến đường nghiệp vụ (Role Enforcement) | **PASS** | Đã xác minh (Verified) | [Program.cs:118, 124, 146, 148, 154, 156](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs#L118) |

---

## 3. CHI TIẾT CÁC PHÁT HIỆN

### Phát hiện ACL-01: Cơ chế chống IDOR trên tuyến tài nguyên `GET /{resource}/{id}`
- **Mức độ:** `PASS` (Thiết kế phòng ngừa IDOR chủ động)
- **Tập tin liên quan:** [Program.cs:161-177](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs#L161-L177)
- **Bằng chứng mã nguồn:**
  ```csharp
  api.MapGet("/{resource}/{id}", [Authorize] async (HttpContext context, string resource, string id, CafeRepository repository, CancellationToken ct) =>
  {
      var workspace = await repository.WorkspaceAsync(context.User.IdentityContext(), ct);
      var collection = workspace.GetType().GetProperties()
          .FirstOrDefault(property => string.Equals(property.Name, resource, StringComparison.OrdinalIgnoreCase))
          ?.GetValue(workspace) as System.Collections.IEnumerable;
      if (collection is null)
          return Api.Error("NOT_FOUND", "Không tìm thấy loại dữ liệu.", StatusCodes.Status404NotFound);

      foreach (var row in collection)
      {
          var rowId = row?.GetType().GetProperty("Id")?.GetValue(row) as string;
          if (string.Equals(rowId, id, StringComparison.OrdinalIgnoreCase))
              return Results.Ok(Api.Data(row));
      }
      return Api.Error("NOT_FOUND", "Không tìm thấy dữ liệu hoặc bạn không có quyền xem.", StatusCodes.Status404NotFound);
  });
  ```
- **Phân tích:**
  - Thông thường, các API lấy chi tiết dữ liệu nhận tham số ID trực tiếp từ URL và thực hiện `SELECT * WHERE id = @id`, dẫn đến lỗ hổng IDOR nếu không kiểm tra `user_id == current_user.id`.
  - Trong dự án này, tuyến `GET /{resource}/{id}` không truy vấn trực tiếp vào database với ID mà người dùng truyền lên. Thay vào đó, nó nạp tập dữ liệu hợp lệ của chính người dùng đó thông qua mô hình `WorkspaceAsync(identity)` (trong đó các bảng khách hàng, đơn hàng, phiên chơi, giao dịch đều đã được lọc theo `WHERE Customer_ID = @customerId`).
  - Sau đó, hệ thống chỉ tìm ID khớp bên trong tập kết quả được cấp phép. Nếu một khách hàng cố tình đoán ID đơn hàng của khách hàng khác (ví dụ: `GET /orders/order-002`), tập dữ liệu của khách hàng đó không chứa đơn hàng này, và API trả về `404 NOT_FOUND: Không tìm thấy dữ liệu hoặc bạn không có quyền xem`.

---

### Phát hiện ACL-02: Ràng buộc sở hữu tài nguyên trong nghiệp vụ thay đổi trạng thái
- **Mức độ:** `PASS` (Đạt chuẩn an toàn)
- **Tập tin liên quan:** [BusinessService.cs:16, 82, 407, 430, 497](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/BusinessService.cs#L16)
- **Phân tích:**
  - **Bắt đầu phiên chơi (`StartSessionAsync`):** Chỉ cho phép `Role = Customer`. `CustomerId` được lấy trực tiếp từ Claim của Token được ký xác thực, không cho phép client tự gửi `CustomerId` khác.
  - **Kết thúc phiên (`EndSessionAsync`):**
    ```csharp
    if (identity.Role == Roles.Customer && identity.CustomerId != customerId)
        return await Rollback(transaction, OperationResult<object>.Fail("FORBIDDEN", "Bạn không sở hữu phiên này.", StatusCodes.Status403Forbidden), ct);
    ```
    Khách hàng chỉ có thể kết thúc phiên chơi của chính mình.
  - **Đặt món (`CreateOrderAsync`):** Kiểm tra bắt buộc phải có phiên chơi đang hoạt động trên máy đó gắn với chính `CustomerId` của người dùng:
    ```sql
    SELECT COUNT(*) FROM dbo.Usage_Sessions WHERE Customer_ID = @customerId AND Computer_ID = @computerId AND Status = 'Active'
    ```
    Khách hàng không thể đặt món cho máy khác hoặc tài khoản khác.
  - **Hủy đơn hàng (`TransitionOrderAsync`):** Khách hàng chỉ có quyền hủy đơn ở trạng thái `"Pending"` do chính mình tạo. Chỉ vai trò thu ngân/quản lý (`IsOperator`) mới được chuyển trạng thái sang `"Preparing"` hoặc `"Served"`.

---

### Phát hiện ACL-03: Chống tự hạ quyền hoặc tự khóa tài khoản Quản trị viên
- **Mức độ:** `PASS` (Đạt chuẩn an toàn)
- **Tập tin liên quan:** [ManagementService.cs:456, 717](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ManagementService.cs#L456)
- **Bằng chứng mã nguồn:**
  ```csharp
  if (key == actor.AccountId && ((Has(body, "role") && Value(body, "role") != Roles.Admin) || Value(body, "status") == "Banned"))
      return OperationResult<object>.Fail("SELF_LOCKOUT", "Không thể tự hạ quyền hoặc khóa tài khoản hiện tại.", StatusCodes.Status409Conflict);
  ```
- **Phân tích:**
  Mã nguồn chủ động ngăn ngừa tình huống Admin vô tình hoặc do bị thao túng tự khóa tài khoản của chính mình hoặc hạ quyền của bản thân khỏi nhóm Admin, ngăn ngừa rủi ro mất quyền quản trị toàn hệ thống.

---

### Phát hiện ACL-04: Tuyến Heartbeat thiếu cơ chế xác thực nguồn gốc máy trạm
- **Mức độ:** `MEDIUM` (Nghi vấn bảo mật / Suspected)
- **Tập tin liên quan:** [BusinessService.cs:537-546](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/BusinessService.cs#L537-L546)
- **Bằng chứng mã nguồn:**
  ```csharp
  public async Task<OperationResult<object>> HeartbeatAsync(IdentityContext identity, string id, CancellationToken ct)
  {
      var computerId = TryId(id, "pc"); if (computerId is null) return OperationResult<object>.Fail("NOT_FOUND", "Không tìm thấy máy.", StatusCodes.Status404NotFound);
      await using var connection = await connections.OpenAsync(ct);
      await using var command = new SqlCommand("""
          MERGE dbo.Machine_Heartbeats AS target USING (SELECT @id AS Computer_ID) AS source ON target.Computer_ID = source.Computer_ID
          WHEN MATCHED THEN UPDATE SET Last_Seen_At = SYSUTCDATETIME(), Last_Account_ID = @accountId
          WHEN NOT MATCHED THEN INSERT (Computer_ID, Last_Seen_At, Last_Account_ID) VALUES (@id, SYSUTCDATETIME(), @accountId);
          """, connection);
      command.Parameters.AddWithValue("@id", computerId.Value);
      command.Parameters.AddWithValue("@accountId", identity.AccountId);
      await command.ExecuteNonQueryAsync(ct);
      return OperationResult<object>.Ok(new { id, online = true });
  }
  ```
- **Phân tích rủi ro:**
  Bất kỳ người dùng nào có tài khoản hợp lệ (kể cả Khách hàng thông thường) đều có thể gửi yêu cầu `POST /api/v1/machines/{id}/heartbeat` với bất kỳ `id` máy nào (ví dụ `pc-001`, `pc-002`, ...). Mặc dù endpoint chỉ cập nhật `Last_Seen_At`, điều này có thể bị lạm dụng để làm sai lệch trạng thái trực tuyến của máy trong phòng máy hoặc ngăn chặn tiến trình nền `CloseExpiredSessionsAsync` tự động đóng phiên chơi khi máy bị tắt đột ngột.
- **Biện pháp khắc phục khuyến nghị:**
  Chỉ cho phép gửi heartbeat cho máy mà tài khoản hiện tại đang có phiên hoạt động (`Usage_Sessions.Status = 'Active'`), hoặc cấp Secret Key định danh riêng cho từng máy trạm desktop client.
