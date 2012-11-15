@echo off
echo Uninstalling LogRotator Service
echo.
call "C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe" -u "LogRotator.WinService.exe"
echo.
pause
