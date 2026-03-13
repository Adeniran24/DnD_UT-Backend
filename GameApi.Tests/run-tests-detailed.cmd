@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-tests-detailed.ps1" %*
set "EXITCODE=%ERRORLEVEL%"
echo.
pause
exit /b %EXITCODE%
