cd ../../

mkdir Scripts/logs

rm Scripts/logs/Content.Tests.log
dotnet test Content.Tests/Content.Tests.csproj -c DebugOpt -- NUnit.ConsoleOut=0 > Scripts/logs/Content.Tests.log

echo "Tests complete. Press enter to continue."
read
