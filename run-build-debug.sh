#!/usr/bin/env bash
dotnet build --configuration Debug
read -p "Press enter to continue"
./run-nobuild.sh