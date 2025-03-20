#!/bin/sh
git pull
dotnet run --project Content.Packaging server --hybrid-acz --platform linux-x64
dotnet run --project Content.Server --config-file server_config.toml
