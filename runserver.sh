#!/bin/sh
dotnet run --project Content.Server --config-file ../server_config.toml
read -p "Press enter to continue"
