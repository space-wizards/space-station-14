# Milestone 0 development log

Date: 2026-11-11 (America/Los_Angeles). **Milestone 0 completion criteria verified. Milestone 1 has not begun and requires owner approval.**

## Foundation established

- Inspected the workspace first: pre-existing empty parent Git repository and empty `SS14/` directory. Preserved the parent repository and cloned official SS14 into `SS14/`; no unrelated projects changed.
- Created [Chaunsy19/persistent-space-city](https://github.com/Chaunsy19/persistent-space-city), a separate GitHub fork. Local `origin` points there; `upstream` points to `https://github.com/space-wizards/space-station-14.git`.
- Working branch: `codex/milestone-0`. Documentation changes remain local unless explicitly published later; creating the fork did not publish custom gameplay or these local changes.
- Existing dependencies: Git 2.55.0.windows.3, Python 3.11.9, .NET SDK 10.0.102 (x64 Windows). No installers run. `global.json` requests 10.0.100 with `latestFeature`, accepting installed 10.0.102.
- Ran `git submodule update --init --recursive` and official `python RUN_THIS.py`; setup installed repository hooks and confirmed submodules. Subsequent status verification found all five submodules at recorded revisions, with no uninitialized/mismatched markers.

| Repository/submodule | Tested commit |
| --- | --- |
| SS14 | `9ccfa8d118307aced564fa824d6ad5b9e217847a` |
| RobustToolbox | `d56b0d0bcf94c76bef7fc18625bf226607dca092` |
| Lidgren.Network | `4a111ba2e392bc0adcf2b724999c40cbbaae4662` |
| Robust.LoaderApi | `2b6a7db8d85166b345302e446a8492a213fc92aa` |
| XamlX | `8d9b5a8c670cf410308f85e3914373d848ccf698` |
| cefglue | `f8f5135dbcb6d1c1638043966d7a7c620676b5f8` |

## Verification results

| Criterion/test | Observed result |
| --- | --- |
| Repository/fork initialized | PASS: official history cloned, personal fork created, remotes separated |
| Required SDK and setup dependencies | PASS: existing versions compatible; NuGet restore completed |
| Recursive submodules | PASS: all pinned commits verified |
| `dotnet restore SpaceStation14.slnx --verbosity minimal` | PASS, exit 0; advisory/pruning warnings recorded below |
| `dotnet build SpaceStation14.slnx --no-restore --verbosity minimal` | PASS, exit 0; 1,025 warnings, 0 errors; elapsed 00:01:15.72 |
| Local server launch | PASS: `Server Version 295.0.3.0 -> Ready`; Sandbox development station loaded |
| Local-only listener | PASS: server UDP endpoint confirmed `127.0.0.1:1212`; hub advertising explicitly disabled |
| Graphical client launch | PASS: actual game window rendered the development map, character, HUD and welcome message |
| Client/server connection | PASS: server approved `localhost@CityDev`; client received Full GameState, reached `InGame`, attached local player to a `MobHuman`, and entered `GameplayState`; corroborated visually |
| NPC/job/persistence investigation | PASS: source findings and future design recorded in `ARCHITECTURE.md`; two fork organization strategies inspected |
| Documentation/rules | PASS: four requested documents plus repository `AGENTS.md` and workspace navigation README |
| No gameplay changes | PASS: only documentation and local-data ignore rule added; engine and gameplay source untouched |

This is a development smoke test on the upstream Sandbox/Dev setup, not a production hosting, persistence, multi-client, endurance, or autonomous-worker test. No gameplay tests were added and the full automated test suite was not run for this documentation/setup-only milestone. No completion criterion remains unverified at the time of this log.

Local evidence files (ignored by Git): `milestone0-restore.log`, `milestone0-build.log`, `milestone0-server.log`, `milestone0-client.log`. Preserve these when diagnosing this particular build. Runtime SQLite/server data lives in `.space-city-local/server/`, explicitly ignored. Client uses `--self-contained`, so its data stays relative to the built client instead of a user-global profile.

At handoff, the local server and client were left running for the owner to inspect. Observed process IDs were server **20660**, client **1856**; these IDs are session-specific and must be rechecked before any later process operation. No public hosting, router configuration, firewall changes, or hub registration was performed.

## Repeatable development commands

Run from `C:\Users\devin\Documents\ChatGPT\SS14 City Sim\SS14`, not the parent workspace:

```powershell
git status --short
git submodule status --recursive
python RUN_THIS.py
dotnet restore SpaceStation14.slnx
dotnet build SpaceStation14.slnx --no-restore
```

For a new test session, stop the existing test processes first to avoid a port conflict. Open two PowerShell consoles in the checkout. In the first:

```powershell
dotnet run --project Content.Server --no-build --no-restore -- --data-dir .space-city-local/server --cvar net.bindto=127.0.0.1 --cvar hub.advertise=false
```

After the server reports Ready, in the second:

```powershell
dotnet run --project Content.Client --no-build --no-restore -- --self-contained --username CityDev --connect --connect-address ss14://127.0.0.1:1212
```

These use the same official `Content.Server`/`Content.Client` development entry points as `runserver.bat`/`runclient.bat`, with flags for local binding, isolated data and automatic connection. Alternatively launch the client normally and use Direct Connect to `127.0.0.1:1212`. On the unmodified Debug setup, the Sandbox map spawned the test player automatically and granted local host administration; this is development behavior, not a production access policy. Close the client normally; for a server launched in your own console, use its `shutdown` command and wait for exit. Rebuild after code changes before using `--no-build`.

## Known issues and limits

- Restricted-shell network access initially caused `Could not resolve host: github.com`; Git's MSYS helper also reported `NtCreateDirectoryObject ... 0xC0000022` in restricted mode. Network/setup/build/submodule verification succeeded through approved expanded execution. CLI authentication appeared invalid in restricted mode but succeeded with the existing Windows keyring outside that restriction; no login/token change was necessary. Do not mistake these environment restrictions for repository errors.
- Restore reported NU1903 high-severity advisories on `Microsoft.NETCore.App` 1.0.5 and `Microsoft.NETCore.Jit` 1.0.7 (legacy Pow3r dependency graph), and `SixLabors.ImageSharp` 3.1.12 (Pow3r/Robust benchmarks). Exact advisory URLs and affected project contexts are in the restore log. Packages were not upgraded or warnings suppressed. Review reachable dependency impact before any public deployment or use of those tools with untrusted inputs.
- Other upstream build warnings include obsolete APIs/analyzers, NU1510 package-pruning notices, and NETSDK1206 legacy runtime identifiers (Libuv). They did not prevent Debug build or launch. Release builds were not verified.
- Fresh SQLite migrations emitted EF warnings about historical `PRAGMA foreign_keys = 0` operations outside transactions. Initial migration and ongoing admin-log writes succeeded. These databases are local test data, not a tested city persistence store.
- Client logged a Discord IPC connection timeout; it was nonfatal and the game connected/rendered normally. Initial loading also emitted a main-loop catch-up warning. No steady-state performance baseline is claimed.
- Snapshot completeness, restart recovery, job automation, multi-grid commuting, zero-player endurance and population scaling remain future work, not Milestone 0 features.

## Decisions and next step

Use existing assemblies with `_SpaceCity` folders when custom work begins. Extend HTN, steering, hands and shared interaction systems. Do not modify RobustToolbox or duplicate simulation mechanics. Keep durable identity separate from transient plans and raw entity IDs. Proposed persistence combines audited engine serialization with versioned metadata and an explicit persistent-world lifecycle; it must be proven through restart tests in Milestone 6.

Changes: `.gitignore` (local runtime data only), `AGENTS.md`, `docs/space-city/PROJECT_VISION.md`, `ARCHITECTURE.md`, `ROADMAP.md`, and this log. The parent workspace `README.md` points future sessions to the actual game checkout. Official setup generated ignored hook/version/build artifacts. No C# or prototype gameplay files were changed; no custom gameplay directories were created.

Recommended next action: review these notes, then explicitly approve Milestone 1. Its first increment should be one server-controlled humanoid that navigates through an authorized door and performs a real item interaction using existing HTN, with clear blocked-path/cancellation behavior. Verify it from two clients before expanding to needs or professions. Do not start that implementation automatically.
