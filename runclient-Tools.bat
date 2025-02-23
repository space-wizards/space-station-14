@echo off
dotnet build -c Release RobustToolbox\Robust.Client.Injectors
dotnet run --project Content.Client --configuration Tools
pause