@echo off
rem Builds the client content package used by the launcher, CDN manifests, and Hybrid ACZ server delivery.
setlocal
dotnet run --project "%~dp0Content.Packaging.csproj" client --content-root "%~dp0.." %*
set EXIT_CODE=%ERRORLEVEL%
pause
exit /b %EXIT_CODE%
