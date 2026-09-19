#!/usr/bin/env bash
# Builds the client content package used by the launcher, CDN manifests, and Hybrid ACZ server delivery.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
dotnet run --project "$SCRIPT_DIR/Content.Packaging.csproj" client --content-root "$SCRIPT_DIR/.." "$@"
exit_code=$?
read -r -p "Press enter to continue"
exit "$exit_code"
