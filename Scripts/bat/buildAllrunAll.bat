@echo off
cd ../../
REM Build All
call git submodule update --init --recursive
call dotnet build -c Debug

REM Run server + Client
cd Scripts/bat/
start runQuickServer.bat %*
start runQuickClient.bat %*
pause