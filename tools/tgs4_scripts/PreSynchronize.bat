@echo off

powershell -NoProfile -ExecutionPolicy Bypass -File PreSynchronize.ps1 -game_path %1
