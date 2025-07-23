#!/bin/sh
dotnet run --project Content.Europa.AGPL.Server --configuration Tools
dotnet run --project Content.Europa.MIT.Server --configuration Tools
read -p "Press enter to continue"
