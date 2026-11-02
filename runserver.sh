#!/usr/bin/env bash
# Default: Tools (matches client; use run*-Tools.sh aliases also exist)
dotnet run --project Content.Server --configuration Tools
read -p "Press enter to continue"
