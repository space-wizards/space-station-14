@echo off
set PDIR=%~dp0
cd %PDIR%RobustToolbox\Bin\Client
start Robust.Client.exe %*
cd %PDIR%
set PDIR=
