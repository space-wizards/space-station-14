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
set windows-shell := ["C:\\Program Files\\Git\\bin\\sh.exe", "-c"]
# --------------------------------------------------------------------------------------------------------------------
# Windows installation instructions
# --------------------------------------------------------------------------------------------------------------------
# On Windows, you can install Just with winget:
# `winget install Casey.Just`
# To use Just's --choose option, you can also install fzf:
# `winget install fzf`
# Software development environments like VS Code and Rider have plugins for Just support.
# To install and use these, refer to their own installation instructions.
# -----------------
# Building the game
# -----------------
# Build everything.
build:
    dotnet build
# Build everything but only show errors.
build-no-warnings:
    dotnet build --property WarningLevel=0
# -----------------------------
# Building-and-running the game
# -----------------------------
# Build and run the specified project.
build-and-run +PROJECT:
    dotnet run --project {{ PROJECT }}
# Build and run the server.
build-and-run-server:
    just build-and-run Content.Server
# Build and run the client.
build-and-run-client:
    just build-and-run Content.Client
# Build and run the specified project.
build-and-run-no-warnings +PROJECT:
    dotnet run --project {{ PROJECT }} --property WarningLevel=0
# Build and run the server.
build-and-run-server-no-warnings:
    just build-and-run-no-warnings Content.Server
# Build and run the client.
build-and-run-client-no-warnings:
    just build-and-run-no-warnings Content.Client
    # Build and run the game.
build-and-run-game:
    just build
    just run-game
# Build and run the game, but only show errors.
build-and-run-game-no-warnings:
    just build-no-warnings
    just run-game
# -----------------------------
# Running the game
# -----------------------------
# Runs the specified project.
run +PROJECT:
    dotnet run --project {{ PROJECT }} --no-build
# Runs the specified project, only showing errors.
run-no-warnings +PROJECT:
    dotnet run --project {{ PROJECT }} --no-build --property WarningLevel=0
# Run the client.
run-client:
    just run Content.Client
# Run the server.
run-server:
    just run Content.Server
# Run the client and the server.
[parallel]
run-game: run-server run-client
# Run the server without warnings.
run-server-no-warnings:
    just run-no-warnings Content.Server
# Run the client without warnings.
run-client-no-warnings:
    just run-no-warnings Content.Client
# Run the client and the server.
[parallel]
run-game-no-warnings: run-server-no-warnings run-client-no-warnings
# ---------
# Tests
# ---------
# Run every integration test.
test-all:
    dotnet test --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj -- NUnit.MapWarningTo=Failed
# Run the sandbox validation test.
test-sandbox:
    just test SandboxTest
# Run a particular test. Supply the name of the test's class.
test +TEST_NAME:
    dotnet test --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj --filter {{ TEST_NAME }} -- NUnit.MapWarningTo=Failed
# -----
# Tools
# -----
# Run the YAML linter.
lint-yaml:
    dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj
# Builds (and runs) packaging for the specified platform.
# The platforms are: win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64
# This list may be out of date. See Content.Packaging/ServerPackaging.cs for the current list of build targets.
build-packaging +PLATFORM:
    dotnet run --project Content.Packaging server --hybrid-acz --platform {{ PLATFORM }}
# Builds (and runs) packaging for the specified platform, only displaying errors.
# The platforms are: win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64
# This list may be out of date. See Content.Packaging/ServerPackaging.cs for the current list of build targets.
build-packaging-no-warnings +PLATFORM:
    dotnet run --property WarningLevel=0 --project Content.Packaging server --hybrid-acz --platform {{ PLATFORM }}
# Runs packaging for the specified platform.
# The platforms are: win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64
# This list may be out of date. See Content.Packaging/ServerPackaging.cs for the current list of build targets.
run-packaging +PLATFORM:
    dotnet run --no-build --project Content.Packaging server --hybrid-acz --platform {{ PLATFORM }}
# Runs packaging for the specified platform, only displaying errors.
# The platforms are: win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64
# This list may be out of date. See Content.Packaging/ServerPackaging.cs for the current list of build targets.
run-packaging-no-warnings +PLATFORM:
    dotnet run --property WarningLevel=0 --no-build --project Content.Packaging server --hybrid-acz --platform {{ PLATFORM }}
