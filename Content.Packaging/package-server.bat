@echo off
rem Builds standalone server packages without bundling a client for delivery.
setlocal
dotnet run --project "%~dp0Content.Packaging.csproj" server --platform current --content-root "%~dp0.." %*
set EXIT_CODE=%ERRORLEVEL%
pause
exit /b %EXIT_CODE%
