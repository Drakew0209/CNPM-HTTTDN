@echo off
title InternetCafe - Khởi chạy toàn bộ hệ thống (1-Click)
chcp 65001 >nul
cd /d "%~dp0"

echo ============================================================
echo   HỆ THỐNG QUẢN LÝ PHÒNG MÁY INTERNETCAFE (ENTERPRISE)
echo   KHỞI CHẠY TOÀN BỘ 1-CLICK: DATABASE + BACKEND + WEB + CLIENT
echo ============================================================
echo.

echo [1/4] Đang khởi động SQL Server LocalDB (MSSQLLocalDB)...
sqllocaldb start MSSQLLocalDB >nul 2>&1
echo       -^> SQL Server LocalDB đã sẵn sàng.
echo.

echo [2/4] Đang khởi chạy Backend API (Cổng 5055)...
start "InternetCafe - Backend API (5055)" cmd /k "chcp 65001 >nul && cd /d "%~dp0InternetCafe\apps\backend\InternetCafe.Backend" && dotnet run --urls "http://127.0.0.1:5055""
timeout /t 3 /nobreak >nul
echo       -^> Backend API đang chạy tại http://127.0.0.1:5055
echo.

echo [3/4] Đang khởi chạy Web Admin Quản lý (Cổng 5173)...
start "InternetCafe - Web Admin (5173)" cmd /k "chcp 65001 >nul && cd /d "%~dp0InternetCafe\apps\admin-web" && npm run dev:shared"
timeout /t 2 /nobreak >nul
start http://127.0.0.1:5173
echo       -^> Web Admin đã mở tại http://127.0.0.1:5173
echo.

echo [4/4] Đang khởi chạy Desktop Client (Máy trạm phòng NET)...
start "InternetCafe - Desktop Client" cmd /k "chcp 65001 >nul && cd /d "%~dp0InternetCafe\apps\desktop-client" && dotnet run --project InternetCafe.Client/InternetCafe.Client.csproj"
echo       -^> Cửa sổ Desktop Client đã khởi động.
echo.

echo ============================================================
echo   KHỞI CHẠY THÀNH CÔNG TOÀN BỘ HỆ THỐNG!
echo.
echo   * Backend API:    http://127.0.0.1:5055
echo   * Web Admin:      http://127.0.0.1:5173
echo   * Desktop Client: Cửa sổ máy trạm Kiosk Client
echo.
echo   THÔNG TIN TÀI KHOẢN TRẢI NGHIỆM (Mật khẩu chung: Demo@123):
echo     1. Quản trị hệ thống:     admin
echo     2. Quản lý cơ sở:         manager
echo     3. Thu ngân tại quầy:     cashier
echo     4. Nhân viên trực ca:     staff
echo     5. Khách hàng chơi máy:   gamer (Nguyễn Văn An)
echo ============================================================
echo.
echo Nhấn phím bất kỳ để đóng cửa sổ thông báo này (các dịch vụ vẫn tiếp tục chạy).
pause >nul
