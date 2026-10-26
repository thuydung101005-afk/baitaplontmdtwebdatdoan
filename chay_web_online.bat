@echo off
chcp 65001 >nul
title FOODIE - Máy chủ Website & Chia sẻ Online
color 0A

echo ==============================================================
echo           HỆ THỐNG MÁY CHỦ FOODIE - CHIA SẺ ONLINE
echo ==============================================================
echo.
echo [1/3] Đang khởi động Website Foodie trên máy tính...
start "Foodie Web Server (Đừng tắt)" /min cmd /c "cd /d "%~dp0baitaplontmdtwebdatdoan" && dotnet run"

echo.
echo Đang chờ máy chủ khởi động (khoảng 5 giây)...
timeout /t 5 /nobreak >nul

echo.
echo [2/3] Đang lấy địa chỉ IP mạng của bạn...
for /f "usebackq tokens=*" %%a in (`powershell -Command "(Invoke-RestMethod 'https://api.ipify.org') -replace '\s',''"`) do set "MY_IP=%%a"

echo.
echo [3/3] Đang tạo đường dẫn chia sẻ ra Internet...
echo.
echo ==============================================================
echo   MẬT KHẨU / IP XÁC NHẬN CỦA BẠN LÀ: %MY_IP%
echo   (Khi người khác mở web lần đầu, nhập số này vào ô rồi Submit)
echo ==============================================================
echo.
echo Đường dẫn Online của bạn sẽ xuất hiện bên dưới (chờ 3-5 giây):
echo --------------------------------------------------------------
npx -y localtunnel --port 5262

echo.
pause
