@echo off
rem Builds a standalone client package for people who want a runnable client zip.
setlocal
dotnet run --project "%~dp0Content.Packaging.csproj" client --standalone --content-root "%~dp0.." %*
set EXIT_CODE=%ERRORLEVEL%
pause
exit /b %EXIT_CODE%
