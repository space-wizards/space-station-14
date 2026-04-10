# How the editor talks to RT

Notes on the WPF host to Robust Toolbox communication layer in
`Tools/map_editor/`. If you're writing a new editor command, trying
to debug a threading bug, or just wondering why the bridge is shaped
the way it is, read this.

## One process, two UI threads

The editor is a single process. Inside that process there are two
threads you care about:

The **WPF dispatcher thread** is the normal WPF UI thread. It runs
the window chrome, the menus, the sidebar, the file dialogs, and
the RtHwndHost control. Standard STA, owns the Win32 message loop
for the top level window.

The **RT game thread** is a dedicated STA thread we create during
startup. It runs `ContentStart.StartLibrary`, which means it owns
SDL3's event loop, `EntityManager.FrameUpdate`, Clyde rendering,
`IoCManager`, and everything else RT considers "the main thread".
This thread is named `RT Game Thread` in the debugger.

Both threads live in the same process and can see each other's
objects, but they absolutely cannot just poke at each other. RT's
entity manager, eye manager, and prototype manager are all
unsynchronized. Touching them from the WPF thread will race.
Similarly, WPF controls throw on cross thread access. The bridge
exists to make crossing the boundary safe and obvious.

Two assemblies matter:

* `MapEditor.Desktop`: the WPF exe. XAML, dialogs, command handlers.
  Lives entirely on the dispatcher thread.
* `MapEditor.RTBridge`: a class library both sides reference. Holds
  the `EditorContext` bridge, `EditorBootstrap`, the custom `State`
  and `ViewportContainer` subclasses, and the camera. Types in this
  project are touched by both threads, so everything here has to be
  thread aware.

## EditorContext

The whole conversation funnels through one object:
`MapEditor.RTBridge.EditorContext`. It's a process wide singleton:

```csharp
public static EditorContext? Current { get; internal set; }
public static Task<EditorContext> Ready => _readyTcs.Task;
```

`EditorContext` holds references to the RT collaborators that host
code needs to poke: an `ITaskManager` for thread marshalling, an
`IEntityManager`, an `IEyeManager`, the shared editor `Eye`, plus
host readable state like `PlacementPrototypeId`. Methods on this
class are the public API between the host and RT.

### Lifecycle

1. Process starts, WPF's `MainWindow.OnLoaded` fires, the host
   creates a dedicated STA thread and starts `EditorBootstrap.Start`
   on it.
2. RT boots, goes through its normal `ContentStart` init path, and
   calls `EditorBootstrap.OnRtInitialized` from its
   `PostInitCallback` hook.
3. `OnRtInitialized` runs on the game thread. It sets up the editor
   eye, publishes a fresh `EditorContext` via
   `EditorContext.PublishReady`, then switches to the editor's
   custom state.
4. The WPF thread has been awaiting `EditorContext.Ready` the whole
   time. The await resumes, the host grabs the context, and it can
   now start dispatching commands.

`PublishReady` both assigns `Current` and completes the `Ready`
task. Using a `TaskCompletionSource` instead of a busy wait means
the WPF code can `await EditorContext.Ready` and have the
continuation automatically resume on the dispatcher thread (WPF's
sync context is captured at the await). Late subscribers get the
context instantly because the task stays completed forever after
the first resolution.

## RunOnMainThread is the one primitive

Under the hood everything WPF to RT flows through
`Robust.Shared.Asynchronous.ITaskManager`:

```csharp
public interface ITaskManager
{
    void RunOnMainThread(Action callback);
    ...
}
```

RT's own `RobustSynchronizationContext` lives on the game thread and
its main loop drains the pending queue every tick. A callback posted
through `RunOnMainThread` runs inside the normal game loop, which
means it sees consistent ECS state and doesn't need to worry about
being interrupted.

`EditorContext.RunOnGameThread` wraps this into a `Task` returning
helper so host code can use `async/await` and `try/catch` normally:

```csharp
public Task RunOnGameThread(Action callback)
{
    var tcs = new TaskCompletionSource();
    _taskManager.RunOnMainThread(() =>
    {
        try { callback(); tcs.SetResult(); }
        catch (Exception e) { tcs.TrySetException(e); }
    });
    return tcs.Task;
}
```

## Command API pattern

Every host initiated command looks the same:

* A public `Async` method on `EditorContext`, called from the WPF
  thread. Validates its arguments, then posts the actual work to
  the game thread.
* A private method that runs on the game thread and does the real
  work. Throws on failure.
* A WPF event handler that `await`s the public method and updates
  the UI (usually just the sidebar status text) based on success
  or failure.

Map loading is the cleanest example:

