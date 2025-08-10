@echo off
cd ../../

call dotnet run --project Content.Client --no-build %*

pause
