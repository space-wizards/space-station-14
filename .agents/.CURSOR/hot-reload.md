# Hot reload / hotswap in CarpFinder (SS14)

## XAML (UI layouts)

XAML is **AOT-compiled into the client DLL**. Editing a `.xaml` on disk does nothing by itself until either:

1. **Xaml hot reload** (Debug / Tools builds only), or
2. You **rebuild Content.Client** and **restart the client**.

### Built-in XAML hot reload (TOOLS)

There is **no separate enable switch**. It turns on automatically when the client was compiled with `TOOLS` (`RobustToolsBuild=true`):

| Configuration | XAML hot reload |
|---|---|
| **Debug** (default for local) | On |
| **DebugOpt** | On |
| **Tools** | On |
| **Release** (`FullRelease`) | Off (stub) |

**How to run it:**

1. Build/run **Content.Client** (and Server) with configuration **Debug** or **Tools** — not Release.
2. Start from the **repo root** (so content roots + `SpaceStation14.slnx` resolve). At startup the log should show:
   - `xamlhotreload: code location: /home/…/CarpFinder`
   - If you see `could not find code -- where is SpaceStation14.slnx?`, the watcher never started (wrong working directory / packaged client).
3. Save a `.xaml` under the repo. Log should show `replacing …` / JIT activity.
4. **Recreate** the UI (for lobby: **restart client** — editor is cached). Closing character setup is not enough.

#### Rider

Shared run configs in `.run/` default to **Tools**:

- **Content.Client** / **Content.Server** / **Content Server+Client**

1. Run config dropdown → **Content Server+Client** (or Client/Server alone).
2. Confirm the configuration chip next to the run button says **Tools** (not Debug/Release).
3. Run. First Tools build may take a few minutes.

If hot reload still missing, check client log for `xamlhotreload: code location:`. Also remember lobby editors are cached — restart client after XAML edits.

#### CLI (from repo root)

**Debug** (default `dotnet run` config is Debug — XAML hot reload on):

```bash
# terminal 1 — server
dotnet run --project Content.Server -c Debug

# terminal 2 — client
dotnet run --project Content.Client -c Debug
```

Same thing via the repo scripts:

```bash
./runserver.sh    # Content.Server, Debug
./runclient.sh    # Content.Client, Debug
```

**Tools** (optimized + TOOLS / hot reload on):

```bash
# terminal 1
dotnet run --project Content.Server -c Tools

# terminal 2
dotnet run --project Content.Client -c Tools
```

Or:

```bash
./runserver-Tools.sh
./runclient-Tools.sh
```

Build only (client/server must be closed or you get `MSB3027`):

```bash
dotnet build Content.Server/Content.Server.csproj -c Debug
dotnet build Content.Client/Content.Client.csproj -c Debug
# then:
dotnet run --project Content.Server -c Debug --no-build
dotnet run --project Content.Client -c Debug --no-build
```

Optional CVars (client-only):

- `ui.xaml_hot_reload_marker_name` — default `SpaceStation14.slnx` (file used to find the repo root).
- `ui.xaml_jit_preload` — `false` by default (JIT on demand); `true` JITs all XAML at startup (slower boot).

**Important:** already-constructed controls keep their old visual tree. Hot reload does **not** rebuild open windows.

### Lobby / HumanoidProfileEditor gotcha

`LobbyUIController.EnsureGui()` **caches** `_profileEditor` / `_characterSetup`. Closing character setup only hides them; it does **not** create a new editor.

So for CF lobby XAML (`Content.Client/_CF/Lobby/UI/HumanoidProfileEditor.xaml`):

1. Save the XAML (hot reload may fire in Debug).
2. **Fully restart the client** (or otherwise force a new `HumanoidProfileEditor` instance).
3. Re-open character setup.

If hot reload isn’t active or you’re unsure: close client → `dotnet build Content.Client` → start client again.

### Reliable no-surprises workflow

```bash
# Client must be closed (else MSB3027 file lock)
dotnet build Content.Client/Content.Client.csproj -c Debug
# then start client again and open character setup
```

## Prototypes / locale (not XAML)

There is **no** `reloadprototypes` console command anymore (Toolshed will say “Got unknown command”).

### Prototypes (automatic in TOOLS client)

In Debug/Tools, the **client** watches `Resources/Prototypes/**/*.yml` (`res.prototype_reload_watch`, default `true`):

1. Save a `.yml` under `Resources/Prototypes/` (including `_CF/`).
2. Focus the game window (reload is queued until focus returns).
3. Client log should show `reload: Reloading /Prototypes/...` and `Reloaded prototypes in … ms`.
4. Client also notifies the server (`MsgReloadPrototypes`) so both sides update — needs admin reload rights if connected as a player; local host usually fine.

Manual one-off (client console, opens a file picker and uploads YAML to the server):

```
loadprototype
```

### Locale

Fluent `.ftl` can also be file-watched in TOOLS; if a string doesn’t update, reconnect or restart.

C# system/component logic still needs a rebuild + restart (no general C# hot swap for content).

## Quick checklist when “I don’t see my XAML change”

1. Are you on Debug/Tools (not a tools-stripped Release)?
2. Did the client log show `xamlhotreload` / `code location`?
3. Did you **restart the client** after changing lobby XAML? (cached editor)
4. If still stale: rebuild `Content.Client` with the client closed.
