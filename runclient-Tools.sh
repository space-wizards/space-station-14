#!/bin/sh
dotnet build -c Release RobustToolbox/Robust.Client.Injectors
dotnet run --project Content.Client --configuration Tools
read -p "Press enter to continue"
