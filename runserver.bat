@echo off
set PDIR=%~dp0
cd %PDIR%Bin\Content.Server
call Content.Server.exe %*
cd %PDIR%
set PDIR=
pause
