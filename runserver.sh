#!/bin/sh
dotnet run --project Content.Server --configuration Release -- --config-file ../server_config.toml
read -p "Press enter to continue"

