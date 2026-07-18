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

# -----------------------------
# Building and running the game
# -----------------------------

# Build everything.
[group("Building the game")]
build PROJECT="" CONFIG="Debug" PLATFORM="" WARNINGLEVEL="4" NORESTORE="false" *BUILDFLAGS:
    dotnet build {{ PROJECT }} --configuration {{ CONFIG }} {{ if PLATFORM != "" { "--platform " + PLATFORM } else { "" } }} {{ if NORESTORE == "true" { "--no-restore" } else { "" } }} --property WarningLevel={{ WARNINGLEVEL }} --property GenerateFullPaths=true --consoleLoggerParameters:'ForceNoAlign;NoSummary' {{ BUILDFLAGS }}

# Runs the specified project.
[group("Running the game")]
run PROJECT BUILD="false" WARNINGLEVEL="4" *BUILDFLAGS:
    dotnet run --project {{ PROJECT }} {{ if BUILD != "true" { "--no-build" } else { "" } }} --property WarningLevel={{ WARNINGLEVEL }} {{ BUILDFLAGS }}

# Build and run the specified project. This always runs with implicit restore enabled.
[group("Building the game")]
build-and-run PROJECT CONFIG="Debug" WARNINGLEVEL="4" *BUILDFLAGS: (run PROJECT "true" CONFIG WARNINGLEVEL BUILDFLAGS)

# Run the client.
[group("Running the game")]
run-client BUILD="false" WARNINGLEVEL="4" *BUILDFLAGS: (run "Content.Client" BUILD WARNINGLEVEL BUILDFLAGS)

# Build and run the client.
[group("Building the game")]
build-and-run-client CONFIG="Debug" WARNINGLEVEL="4" *BUILDFLAGS: (run-client "true" CONFIG WARNINGLEVEL BUILDFLAGS)

# Run the server.
[group("Running the game")]
run-server BUILD="false" WARNINGLEVEL="4" *BUILDFLAGS: (run "Content.Server" "false" BUILD WARNINGLEVEL BUILDFLAGS)

# Build and run the server.
[group("Building the game")]
build-and-run-server CONFIG="Debug" WARNINGLEVEL="4" *BUILDFLAGS: (run-server "true" CONFIG WARNINGLEVEL BUILDFLAGS)

# Run the client and the server in parallel.
[group("Running the game"), parallel]
run-game: run-server run-client

# Builds and runs both the server and client at the same time.
[group("Building the game")]
build-and-run-game CONFIG="Debug" WARNINGLEVEL="4" *BUILDFLAGS: (build "" CONFIG "" WARNINGLEVEL "false" BUILDFLAGS)
    just run-game {{ WARNINGLEVEL }}

# Builds the YAML linter.
[group("YAML linter")]
build-yaml-linter CONFIG="Debug" PLATFORM="" WARNINGLEVEL="4" NORESTORE="false" *BUILDFLAGS: (build "Content.YAMLLinter/Content.YAMLLinter.csproj" CONFIG PLATFORM WARNINGLEVEL NORESTORE BUILDFLAGS)

# Runs the YAML linter.
[group("YAML linter")]
run-yaml-linter BUILD="false" WARNINGLEVEL="4" *BUILDFLAGS: (run "Content.YAMLLinter/Content.YAMLLinter.csproj" BUILD WARNINGLEVEL BUILDFLAGS)

# Builds packaging for the specified platform. The platforms are: win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64.
[group("Packaging")]
build-packaging CONFIG="Debug" PLATFORM="" WARNINGLEVEL="4" NORESTORE="false" *BUILDFLAGS: (build "Content.Packaging" CONFIG PLATFORM WARNINGLEVEL NORESTORE BUILDFLAGS)

# Runs packaging for the specified platform. This runs using ACZ. The platforms are: win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64.
[group("Packaging")]
run-packaging-server PLATFORM BUILD="false" WARNINGLEVEL="4" WITHACZ="true" *BUILDFLAGS:
    # The hybrid-acz and platform properties are unique to Content.Packaging.
    dotnet run {{ if BUILD != "true" { "--no-build" } else { "" } }} --project Content.Packaging server {{ if WITHACZ == "true" { "--hybrid-acz" } else { "" } }} --platform {{ PLATFORM }} --property WarningLevel={{ WARNINGLEVEL }} {{ BUILDFLAGS }}

# Runs packaging for the specified platform without using ACZ. The platforms are: win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, and osx-arm64.
[group("Packaging")]
run-packaging-server-no-acz PLATFORM BUILD="false" WARNINGLEVEL="4" *BUILDFLAGS: (run-packaging-server PLATFORM BUILD WARNINGLEVEL "false" BUILDFLAGS)

