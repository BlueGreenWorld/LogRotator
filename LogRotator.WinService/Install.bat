@echo off
echo Installing LogRotator Service
echo.
call "C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe" -i "LogRotator.WinService.exe"
echo.
echo Starting LogRotator Service
echo.
call net start LogRotator
pause
