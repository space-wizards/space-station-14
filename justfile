# --------------------------------------------------------------------------------------------------------------------
# Space Station 14 JustFile
# --------------------------------------------------------------------------------------------------------------------
# This is a file containing commands for Just. You can find Just here: https://github.com/casey/just
# Just is a command runner that automates needing to remember common terminal commands, similar to Make's makefiles.
# If you don't want to use Just, this file also serves as a handy reference for common console commands.
# --------------------------------------------------------------------------------------------------------------------

# This sets the terminal used on Windows.
# This file path is the default installation location Git For Windows will install Git Bash to.
# Given Space Station 14 is a Git-backed project, and you somehow have these files, we assume 
# you have Git installed. And if you have Git, you probably installed it via Git For Windows
# (https://git-scm.com/install/windows).
# If you don't have Git Bash, or it's not in this location, try deleting this line, but Just
# may not work for you.
set windows-shell := ["C:\\Program Files\\Git\\bin\\sh.exe","-c"]

# Build everything. Woe, warnings be upon ye.
build:
    dotnet build

# Build everything but only show errors.
build-no-warnings:
    dotnet build --property WarningLevel=0

# Build and run the server.
build-and-run-server:
    just build-and-run Content.Server

# Build and run the client.
build-and-run-client:
    just build-and-run Content.Client

# Build and run the specified project.
build-and-run +PROJECT:
    dotnet run --project {{PROJECT}}

# Run the server. 
run-server:
    just run Content.Server

# Run the client.
run-client:
    just run Content.Client

# Build and run the game. Woe, warnings be upon ye.
build-and-run-game:
    just build
    just run-game

# Build and run the game, but only show errors.
build-and-run-game-no-warnings:
    just build-no-warnings
    just run-game

# Run the game (without building it). Woe, warnings be upon ye.
# This is a parallel command - the just commands listed run at the same time.
[parallel]
run-game: run-server run-client

# Run the game (without building it), only showing errors.
# This is a parallel command - the just commands listed run at the same time.
[parallel]
run-game-no-warnings: run-server-no-warnings run-client-no-warnings

# Runs the specified project. Woe, warnings be upon ye.
run +PROJECT:
    dotnet run --project {{PROJECT}} --no-build

# Runs the specified project, only showing errors.
run-no-warnings +PROJECT:
    dotnet run --project {{PROJECT}} --no-build --property WarningLevel=0

# Run every integration test.
test-all:
    dotnet test --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj -- NUnit.MapWarningTo=Failed

# Run the sandbox validation test.
test-sandbox:
    just test SandboxTest

# Run a particular test. Supply the name of the test's class.
test +TEST_NAME:
    dotnet test --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj --filter {{TEST_NAME}} -- NUnit.MapWarningTo=Failed

# Run the YAML linter.
lint-yaml:
    dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj

# Builds (and runs) packaging for the specified platform. Woe, warnings be upon ye.
# The platforms are: win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64
# This list may be out of date. See Content.Packaging/ServerPackaging.cs for the current list of build targets.
build-packaging +PLATFORM:
    dotnet run --project Content.Packaging server --hybrid-acz --platform {{PLATFORM}}

# Builds (and runs) packaging for the specified platform, only displaying errors.
# The platforms are: win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64
# This list may be out of date. See Content.Packaging/ServerPackaging.cs for the current list of build targets.
build-packaging-no-warnings +PLATFORM:
    dotnet run --property WarningLevel=0 --project Content.Packaging server --hybrid-acz --platform {{PLATFORM}}

# Runs packaging for the specified platform. Woe, warnings be upon ye.
# The platforms are: win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64
# This list may be out of date. See Content.Packaging/ServerPackaging.cs for the current list of build targets.
run-packaging +PLATFORM:
    dotnet run --no-build --project Content.Packaging server --hybrid-acz --platform {{PLATFORM}}

# Runs packaging for the specified platform, only displaying errors.
# The platforms are: win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64
# This list may be out of date. See Content.Packaging/ServerPackaging.cs for the current list of build targets.
run-packaging-no-warnings +PLATFORM:
    dotnet run --property WarningLevel=0 --no-build --project Content.Packaging server --hybrid-acz --platform {{PLATFORM}}
