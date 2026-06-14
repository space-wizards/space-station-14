#!/usr/bin/env bash
dotnet build --configuration Release
read -p "Press enter to continue"
./run-nobuild.sh