@echo off
dotnet build Content.Client --configuration Release
dotnet build Content.Server --configuration Release

Start "Client" "runclient-Release.bat"
Start "Server" "runserver-Release.bat"
