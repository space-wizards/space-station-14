@echo off
set PDIR=%~dp0
cd %PDIR%Bin\Content.Client
call Content.Client.exe %* > "../../client_output.txt"
cd %PDIR%
set PDIR=
pause