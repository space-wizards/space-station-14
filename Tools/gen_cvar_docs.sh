#!/bin/bash
set -e
# Navigate to script directory
cd "$(dirname "$0")/CVarDocGen"
# Run the tool (pass multiple source paths; repeat --src as needed)
dotnet run --project . -- --src ../../Content.Shared/CCVar --src ../../RobustToolbox/Robust.Shared --out ./out
