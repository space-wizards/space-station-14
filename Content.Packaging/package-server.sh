#!/usr/bin/env bash
# Builds standalone server packages without bundling a client for delivery.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
dotnet run --project "$SCRIPT_DIR/Content.Packaging.csproj" server --platform current --content-root "$SCRIPT_DIR/.." "$@"
exit_code=$?
read -r -p "Press enter to continue"
exit "$exit_code"
