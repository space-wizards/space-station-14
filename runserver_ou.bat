@echo off
set PDIR=%~dp0
cd %PDIR%Bin\Content.Server
call Content.Server.exe %* > "../../server_output.txt"
cd %PDIR%
set PDIR=
pause
