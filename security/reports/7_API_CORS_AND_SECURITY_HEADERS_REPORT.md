# BÁO CÁO KIỂM TRA BẢO MẬT: CẤU HÌNH API, CORS & SECURITY HEADERS
**Mã phân loại vibe-check:** `8. SECURITY_HEADERS`, `9. CORS`, `15. ERROR_HANDLING`  
**Dự án:** Backend Web API ASP.NET Core (`InternetCafe.Backend`)  
**Ngày kiểm định:** 16/10/2026  
**Trạng thái kiểm tra:** Hoàn tất  

---

## 1. TỔNG QUAN HẠNG MỤC
Hạng mục này kiểm tra chính sách chia sẻ tài nguyên nguồn gốc chéo (CORS), các tiêu đề bảo mật HTTP (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy), việc xử lý ngoại lệ toàn cục và ngăn ngừa rò rỉ vết ngăn xếp (Stack Traces) ra bên ngoài.

---

## 2. BẢNG TỔNG HỢP KẾT QUẢ KIỂM TRA

| Mã phát hiện | Hạng mục kiểm tra | Mức độ nghiêm trọng | Trạng thái xác minh | Vị trí (File & Dòng) |
| :--- | :--- | :---: | :---: | :--- |
| **HDR-01** | Thiếu bộ Security Headers (X-Frame-Options, nosniff, CSP) | **MEDIUM** | Đã xác minh (Verified) | [Program.cs:33-37](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs#L33-L37) |
| **HDR-02** | Thiếu Middleware xử lý ngoại lệ toàn cục (Global Exception Handler) | **MEDIUM** | Đã xác minh (Verified) | [Program.cs:29-37](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs#L29-L37) |
| **HDR-03** | Cấu hình CORS chặt chẽ, không dùng Wildcard với Credentials | **PASS** | Đã xác minh (Verified) | [Program.cs:25-27](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs#L25-L27) |
| **HDR-04** | Cấu hình AllowedHosts chấp nhận mọi Host Header | **LOW** | Đã xác minh (Verified) | [appsettings.json:16](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/appsettings.json#L16) |

---

## 3. CHI TIẾT CÁC PHÁT HIỆN

### Phát hiện HDR-01: Thiếu các Security Headers thiết yếu trong phản hồi HTTP
- **Mức độ:** `MEDIUM`
- **Tập tin liên quan:** [Program.cs:33-37](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs#L33-L37)
- **Bằng chứng mã nguồn:**
  ```csharp
  var app = builder.Build();
  ...
  app.UseCors("local");
  app.UseStaticFiles();
  app.UseAuthentication();
  app.UseAuthorization();
  ```
- **Phân tích rủi ro:**
  Máy chủ API không cấu hình chèn các tiêu đề bảo mật HTTP tiêu chuẩn:
  1. `X-Frame-Options: DENY`: Ngăn chặn website bị nhúng vào `<iframe>` của trang web giả mạo (tấn công Clickjacking).
  2. `X-Content-Type-Options: nosniff`: Ngăn chặn trình duyệt đoán sai MIME type của các file tĩnh được phục vụ.
  3. `Referrer-Policy: strict-origin-when-cross-origin`: Giữ kín thông tin đường dẫn URL khi gửi yêu cầu sang domain khác.
- **Biện pháp khắc phục khuyến nghị:**
  Thêm middleware chèn Header trước khi xử lý request:
  ```csharp
  app.Use(async (context, next) =>
  {
      context.Response.Headers.Append("X-Frame-Options", "DENY");
      context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
      context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
      await next();
  });
  ```

---

### Phát hiện HDR-02: Thiếu Middleware xử lý ngoại lệ toàn cục
- **Mức độ:** `MEDIUM`
- **Tập tin liên quan:** [Program.cs:29-37](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs#L29-L37)
- **Phân tích rủi ro:**
  Ứng dụng Minimal API không gọi `app.UseExceptionHandler(...)`. Khi xảy ra lỗi không mong muốn (ví dụ lỗi kết nối SQL Server, lỗi ép kiểu dữ liệu), nếu môi trường chạy ở chế độ `Development`, ASP.NET Core sẽ hiển thị trang lỗi Developer Exception Page bao gồm:
  - Chi tiết đường dẫn thư mục nội bộ máy chủ (`C:\Users\KHANH\...`)
  - Đoạn code C# và số dòng bị lỗi
  - Cấu trúc các bảng và cột cơ sở dữ liệu
  Kẻ tấn công có thể cố tình gửi payload lỗi để trích xuất thông tin kiến trúc nội bộ.
- **Biện pháp khắc phục khuyến nghị:**
  Đăng ký Exception Handler chuẩn hóa định dạng trả về:
  ```csharp
  app.UseExceptionHandler(exceptionApp =>
  {
      exceptionApp.Run(async context =>
      {
          context.Response.StatusCode = StatusCodes.Status500InternalServerError;
          context.Response.ContentType = "application/json";
          await context.Response.WriteAsJsonAsync(new {
              error = new {
                  code = "INTERNAL_SERVER_ERROR",
                  message = "Đã xảy ra lỗi trên hệ thống máy chủ."
              }
          });
      });
  });
  ```

---

### Phát hiện HDR-03: Cấu hình CORS an toàn
- **Mức độ:** `PASS` (Đạt chuẩn an toàn)
- **Tập tin liên quan:** [Program.cs:25-27](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Program.cs#L25-L27)
- **Bằng chứng mã nguồn:**
  ```csharp
  builder.Services.AddCors(options => options.AddPolicy("local", policy => policy
      .WithOrigins("http://127.0.0.1:5173", "http://localhost:5173", "http://127.0.0.1:4173", "http://localhost:4173")
      .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
  ```
- **Phân tích:**
  Chính sách CORS tuân thủ nghiêm ngặt nguyên tắc của vibe-check:
  - Tuyệt đối không dùng `AllowAnyOrigin()` hoặc ký tự đại diện `*` khi có bật `AllowCredentials()`.
  - Chỉ cho phép các domain phát triển và quản trị được định danh rõ ràng.
  - Ngăn ngừa hoàn toàn nguy cơ một trang web độc hại từ domain lạ gửi yêu cầu đánh cắp phiên đăng nhập của quản trị viên.

---

### Phát hiện HDR-04: Cấu hình AllowedHosts chấp nhận ký tự đại diện `*`
- **Mức độ:** `LOW`
- **Tập tin liên quan:** [appsettings.json:16](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/appsettings.json#L16)
- **Phân tích rủi ro:**
  Giá trị `"AllowedHosts": "*"` cho phép máy chủ chấp nhận mọi Host Header trong yêu cầu HTTP. Đối với môi trường phát triển cục bộ thì điều này hoàn toàn bình thường, nhưng khi đưa lên máy chủ công khai cần chỉ định rõ host hợp lệ để tránh rủi ro Host Header Poisoning.
