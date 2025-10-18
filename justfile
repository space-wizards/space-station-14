set windows-shell := ["C:\\Program Files\\Git\\bin\\sh.exe","-c"]

build:
    dotnet build --property WarningLevel=0

build-and-run-server:
    just build-and-run Content.Server

build-and-run-client:
    just build-and-run Content.Client

build-and-run +PROJECT:
    dotnet run --project {{PROJECT}}

run-server:
    just run Content.Server

run-client:
    just run Content.Client

build-and-run-game:
    just build
    just run-game

[parallel]
run-game: run-server run-client

run +PROJECT:
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

build-packaging:
    dotnet run --project Content.Packaging server --hybrid-acz --platform linux-x64

run-packaging:
    dotnet run --property WarningLevel=0 --no-build --project Content.Packaging server --hybrid-acz --platform linux-x64

build-and-run-packaging: build-packaging run-packaging
