@echo off
set PDIR=%~dp0
cd %PDIR%RobustToolbox\Bin\Client
start SS14.Client.exe %*
cd %PDIR%
set PDIR=