# Runs packaging for the client.
[group("Packaging")]
run-packaging-client BUILD="false" WARNINGLEVEL="4" *BUILDFLAGS:
    # The no-wipe-release property is unique to Content.Packaging.
    dotnet run {{ if BUILD != "true" { "--no-build" } else { "" } }} --project Content.Packaging client --no-wipe-release --property WarningLevel={{ WARNINGLEVEL }} {{ BUILDFLAGS }}

# Builds the map renderer.
[group("Map renderer")]
build-map-renderer CONFIG="Debug" PLATFORM="" WARNINGLEVEL="4" NORESTORE="false" *BUILDFLAGS: (build "Content.MapRenderer" CONFIG PLATFORM WARNINGLEVEL NORESTORE BUILDFLAGS)

# Runs the map renderer.
[group("Map renderer")]
run-map-renderer MAP="Dev" BUILD="false" WARNINGLEVEL="4" *BUILDFLAGS:
    dotnet run {{ if BUILD != "true" { "--no-build" } else { "" } }} --project Content.MapRenderer {{ MAP }} --property WarningLevel={{ WARNINGLEVEL }} {{ BUILDFLAGS }}

# ---------
# Tests
# ---------

# Run a test project. Provide the filter flag if you want to filter to run specific tests.
[group("Running tests")]
run-tests TESTPROJ FILTER="" BUILD="false" CONFIG="DebugOpt" WARNINGLEVEL="4" *NUNITFLAGS:
    dotnet test {{ TESTPROJ }} {{ if BUILD != "true" { "--no-build" } else { "" } }} --configuration {{ CONFIG }} {{ if FILTER != "" { " --filter " + FILTER } else { "" } }} --property WarningLevel={{ WARNINGLEVEL }} {{ if NUNITFLAGS != "" { " -- " + NUNITFLAGS } else { "" } }}

# Run the unit test project, with some default NUnit flags. Provide the filter flag if you want to filter to run specific tests.
[group("Running tests")]
run-unit-tests FILTER="" CONFIG="DebugOpt" BUILD="false" WARNINGLEVEL="4" *NUNITFLAGS: (run-tests "Content.Tests/Content.Tests.csproj" FILTER BUILD CONFIG WARNINGLEVEL "NUnit.ConsoleOut=0" "NUnit.MapWarningTo=Failed.ConsoleOut=0" "NUnit.MapWarningTo=Failed" NUNITFLAGS)

# Run the integration test project, with some default NUnit flags. Provide the filter flag if you want to filter to run specific tests.
[group("Running tests")]
run-integration-tests FILTER="" CONFIG="DebugOpt" BUILD="false" WARNINGLEVEL="4" *NUNITFLAGS: (run-tests "Content.IntegrationTests/Content.IntegrationTests.csproj" FILTER BUILD CONFIG WARNINGLEVEL "NUnit.ConsoleOut=0" "NUnit.MapWarningTo=Failed.ConsoleOut=0" "NUnit.MapWarningTo=Failed" NUNITFLAGS)

# Run the sandbox validation test.
[group("Running tests")]
run-sandbox-test CONFIG="DebugOpt" BUILD="false" WARNINGLEVEL="4" *NUNITFLAGS: (run-integration-tests "SandboxTest" CONFIG BUILD WARNINGLEVEL NUNITFLAGS)

# -----
# Tools
# -----

# Does a restore of Dotnet dependencies. Used in CI so that fetching dependencies only happens once during a workflow.
[group("Helper commands - CI")]
install-dependencies:
    dotnet restore

# Fetches RT which populates the Git tag information in the repo.
[group("Helper commands - CI")]
get-engine-tag:
    cd RobustToolbox
    git fetch --depth=1

# Sets up the project on a new development machine. Requires Python 3 to be installed.
[group("Helper commands - Development")]
setup-project:
    py RUN_THIS.py

# ------------
# Git commands
# ------------

# Initializes and updates your submodules.
[group("Git")]
update-submodules:
    git submodule update --init --recursive

# Initializes and updates specifically the Robust Toolbox submodules
[group("Git")]
update-rt-submodules:
    cd RobustToolbox/
    git submodule update --init --recursive

[group("Git")]
add-remote NAME REMOTE:
    git remote add {{ NAME }} {{ REMOTE }}

# Creates a remote called upstream that points to some remote repo. Used when working on a downstream codebase.
[group("Git")]
add-upstream-remote REMOTE: (add-remote "upstream" REMOTE)

# Creates a remote called upstream that points to Wizden. Used when working on the Wizden codebase, or if you're a downstream wanting to pull from Wizden.
[group("Git")]
add-upstream-remote-to-wizden: (add-upstream-remote "https://github.com/Space-Wizards/space-station-14.git")

# Updates your current branch with the latest changes on your upstream.
[group("Git")]
get-latest-changes:
    git pull upstream master
