# BÁO CÁO KIỂM TRA BẢO MẬT: AN TOÀN FRONTEND & XSS (FRONTEND SECURITY)
**Mã phân loại vibe-check:** `5. FRONTEND_SECRETS`, `11. XSS`  
**Dự án:** Ứng dụng Web Quản trị Admin (`InternetCafe/apps/admin-web`)  
**Ngày kiểm định:** 16/10/2026  
**Trạng thái kiểm tra:** Hoàn tất  

---

## 1. TỔNG QUAN HẠNG MỤC
Hạng mục này kiểm tra nguy cơ XSS (Cross-Site Scripting) thông qua React DOM, các phương thức chèn HTML không an toàn (`dangerouslySetInnerHTML`, `innerHTML`), bảo vệ cửa sổ Popup In chứng từ, việc lộ bí mật qua biến môi trường frontend `VITE_*`, và cấu hình Source Maps.

---

## 2. BẢNG TỔNG HỢP KẾT QUẢ KIỂM TRA

| Mã phát hiện | Hạng mục kiểm tra | Mức độ nghiêm trọng | Trạng thái xác minh | Vị trí (File & Dòng) |
| :--- | :--- | :---: | :---: | :--- |
| **XSS-01** | Kiểm tra `dangerouslySetInnerHTML` và `innerHTML` | **PASS** | Đã xác minh (Verified) | Toàn bộ thư mục `apps/admin-web/src` |
| **XSS-02** | An toàn chống DOM XSS trong tính năng In chứng từ (`printRows`) | **PASS** | Đã xác minh (Verified) | [components.tsx:70-79](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/admin-web/src/components.tsx#L70-L79) |
| **XSS-03** | Khóa cấu hình Vite Dev Server `server.fs.strict: false` | **LOW** | Nghi vấn bảo mật (Suspected) | [vite.config.ts:7](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/admin-web/vite.config.ts#L7) |
| **XSS-04** | Rà soát biến môi trường Frontend `VITE_*` | **PASS** | Đã xác minh (Verified) | [.env](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/admin-web/.env#L1-L4) |

---

## 3. CHI TIẾT CÁC PHÁT HIỆN

### Phát hiện XSS-01: Rà soát nguy cơ XSS trong JSX
- **Mức độ:** `PASS` (Đạt chuẩn)
- **Tập tin liên quan:** Toàn bộ thư mục `apps/admin-web/src`
- **Phân tích:**
  - Không có bất kỳ thành phần React nào sử dụng `dangerouslySetInnerHTML`.
  - Mọi dữ liệu trả về từ máy chủ (tên khách hàng, tên máy, ghi chú phản hồi, nội dung khảo sát) đều được React tự động mã hóa thực thể HTML (`HTML entity encoding`) trước khi render vào DOM.
  - Các liên kết điều hướng chỉ sử dụng `<Link to="...">` của React Router nội bộ, không có thẻ `<a href={userInput}>` chứa nguy cơ chèn URI `javascript:...`.

---

### Phát hiện XSS-02: Cơ chế an toàn DOM trong tính năng In chứng từ
- **Mức độ:** `PASS` (Thiết kế mã nguồn tốt)
- **Tập tin liên quan:** [components.tsx:70-79](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/admin-web/src/components.tsx#L70-L79)
- **Bằng chứng mã nguồn:**
  ```ts
  export function printRows(title: string, rows: Row[], keys: string[], workspace: Workspace) {
    const popup = window.open('', '_blank', 'width=960,height=720'); if (!popup) return false;
    const d = popup.document;
    d.title = title; const style = d.createElement('style'); ...; d.head.append(style);
    const heading = d.createElement('h1'); heading.textContent = `InternetCafe · ${title}`; d.body.append(heading);
    ...
    for(const row of rows){
      const tr = d.createElement('tr');
      for(const k of keys){
        const td = d.createElement('td');
        td.textContent = displayValue(k, row[k], workspace);
        tr.append(td);
      }
      table.append(tr);
    }
    d.body.append(table);
  ```
- **Phân tích:**
  Nhiều ứng dụng thường sử dụng `popup.document.write("<table><tr><td>" + userInput + "</td></tr></table>")`, tạo ra lỗ hổng DOM XSS nghiêm trọng khi tên khách hàng hoặc ghi chú chứa mã JavaScript độc hại.
  Tại đây, hàm `printRows` hoàn toàn sử dụng `document.createElement` và gán thuộc tính qua `.textContent`. Trình duyệt sẽ tự động xem toàn bộ nội dung là văn bản thuần túy, vô hiệu hóa hoàn toàn nguy cơ thực thi mã độc.

---

### Phát hiện XSS-03: Cấu hình `server.fs.strict: false` trong Vite
- **Mức độ:** `LOW` (Tiềm ẩn rủi ro cục bộ)
- **Tập tin liên quan:** [vite.config.ts:7](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/admin-web/vite.config.ts#L7)
- **Bằng chứng mã nguồn:**
  ```ts
  export default defineConfig({
    plugins: [react()],
    server: { fs: { strict: false } },
    ...
  ```
- **Phân tích rủi ro:**
  Cấu hình `fs.strict: false` cho phép máy chủ dev của Vite phục vụ các file nằm ngoài thư mục gốc của dự án. Trong kịch bản chạy dev cục bộ trên `127.0.0.1` thì rủi ro bị hạn chế, tuy nhiên nếu bind ra mạng LAN hoặc chạy dev công khai, kẻ tấn công có thể lợi dụng endpoint `/@fs/` để đọc trộm file trên máy tính của lập trình viên.
- **Biện pháp khắc phục khuyến nghị:**
  Chuyển sang cấu hình an toàn bằng cách dùng `server.fs.allow`:
  ```ts
  server: {
    fs: {
      strict: true,
      allow: ['..']
    }
  }
  ```

---

### Phát hiện XSS-04: Biến môi trường Frontend không chứa khóa bí mật
- **Mức độ:** `PASS` (Đạt chuẩn)
- **Tập tin liên quan:** [apps/admin-web/.env](file:///c:/Users/KHANH/OneDrive/Desktop/Database/CNPM-HTTTDN/InternetCafe/apps/admin-web/.env#L1-L4)
- **Phân tích:**
  File `.env` của frontend chỉ chứa các biến điều hướng công khai:
  - `VITE_API_MODE=http`
  - `VITE_API_BASE_URL=http://127.0.0.1:5055/api/v1`
  - `VITE_SIGNALR_URL=http://127.0.0.1:5055/hubs/internetcafe`
  Không có API Key, Private Key, hay Database Credentials nào bị nhúng vào bundle JavaScript của client.
