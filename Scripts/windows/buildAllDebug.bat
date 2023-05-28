@echo off
cd ../../
call python RUN_THIS.py
call git submodule update --init --recursive
call dotnet build -c Debug
pause
