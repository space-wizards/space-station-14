# -------------------------
# Space Station 14 JustFile
# -------------------------
# This is a file containing commands for Just. You can find Just here: https://github.com/casey/just
# Just is a command runner that automates needing to remember common terminal commands, similar to Make's makefiles.
# If you don't want to use Just, this file also serves as a handy reference for common console commands.
# -------------------------
# Windows Shell assignation
# -------------------------
# This sets the terminal used on Windows.
# This file path is the default installation location Git For Windows will install Git Bash to.
# Given Space Station 14 is a Git-backed project, and you somehow have these files, we assume
# you have Git installed. And if you have Git, you probably installed it via Git For Windows
# (https://git-scm.com/install/windows).
# If you don't have Git Bash, or it's not in this location, try deleting this line, but Just
# may not work for you.
set windows-shell := ["C:\\Program Files\\Git\\bin\\sh.exe", "-c"]
# ---------------------------------
# Windows installation instructions
# ---------------------------------
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
[group("build")]
build config="Debug" warninglevel="4":
    dotnet build --configuration {{ config }} --property WarningLevel={{ warninglevel }} --property GenerateFullPaths=true --consoleLoggerParameters:'ForceNoAlign;NoSummary'
# -----------------------------
# Building-and-running the game
# -----------------------------
# Build and run the specified project.
[group("build-and-run")]
build-and-run project config="Debug" warninglevel="4":
    dotnet run --project {{ project }} --configuration {{ config }} --property WarningLevel={{ warninglevel }}
# Build and run the server.
[group("build-and-run")]
build-and-run-server config="Debug" warninglevel="4":
    just build-and-run Content.Server {{ config }} {{ warninglevel }}
# Build and run the client.
[group("build-and-run")]
build-and-run-client config="Debug" warninglevel="4":
    just build-and-run Content.Client {{ config }} {{ warninglevel }}
[group("build-and-run")]
build-and-run-game config="Debug" warninglevel="4":
    just build {{ config }} {{ warninglevel }}
    just run-game
# ----------------
# Running the game
# ----------------
# Runs the specified project.
[group("run")]
run project warninglevel="4":
    dotnet run --project {{ project }} --no-build --property WarningLevel={{ warninglevel }}
# Run the client.
[group("run")]
run-client warninglevel="4":
    just run Content.Client {{ warninglevel }}
# Run the server.
[group("run")]
run-server warninglevel="4":
    just run Content.Server {{ warninglevel }}
# Run the client and the server.
[group("run")]
[parallel]
run-game warninglevel="4": (run-server warninglevel) (run-client warninglevel)
# ---------
# Tests
# ---------
[group("test")]
run-tests:
    dotnet test --no-build --configuration DebugOpt Content.Tests/Content.Tests.csproj -- NUnit.ConsoleOut=0
# Run every integration test.
[group("test")]
run-integration-tests:
    dotnet test --no-build --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed.ConsoleOut=0 NUnit.MapWarningTo=Failed
# Run the sandbox validation test.
[group("test")]
run-sandbox-test:
    just test SandboxTest
# Run a particular test. Supply the name of the test's class.
[group("test")]
run-integration-test testname:
    dotnet test --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj --filter {{ testname }} -- NUnit.MapWarningTo=Failed
# -----
# Tools
# -----
# Run the YAML linter.
[group("tools")]
build-yaml-linter:
    dotnet build --project Content.YAMLLinter/Content.YAMLLinter.csproj --property GenerateFullPaths=true --consoleLoggerParameters:'ForceNoAlign;NoSummary'
run-yaml-linter:
    dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj
# Builds (and runs) packaging for the specified platform. The platforms are: win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64. This list may be out of date. See Content.Packaging/ServerPackaging.cs for the current list of build targets.
[group("tools")]
build-packaging platform warninglevel="4":
    dotnet run --project Content.Packaging server --hybrid-acz --platform {{ platform }} --property WarningLevel={{ warninglevel }}
# Runs packaging for the specified platform. The platforms are: win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm6. This list may be out of date. See Content.Packaging/ServerPackaging.cs for the current list of build targets.
[group("tools")]
run-packaging platform warninglevel="4":
    dotnet run --no-build --project Content.Packaging server --hybrid-acz --platform {{ platform }} --property WarningLevel={{ warninglevel }}
[group("tools")]
setup-project:
    py RUN_THIS.py
# ------------
# Git commands
# ------------

# Initializes and updates your submodules.
[group("git shortcuts")]
update-submodules:
    git submodule update --init --recursive
# Creates a remote called upstream that points to Wizden. Change this if you're a downstream fork!
[group("git shortcuts")]
set-upstream-remote:
    git remote add upstream https://github.com/Space-Wizards/space-station-14.git
# Updates your current branch with the latest state of Wizden upstream.
[group("git shortcuts")]
pull-upstream-master:
    git pull upstream master
