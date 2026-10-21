@echo off
chcp 65001 >nul
echo ========================================================
echo   CHẠY KIỂM THỬ TỰ ĐỘNG GIAO DIỆN PLAYWRIGHT E2E
echo   InternetCafe Management System - Admin Web
echo ========================================================
echo.

cd InternetCafe\apps\admin-web
echo [1/2] Đang thực thi Playwright E2E Tests...
call npx playwright test

echo.
echo [2/2] Hoàn tất kiểm thử Playwright!
echo Báo cáo HTML đã được tạo tại: InternetCafe\apps\admin-web\playwright-report\index.html
echo.
echo Gõ 'npx playwright show-report' để xem báo cáo trực quan trong trình duyệt.
echo ========================================================
pause
