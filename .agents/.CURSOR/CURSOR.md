# CarpFinder — Cursor Code Instructions

CarpFinder (CF) is a fork of Space Station 14 built on RobustToolbox. It uses an ECS architecture with C# on both server and client.

## Editing and Making Documents

Whenever you understand how SS14 is built and structured, add or update docs under `.agents/.CURSOR/` (e.g. `ss14.md`). Warnings and errors to avoid are in `ss14rules.md`.

You may edit this document with any rules and information that make the project easier to work on.

Read and follow everything in this folder before making CF or upstream-touching changes.

Character builder + PF2e spawn/combat MVP notes: `character-builder.md`.

## Pathfinder Game Rules

CarpFinder is an action-based RPG based on the Pathfinder 2e system. Use https://2e.aonprd.com/ for rule questions (ancestries, classes, backgrounds, feats, skills, combat math, etc.).

## Building

The project takes ~5 minutes for a full build.

The raw build output is flooded with RobustToolbox warnings. To see only errors:

```bash
# Linux / bash
dotnet build -c Tools -clp:ErrorsOnly -nologo 2>&1 | grep -E ': error |Build succeeded|FAILED|Time Elapsed|[0-9]+ Error' | sort -u
```

```powershell
# PowerShell
$out = dotnet build -c Tools -clp:ErrorsOnly -nologo 2>&1; $out | Select-String -Pattern ": error |Build succeeded|FAILED|Time Elapsed|\d+ Error" | Select-Object -Unique
```

Never build only Shared: build Server, Client, or both via a normal `dotnet build`.

`MSB3027` / "file is locked" errors mean Content.Server or Content.Client is running and holding DLLs in `bin/`. Ask the user to close them; rebuilding will not help.

After XAML moves under `_CF/`, do a rebuild (not incremental) if you see `No precompiled XAML found for …`.

**XAML hot reload:** see `hot-reload.md`. Debug/Tools can JIT-reload `.xaml` on save, but **open lobby editors are cached** — restart the client (or rebuild + restart) to see `HumanoidProfileEditor` layout changes. Closing the character window is not enough.

```bash
# Default (Tools — XAML hot reload on) — two terminals, from repo root
dotnet run --project Content.Server -c Tools
dotnet run --project Content.Client -c Tools
# or: ./runserver.sh  and  ./runclient.sh
# Rider: Content Server+Client run config (Tools)
```

## Testing

Reuse the Tools build and target the tests you need; without `-c Tools --no-build` the test run rebuilds everything in Debug (~5 min):

```bash
dotnet test Content.IntegrationTests/Content.IntegrationTests.csproj -c Tools --no-build --filter "FullyQualifiedName~TestName"
```

## Searching

Repo-wide Grep times out (~54k files, mostly textures and maps). Always pass a path: `Content.Client`, `Content.Server`, `Content.Shared`, `Resources/Prototypes`, `Resources/Locale`, or `Resources/Maps` when maps are the target.

## C# Code Style

- File-scoped namespaces: `namespace Content.Shared.Example;`
- 4 spaces indentation (no tabs)
- Private fields: `_camelCase` (underscore prefix)
- Public members: `PascalCase`
- Local variables/parameters: `camelCase`
- Interfaces: `IPascalCase`, type parameters: `TPascalCase`
- Use `var` when type is apparent from the right side
- Allman braces (opening brace on new line)
- Expression-bodied members for simple properties
- Always include final newline in files
- Minimize LINQ in performance-critical paths (allocations)

## CF Code Organization Rules

**Always place CF code in `_CF/` subfolders** and prefix class names with `CF`:

| Location | Folder | C# namespace |
|---|---|---|
| Shared | `Content.Shared/_CF/MyFeature/` | `Content.Shared.CF.MyFeature` |
| Server | `Content.Server/_CF/MyFeature/` | `Content.Server.CF.MyFeature` |
| Client | `Content.Client/_CF/MyFeature/` | `Content.Client.CF.MyFeature` |
| Resources | `Resources/Prototypes/_CF/`, `Resources/Locale/en-US/_CF/` | n/a |

