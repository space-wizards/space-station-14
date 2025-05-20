dotnet build Content.Client --configuration Release
dotnet build Content.Server --configuration Release

runclient-Release.bat&
runserver-Release.bat