```csharp
public Task LoadMapAsync(string absolutePath)
{
    if (string.IsNullOrWhiteSpace(absolutePath))
        throw new ArgumentException("Path must not be empty", nameof(absolutePath));
    return RunOnGameThread(() => LoadMapInternal(absolutePath));
}

private void LoadMapInternal(string absolutePath)
{
    var mapLoader = _entityManager.System<MapLoaderSystem>();
    using var stream = File.OpenRead(absolutePath);
    if (!mapLoader.TryLoadGeneric(stream, Path.GetFileName(absolutePath), out var loadResult))
        throw new InvalidOperationException($"Failed to load map: {absolutePath}");
    // ...point the eye at the loaded map...
}
```

Commands that return a value use a manual `TaskCompletionSource<T>`
instead of `RunOnGameThread` since `RunOnGameThread` only gives you
a non generic `Task`. The spawn and prototype enumeration methods
on `EditorContext` follow this pattern.

A few things about the pattern that are easy to miss:

Validation runs on the calling thread, not the game thread. Bad
arguments throw a normal exception on whichever thread the WPF
code called from, so the stack trace points at the actual caller.
Only the RT touching work runs inside the marshalled callback.

Exceptions thrown inside the marshalled callback get captured on
the task and re-thrown at the `await`. The game loop is never
disturbed. The WPF caller catches with a normal `try/catch`
around the await and updates the sidebar.

Continuations after `await` are back on the WPF dispatcher. That's
because WPF captures its sync context before suspending, and
`await` restores it on resume. Touching WPF controls after an await
is safe without `Dispatcher.Invoke` or `BeginInvoke`. If you ever
see someone write `Dispatcher.Invoke(() => StatusText.Text = ...)`
immediately after awaiting a bridge call, that's a bug (a harmless
one, but still).

## Shared state pattern

Not every WPF to RT handoff needs a task round trip. Sometimes you
just want to set a flag the game thread will read later. Entity
placement is the canonical example:

```csharp
public string? PlacementPrototypeId { get; set; }
```

The WPF palette writes this when the user picks an entity. The
viewport control reads it on every left click. No lock, no
marshalling, no task. Reference type reads and writes are atomic in
.NET so there's no tearing, and the worst case race is that the
game thread sees the old value for one frame because the write
hasn't propagated yet. That doesn't matter for a manually
triggered palette pick.

Rule of thumb: use shared state when the host just wants to set a
value the game thread will consult later, when there's no return
value, when the read happens frequently (every frame, every click),
and when the value is a single field that reads and writes
atomically. Use a command call when you want to trigger something
to happen right now, or when you need the result back, or when the
operation involves multiple fields that need to stay consistent.

## Direct access from game thread code

Code that already runs on the game thread doesn't need the bridge
at all. The viewport control, the custom state, any entity system:
they can read `EditorContext.Current` directly and use the RT APIs
they're already holding without any marshalling. For example, the
left click placement handler in `EditorViewportControl`:

```csharp
private void TryPlaceEntity(Vector2 cursorPixel)
{
    var context = EditorContext.Current;
    var protoId = context?.PlacementPrototypeId;
    if (context == null || string.IsNullOrEmpty(protoId))
        return;

    var world = _camera.ScreenToWorld(cursorPixel, Size);
    var coords = new MapCoordinates(world, _editorEye.Position.MapId);
    _entityManager.SpawnEntity(protoId, coords);
}
```

No `RunOnGameThread`. This code is called from
`EditorViewportControl.KeyBindDown`, which RT invokes from its main
loop, which means we're already on the game thread. `RunOnGameThread`
is only for code that starts on the WPF thread.

## End to end: left click places an entity

Walk through of what happens when the user clicks a tile with a
Chair selected in the palette. The point of this is to make clear
how many thread hops are actually involved (not many).

Startup, happens once:

1. WPF dispatcher: `OnLoaded` finishes booting RT, then awaits
   `EditorContext.Ready`.
2. Game thread: `OnRtInitialized` publishes the context.
3. WPF dispatcher: `Ready` resolves, the handler calls
   `GetSpawnablePrototypesAsync` which round trips to the game
   thread to enumerate `IPrototypeManager` and back.
4. WPF dispatcher: the returned list populates the sidebar ListBox.

The user picks "Chair":

5. WPF dispatcher: `ListBox.SelectionChanged` handler writes
   `EditorContext.Current.PlacementPrototypeId = "Chair"`. Single
   atomic reference write, no round trip.

The user left clicks the viewport:

6. OS: mouse click goes to the hosted Win32 child HWND. SDL3's
   window proc subclass catches it and posts a mouse button event
   into SDL3's event queue.
7. Game thread: RT's main loop drains SDL3 events, translates the
   button press into `EngineKeyFunctions.Use`, and dispatches it
   through the UI manager to the control under the cursor.
8. Game thread: `EditorViewportControl.KeyBindDown` fires with
   `args.Function == Use`, calls `TryPlaceEntity`.
