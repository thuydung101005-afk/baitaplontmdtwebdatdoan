@echo off
chcp 65001 >nul
title Bật Dịch Vụ SQL Server (FoodieDB)

:: Kiểm tra quyền Admin, nếu chưa có thì tự động xin quyền Admin
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Đang yêu cầu quyền Quản trị viên ^(Administrator^) để bật SQL Server...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo ==============================================================
echo        ĐANG KHỞI ĐỘNG DỊCH VỤ SQL SERVER TRÊN MÁY
echo ==============================================================
echo.

sc config "MSSQL$SQLEXPRESS01" start= auto >nul 2>&1
net start "MSSQL$SQLEXPRESS01"

sc config "MSSQL$SQLEXPRESS" start= auto >nul 2>&1
net start "MSSQL$SQLEXPRESS"

sc config "MSSQL$SQLEXPRESS02" start= auto >nul 2>&1
net start "MSSQL$SQLEXPRESS02"

net start "SQLBrowser" >nul 2>&1

echo.
echo ==============================================================
echo   ĐÃ BẬT SQL SERVER THÀNH CÔNG!
echo   Bây giờ bạn có thể quay lại Visual Studio và bấm F5 để chạy.
echo ==============================================================
pause

