@echo off
set PDIR=%~dp0
cd %PDIR%RobustToolbox\Bin\Server
call Robust.Server.exe %*
cd %PDIR%
set PDIR=
pause
