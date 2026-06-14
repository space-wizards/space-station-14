#!/usr/bin/env bash
dotnet build --configuration Tools
read -p "Press enter to continue"
./run-nobuild.sh