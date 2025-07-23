#!/bin/sh
dotnet run --project Content.Europa.AGPL.Client --configuration Tools
dotnet run --project Content.Europa.MIT.Client --configuration Tools
read -p "Press enter to continue"