9. Game thread: `TryPlaceEntity` reads
   `EditorContext.Current.PlacementPrototypeId`, converts the click
   pixel to world coordinates via `EditorCamera.ScreenToWorld`,
   calls `_entityManager.SpawnEntity("Chair", coords)`. The chair
   appears under the cursor.

Visual result: instant. The status bar isn't updated because this
code path doesn't touch WPF state, and it doesn't need to. The
viewport is the feedback.

## Design choices worth explaining

One bridge object, not a scattering of helpers. Concentrates all
the thread boundary knowledge in one class that's easy to audit.
Every call site is visible at review time.

Task based API, not events or callbacks. Lets WPF code use
`async/await` with natural exception flow, and keeps the command
and its status updates in the same method.

`EditorContext.Current.PlacementPrototypeId` is publicly mutable.
The WPF side writes it directly, no setter method, no round trip.
The alternative (wrap it in `SetPlacementAsync` that marshals to
the game thread) would be strictly more boilerplate with no
correctness benefit, because atomic reference assignment is
already safe.

Validation lives on the calling thread. Arg checks that throw
`ArgumentException` run on whichever thread invoked the public
method. Bad host code fails in the WPF dispatcher with a stack
trace the author can actually act on. Only RT touching work
marshals.

Custom `State` subclass tagged `[ContentAccessAllowed]`. The
sandbox check in `DynamicTypeFactory.CreateInstance` refuses
non content types by default, and the bridge project lives outside
the Content load path. Without the attribute,
`IStateManager.RequestStateChange<EditorViewportState>` throws.

`IBaseClient.StartSinglePlayer()` instead of calling
`EntityManager.Startup` and `MapManager.Startup` manually. Both
approaches appear to work at first, but the manual one leaves
`RunLevel` at `Initialize`, and `GameController.Update` skips
`EntityManager.FrameUpdate` unless `RunLevel >= Connected`. Per
frame work like `IconSmoothSystem` silently dies. Debugging that
cost a morning, `StartSinglePlayer` does the runlevel transition
as a bundle and is the correct entry point.

## Adding a new command

Template for adding "save map as", "delete entity under cursor",
"rotate selected", or whatever comes next.

Add the method to `EditorContext`:

```csharp
public Task DoThingAsync(ThingArgs args)
{
    if (args == null) throw new ArgumentNullException(nameof(args));
    return RunOnGameThread(() => DoThingInternal(args));
}

private void DoThingInternal(ThingArgs args)
{
    // Runs on the game thread. Free to touch _entityManager,
    // IoCManager.Resolve, all the RT APIs. Throwing is fine,
    // the exception surfaces via the returned task.
}
```

Call it from the WPF handler:

```csharp
private async void OnDoThingClick(object sender, RoutedEventArgs e)
{
    var context = EditorContext.Current;
    if (context == null) { StatusText.Text = "RT not ready"; return; }

    StatusText.Text = "Doing thing...";
    try
    {
        await context.DoThingAsync(args);
        StatusText.Text = "Done";
    }
    catch (Exception ex)
    {
        StatusText.Text = $"Failed: {ex.Message}";
    }
}
```

For commands that need a return value, use a manual
`TaskCompletionSource<T>` the way `GetSpawnablePrototypesAsync`
does.

For commands triggered by in viewport input (clicks, keys,
scrolls), don't use the bridge at all. Handle the event in the
viewport control, read whatever state you need from
`EditorContext.Current`, and call RT APIs directly. You're already
on the game thread.

## Anti patterns

Don't touch RT APIs from the WPF thread. `EntityManager`,
`IoCManager`, `IPrototypeManager`, the eye, none of it is
thread safe. Always hop to the game thread via `RunOnGameThread`
or an equivalent.

Don't touch WPF controls from the game thread. No
`StatusText.Text = ...` inside a `RunOnMainThread` callback.
Return a value from the task and update the UI after the `await`.

Don't call `.Result` or `.Wait()` on bridge tasks from the WPF
thread. That will deadlock if the task's continuation needs the
dispatcher. Always use `async void` handlers with `await`.

Don't assume `EditorContext.Current` is non null at startup. The
WPF window loads before RT finishes booting. Either check for null
or `await EditorContext.Ready`.

Don't add locks to `EditorContext` without a real multi field
invariant to protect. Single field reference reads and writes are
atomic and don't need synchronization. Locks slow down the hot
path and usually signal you're solving the wrong problem.

Don't construct multiple `EditorContext` instances. Only
`EditorBootstrap.OnRtInitialized` should ever call the constructor,
and it should publish exactly once via `PublishReady`.

## See also

* `rt-changes-for-embedding.md` for the RT side changes the bridge
  depends on (external HWND, `PostInitCallback`, the attribute
  visibility).
* `Tools/map_editor/src/MapEditor.RTBridge/EditorContext.cs` is
  where the bridge itself lives and is the best place to start
  reading if you want to extend it.
