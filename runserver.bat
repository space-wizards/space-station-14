@echo off
set PDIR=%~dp0
cd %PDIR%RobustToolbox\Bin\Server
call SS14.Server.exe %*
cd %PDIR%
set PDIR=
pause
