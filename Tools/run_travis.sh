#!/usr/bin/env bash

set -e

# Use manually installed .NET.
# Travis is shitting itself. Wonderful.
PATH="~/.dotnet:$PATH"

dotnet build SpaceStation14.sln /p:Python=python3.5
dotnet test Content.Tests/Content.Tests.csproj
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj
