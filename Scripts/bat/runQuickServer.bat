@echo off
cd ../../

call dotnet run --project Content.Server --no-build %*

pause
