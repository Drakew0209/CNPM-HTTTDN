# BÁO CÁO KIỂM TRA BẢO MẬT: KIỂM HỢP DỮ LIỆU ĐẦU VÀO & LOGIC NGHIỆP VỤ (INPUT VALIDATION)
**Mã phân loại vibe-check:** `10. INPUT_VALIDATION`, `14. BUSINESS_LOGIC`  
**Dự án:** Hệ thống Quản trị & Vận hành Internet Cafe (`CNPM-HTTTDN`)  
**Ngày kiểm định:** 16/10/2026  
**Trạng thái kiểm tra:** Hoàn tất  

---

## 1. TỔNG QUAN HẠNG MỤC
Hạng mục này đánh giá việc kiểm tra hợp lệ dữ liệu đầu vào trên máy chủ (Server-side Validation), kiểm tra chặn số âm hoặc tràn số đối với số tiền, số lượng hàng hóa tồn kho, và tính toán số dư khả dụng khi trừ tiền giờ chơi đang diễn ra.

---

## 2. BẢNG TỔNG HỢP KẾT QUẢ KIỂM TRA

| Mã phát hiện | Hạng mục kiểm tra | Mức độ nghiêm trọng | Trạng thái xác minh | Vị trí (File & Dòng) |
| :--- | :--- | :---: | :---: | :--- |
| **VAL-01** | Kiểm tra giới hạn số tiền nạp và hoàn tiền (Amount Boundaries) | **PASS** | Đã xác minh (Verified) | [BusinessService.cs:184, 274, 345](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/BusinessService.cs#L184) |
| **VAL-02** | Chống số âm và kiểm tra số dư khả dụng khi đặt món (Incurred Rental Deduction) | **PASS** | Đã xác minh (Verified) | [BusinessService.cs:408, 452-455](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/BusinessService.cs#L408) |
| **VAL-03** | Khóa giá món tại thời điểm đặt đơn hàng (Price Tampering Protection) | **PASS** | Đã xác minh (Verified) | [BusinessService.cs:440, 443](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/BusinessService.cs#L440) |
| **VAL-04** | Thiếu kiểm tra số âm ở tầng ứng dụng cho định mức nguyên vật liệu | **LOW** | Đã xác minh (Verified) | [ManagementService.cs:838-844](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ManagementService.cs#L838-L844) |

---

## 3. CHI TIẾT CÁC PHÁT HIỆN

### Phát hiện VAL-01: Giới hạn hạn mức giao dịch nạp / rút tiền
- **Mức độ:** `PASS` (Đạt chuẩn)
- **Tập tin liên quan:** [BusinessService.cs:184, 274, 345](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/BusinessService.cs#L184)
- **Bằng chứng mã nguồn:**
  ```csharp
  if (request.Amount is < 1000 or > 10000000)
      return OperationResult<object>.Fail("VALIDATION_ERROR", "Số tiền nạp/rút từ 1.000 đến 10.000.000 đồng.");
  ```
- **Phân tích:**
  - Hệ thống chặn hoàn toàn các giá trị âm (ví dụ `amount = -50000` nhằm làm tăng ngược số dư).
  - Khống chế trần giao dịch 10.000.000 đồng để ngăn ngừa lỗi tràn số hoặc nhập nhầm số tiền lớn.

---

### Phát hiện VAL-02: Kiểm tra số dư khả dụng thực tế khi khách đang chơi
- **Mức độ:** `PASS` (Đạt chuẩn nghiệp vụ chuyên sâu)
- **Tập tin liên quan:** [BusinessService.cs:444-455](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/BusinessService.cs#L444-L455)
- **Bằng chứng mã nguồn:**
  ```csharp
  var incurredRental = 0m;
  if (await rentalReader.ReadAsync(ct))
  {
      var (start, rate) = (rentalReader.GetDateTime(0), rentalReader.IsDBNull(1) ? 0m : rentalReader.GetDecimal(1));
      incurredRental = Math.Min(balance, decimal.Ceiling((decimal)Math.Max(0, (DateTime.UtcNow - DateTime.SpecifyKind(start, DateTimeKind.Utc)).TotalSeconds) * rate / 3600m));
  }
  await rentalReader.CloseAsync();
  if (balance - incurredRental < total)
      return await Rollback(transaction, OperationResult<object>.Fail("INSUFFICIENT_BALANCE", "Số dư khả dụng không đủ để đặt món (đã trừ tạm tính tiền giờ đang sử dụng).", StatusCodes.Status409Conflict), ct);
  ```
- **Phân tích:**
  Đây là kịch bản gian lận phổ biến ở các quán net: Khách hàng nạp 20.000đ, ngồi chơi gần hết 20.000đ tiền giờ, sau đó dùng chính số dư hiển thị chưa trừ trên tài khoản để đặt đồ ăn/nước uống. Hệ thống tính toán chính xác số giây đã chơi nhân với giá theo giờ để trừ trước vào số dư thực tế, ngăn ngừa tình trạng số dư bị âm khi kết thúc phiên.

---

### Phát hiện VAL-03: Phòng chống thao túng giá món ăn từ Client
- **Mức độ:** `PASS` (Đạt chuẩn)
- **Tập tin liên quan:** [BusinessService.cs:436-443](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/BusinessService.cs#L436-L443)
- **Phân tích:**
  Client không có quyền tự gửi tổng tiền của đơn hàng. Backend lấy giá niêm yết trực tiếp từ bảng `dbo.Products` bằng câu lệnh có khóa dòng `WITH (UPDLOCK, HOLDLOCK)`, sau đó tự nhân với số lượng và tính tổng tiền:
  ```csharp
  var price = reader.GetDecimal(1);
  ...
  var total = lines.Sum(x => x.Quantity * x.Price);
  ```
  Nếu Client có truyền `ExpectedUnitPrice` mà giá đã bị thay đổi ở backend, hệ thống trả về lỗi `PRICE_CHANGED` để bảo vệ quyền lợi người mua.

---

### Phát hiện VAL-04: Bỏ sót kiểm tra số âm ở tầng ứng dụng cho định mức nguyên vật liệu
- **Mức độ:** `LOW`
- **Tập tin liên quan:** [ManagementService.cs:838-844](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/backend/InternetCafe.Backend/Services/ManagementService.cs#L838-L844)
- **Bằng chứng mã nguồn:**
  ```csharp
  var stock = Decimal(body, "stockQuantity") ?? 0m;
  var reorder = Decimal(body, "reorderLevel") ?? 0m;
  var cost = Decimal(body, "unitCost") ?? 0m;
  if (name is null) return Validation("Tên nguyên vật liệu là bắt buộc.");
  ```
- **Phân tích rủi ro:**
  Code C# không kiểm tra điều kiện `stock < 0 || reorder < 0 || cost < 0`. Trong database có ràng buộc `CHECK (Stock_Quantity >= 0)`. Khi người dùng nhập số âm, thay vì nhận phản hồi lỗi `400 Validation`, database sẽ ném ra ngoại lệ `SqlException`, dẫn đến phản hồi lỗi máy chủ không thân thiện.
- **Biện pháp khắc phục khuyến nghị:**
  Thêm kiểm tra điều kiện:
  ```csharp
  if (stock < 0 || reorder < 0 || cost < 0)
      return Validation("Số lượng tồn kho, mức đặt hàng lại và đơn giá không được là số âm.");
  ```
