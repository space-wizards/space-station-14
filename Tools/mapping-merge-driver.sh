#!/usr/bin/env bash

# Add this to .git/config:
# [merge "mapping-merge-driver"]
#         name = Merge driver for maps
#         driver = Tools/mapping-merge-driver.sh %A %O %B

dotnet run --project ./Content.Tools "$@"