Folder uses `_CF` (sorts with other fork folders). Namespace drops the underscore: `Content.*.CF.*`.

Class examples: `CFMySystem`, `CFMyServerSystem`, `CFMyClientSystem`, `CFExampleComponent`.

**XAML under `_CF/`:** Robust derives the default type from the *file path* (`Content.Client._CF.…`). That does **not** match `Content.Client.CF.…` or a replaced upstream type. Always set `x:Class` to the real C# type. See `lobby-overlay.md`.

**Prototype IDs** — the `CF` prefix exists only to avoid name collisions with upstream:

- Prototype kind declared upstream/engine (`entity`, `reagent`, `Tag`, `job`, `soundCollection`, ...): ID **must** start with `CF` — `CFFoodBread`, `CFActionFireball` (not `ActionCFFireball`).
- Prototype kind declared in `_CF` (`cfBackground`, `cfAncestry`, custom kinds, ...): **no** extra `CF` on the instance ID unless the kind itself already encodes it — prefer plain IDs like `Fireball`, `Thief`.
- New `_CF` prototype kinds get plain camelCase names without prefix: `[Prototype("cookingRecipe")]`; check the name is not already taken by an upstream kind.
- Exceptions: IDs that intentionally reuse upstream IDs because upstream code, tests or cvars look them up (`Secret`, `DefaultMapPool`, `Empty`, `Baseline*`), `roleLoadout` (upstream requires `Job<JobId>`, e.g. `JobCFGuard`), and existing `gameMap` IDs.
- Renaming an ID stored in the DB drops players' data unless migrated.

**Prefer not editing upstream (non-`_CF`) files.** When a change in upstream code is unavoidable, wrap it:

```csharp
// CarpFinder: reason for the edit
... changed multiline ...
... code ...
// CarpFinder end

// or
changed inline 1-line edit // CarpFinder: reason for the edit
```

Never add `// CarpFinder` comments for `using` blocks.

**Preferred overlay pattern:** keep stock Lobby/UI (and similar) files identical to upstream; put replacements in `_CF/` and exclude the stock compile/XAML entries from the project file. See `lobby-overlay.md`.

## ECS Architecture

- **Components**: Pure data containers — no logic

```csharp
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFExampleComponent : Component
{
    [DataField]
    public string SomeField = string.Empty;

    [DataField, AutoNetworkedField]
    public int NetworkedValue;
}
```

- **Systems**: All logic lives here

```csharp
namespace Content.Shared.CF.Example;

public sealed partial class CFExampleSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnSomeEvent(Entity<CFExampleComponent> ent, ref SomeEvent args)
    {
        // logic
    }
}
```

- Use `[Dependency]` for dependency injection **without** the `readonly` keyword.
- Use `[Dependency] EntityQuery<T>` for performance-critical component lookups.
- All classes that use `[Dependency]` should be `partial`.

**Event subscriptions:** put `[SubscribeLocalEvent]` / `[SubscribeNetworkEvent]` / `[EventSubscription]` (= SubscribeAllEvent) on the handler method itself — comp/event are inferred from the signature. Don't call `SubscribeLocalEvent<...>()` in `Initialize()`. Use `[SubscribeLocalEvent(before: [typeof(X)], after: [typeof(Y)])]` for ordering. Fall back to a manual `Subscribe*Event()` call only where the attribute can't work: generic systems, `UIController`s, lambda handlers, conditional subscriptions.

**Critical ECS rules:**

- Never store mutable state inside systems — it is not saved/loaded with the game save. All persistent data belongs in components.
- Before subscribing a `Comp+Event` pair (attribute or manual call), verify it is not already subscribed — the engine throws on duplicate subscriptions.
