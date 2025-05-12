#!/bin/sh
dotnet run --project Content.Server --configuration Release -- --config-file /home/shmeegoid/shmeeg-station/shmeeg-station-14/server_config.toml
read -p "Press enter to continue"

