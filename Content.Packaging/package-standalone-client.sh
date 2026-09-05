#!/usr/bin/env bash
# Builds a standalone client package for people who want a runnable client zip.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
dotnet run --project "$SCRIPT_DIR/Content.Packaging.csproj" client --standalone --content-root "$SCRIPT_DIR/.." "$@"
exit_code=$?
read -r -p "Press enter to continue"
exit "$exit_code"
