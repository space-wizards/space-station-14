@echo off
set PDIR=%~dp0
cd %PDIR%Bin\Client
start SS14.Client.exe %*
cd %PDIR%
set PDIR=
