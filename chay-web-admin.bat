@echo off
title Chạy Web Admin (React + Vite)
chcp 65001 >nul
cd /d "%~dp0InternetCafe\apps\admin-web"
echo ============================================================
echo   KHỞI CHẠY GIAO DIỆN QUẢN TRỊ PHÒNG NET (WEB ADMIN)
echo   Địa chỉ: http://127.0.0.1:5173
echo ============================================================
echo.
npm run dev
pause
