# BÁO CÁO KIỂM TRA BẢO MẬT: XÁC THỰC & QUẢN LÝ PHIÊN (AUTH & SESSION)
**Mã phân loại vibe-check:** `3. AUTH_MIDDLEWARE`, `4. SESSION_MANAGEMENT`  
**Dự án:** Hệ thống Quản trị & Vận hành Internet Cafe (`CNPM-HTTTDN`)  
**Ngày kiểm định:** 16/10/2026  
**Trạng thái kiểm tra:** Hoàn tất  

---

## 1. TỔNG QUAN HẠNG MỤC
Hạng mục này kiểm tra luồng xác thực đăng nhập (`POST /auth/login`), quy trình đăng ký (`POST /auth/register`), cơ chế sinh và xác minh token Bearer, thu hồi phiên làm việc khi logout / đổi mật khẩu / khóa tài khoản, và cơ chế chống tấn công brute-force.

---

## 2. BẢNG TỔNG HỢP KẾT QUẢ KIỂM TRA

| Mã phát hiện | Hạng mục kiểm tra | Mức độ nghiêm trọng | Trạng thái xác minh | Vị trí (File & Dòng) |
| :--- | :--- | :---: | :---: | :--- |
| **AUT-01** | Thiếu cơ chế Rate Limiting / Chống dò mật khẩu (Brute Force) | **HIGH** | Đã xác minh (Verified) | [Program.cs:47](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs#L47), [AuthService.cs:13](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/AuthService.cs#L13) |
| **AUT-02** | SignalR WebSocket handshake thiếu kiểm tra token qua Query String | **MEDIUM** | Đã xác minh (Verified) | [ApiTokenAuthenticationHandler.cs:17](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ApiTokenAuthenticationHandler.cs#L17) |
| **AUT-03** | Khả năng thu hồi Token khi Logout, Đổi mật khẩu hoặc Bị cấm (Revocation) | **PASS** | Đã xác minh (Verified) | [AuthService.cs:38, 103](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/AuthService.cs#L38), [ApiTokenAuthenticationHandler.cs:25](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ApiTokenAuthenticationHandler.cs#L25) |
| **AUT-04** | Độ mạnh ngẫu nhiên của Token (Cryptographic Entropy) | **PASS** | Đã xác minh (Verified) | [AuthService.cs:27](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/AuthService.cs#L27) |
| **AUT-05** | Xác thực tập trung qua Middleware trước khi vào Handler | **PASS** | Đã xác minh (Verified) | [Program.cs:22, 35](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs#L22) |

---

## 3. CHI TIẾT CÁC PHÁT HIỆN

### Phát hiện AUT-01: Thiếu giới hạn tần suất truy vấn (Rate Limiting) trên API Xác thực
- **Mức độ:** `HIGH`
- **Tập tin liên quan:**
  - [Program.cs:47-53](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs#L47-L53)
  - [AuthService.cs:13-33](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/AuthService.cs#L13-L33)
- **Bằng chứng mã nguồn:**
  ```csharp
  api.MapPost("/auth/login", async (LoginRequest request, AuthService auth, CancellationToken ct) =>
  {
      var result = await auth.LoginAsync(request, ct);
      return result is null
          ? Api.Error("INVALID_CREDENTIALS", "Tên đăng nhập hoặc mật khẩu không đúng.", StatusCodes.Status401Unauthorized)
          : Results.Ok(Api.Data(result));
  }).AllowAnonymous();
  ```
- **Phân tích rủi ro:**
  Endpoint `/api/v1/auth/login` và `/api/v1/auth/register` cho phép gọi công khai (`AllowAnonymous()`) mà không có bất kỳ bộ đếm số lần thử thất bại, thời gian chờ trừng phạt (exponential backoff) hoặc khóa tạm thời (account lockout). Kẻ tấn công có thể viết một script tự động thử hàng triệu mật khẩu phổ biến (dictionary attack/credential stuffing) nhắm vào các tài khoản nhạy cảm như `admin`, `manager`, `cashier`.
- **Biện pháp khắc phục khuyến nghị:**
  Tích hợp middleware `Microsoft.AspNetCore.RateLimiting` của .NET (giới hạn tối đa 5 yêu cầu đăng nhập sai trong 1 phút trên mỗi IP/Tài khoản).

---

### Phát hiện AUT-02: SignalR WebSocket Handshake thiếu hỗ trợ Token qua Query String
- **Mức độ:** `MEDIUM`
- **Tập tin liên quan:** [ApiTokenAuthenticationHandler.cs:17-20](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ApiTokenAuthenticationHandler.cs#L17-L20)
- **Bằng chứng mã nguồn:**
  ```csharp
  protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
  {
      var authorization = Request.Headers.Authorization.ToString();
      if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();
      var rawToken = authorization[7..].Trim();
      if (rawToken.Length != 64) return AuthenticateResult.Fail("Invalid bearer token.");
      ...
  ```
- **Phân tích rủi ro:**
  Tại frontend `realtime.ts`, SignalR Client kết nối qua hàm:
  ```ts
  .withUrl(url, { accessTokenFactory: () => token })
  ```
  Theo đặc tả của chuẩn WebSocket trên trình duyệt web, JavaScript API `new WebSocket()` không hỗ trợ truyền header HTTP tùy biến (`Authorization: Bearer ...`). Do đó, thư viện `@microsoft/signalr` bắt buộc phải gửi token qua query string dạng `?access_token=<token>`.
  Hiện tại `ApiTokenAuthenticationHandler` chỉ đọc `Request.Headers.Authorization`, dẫn tới kết nối WebSocket từ trình duyệt lên Hub `[Authorize] CafeHub` sẽ bị từ chối 401 Unauthorized trừ khi SignalR fallback về LongPolling.
- **Biện pháp khắc phục khuyến nghị:**
  Bổ sung kiểm tra query string khi request gửi tới endpoint Hub:
  ```csharp
  var rawToken = string.Empty;
  var authorization = Request.Headers.Authorization.ToString();
  if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
  {
      rawToken = authorization[7..].Trim();
  }
  else if (Request.Path.StartsWithSegments("/hubs") && Request.Query.TryGetValue("access_token", out var tokenFromQuery))
  {
      rawToken = tokenFromQuery.ToString().Trim();
  }
  ```

---

### Phát hiện AUT-03: Kiểm tra thu hồi Token (Revocation & Blacklisting)
- **Mức độ:** `PASS` (Đạt chuẩn an toàn cao)
- **Tập tin liên quan:**
  - [AuthService.cs:38, 103](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/AuthService.cs#L38)
  - [ApiTokenAuthenticationHandler.cs:23-26](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ApiTokenAuthenticationHandler.cs#L23-L26)
- **Phân tích:**
  - Token không phải là Stateless JWT mà là Opaque Token được lưu trong bảng `dbo.App_Tokens`.
  - Trên **mỗi yêu cầu HTTP**, middleware thực hiện truy vấn:
    ```sql
    SELECT a.Account_ID, a.Username, a.Role, a.Customer_ID, a.Employee_ID
    FROM dbo.App_Tokens t JOIN dbo.App_Accounts a ON a.Account_ID = t.Account_ID
    WHERE t.Token_Hash = @hash AND t.Revoked_At IS NULL AND t.Expires_At > SYSUTCDATETIME() AND a.Status = 'Active'
    ```
  - Khi người dùng đăng xuất (`/auth/logout`), `Revoked_At` được cập nhật thời gian hiện tại -> Token lập tức vô hiệu hóa.
  - Khi người dùng đổi mật khẩu (`/me/password`), toàn bộ token hiện có của tài khoản đều bị thu hồi.
  - Khi một tài khoản bị cấm (`Status = 'Banned'`), điều kiện `a.Status = 'Active'` lập tức chặn toàn bộ yêu cầu của tài khoản đó mà không cần đợi token hết hạn.

---

### Phát hiện AUT-04: Độ ngẫu nhiên và an toàn mật mã của Token
- **Mức độ:** `PASS` (Đạt chuẩn an toàn)
- **Tập tin liên quan:** [AuthService.cs:27](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/AuthService.cs#L27)
- **Phân tích:**
  Token được sinh bằng `RandomNumberGenerator.GetBytes(32)` trong `System.Security.Cryptography`, mang lại 256 bits entropy ngẫu nhiên thực sự từ CSPRNG của hệ điều hành. Kẻ tấn công không thể đoán trước hoặc tạo ra token giả mạo bằng phương pháp phân tích chuỗi thời gian.
