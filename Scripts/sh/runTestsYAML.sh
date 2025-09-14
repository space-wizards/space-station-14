cd ../../

mkdir Scripts/logs

rm Scripts/logs/Content.YAMLLinter.log
dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj -c DebugOpt -- NUnit.ConsoleOut=0 > Scripts/logs/Content.YAMLLinter.log

echo "Tests complete. Press enter to continue."
read
