# RT changes for embedded host use

This is a changelog for the RT side modifications that live on the
`map-editor-external-hwnd` branch of the RobustToolbox submodule. The
parent repo's `map-editor` branch bumps the submodule to pick them up
and builds `Tools/map_editor/` against them.

Everything here exists so that RT can be booted as a library inside
another .NET process and render into a window that process already
owns. The experimental editor in `Tools/map_editor/` is the only
current consumer, but the same plumbing would work for any host that
wants to embed RT: an Avalonia editor, a debugging tool, an asset
viewer, an integration test harness with visible output.

No breaking changes. Existing `Content.Client.exe` builds and runs
unchanged. Every new option defaults to the pre-existing behavior.

## The short version

Six commits, nine files, about a hundred lines of code. The shape of
the problem is:

1. Getting RT to wrap a pre-existing HWND instead of making its own
   top level window.
2. Giving the host a hook to run setup code on the game thread after
   RT finishes booting but before the main loop starts.
3. Letting the host's `State` and control subclasses through the
   content sandbox.

The rest of this document walks through each piece in order.

## External HWND support

The three linked commits are:

* `Add external HWND fields to WindowCreateParameters`
* `Plumb external HWND through the SDL3 backend`
* `Expose ExternalMainWindowHwnd on Clyde`
* `Wire MainWindowExternalHwnd through GameControllerOptions`

### WindowCreateParameters

Three new fields on `WindowCreateParameters`:

```csharp
public IntPtr? ExternalHWnd;
public bool WindowOwned = true;
public bool UseExternalGraphicsContext;
```

`ExternalHWnd` is the HWND to wrap, or null for the normal path.
`WindowOwned` controls whether RT destroys the underlying OS window
on shutdown, which you want `false` for when wrapping a host owned
HWND so RT doesn't tear down a window the host still uses.
`UseExternalGraphicsContext` is reserved. Nothing touches it yet.
It's there so a future host that wants to share an existing
OpenGL/ANGLE context has a place to opt in without another round of
API churn. If it turns out nothing ever needs it, it can be dropped.

### SDL3 backend

When `ExternalHWnd` is set, `CreateSdl3WindowForRenderer` passes it
to SDL3 via `SDL_PROP_WINDOW_CREATE_WIN32_HWND_POINTER` before
calling `SDL_CreateWindowWithProperties`. SDL3 then wraps that HWND
instead of creating a new one. Every SDL3 facility continues to work
against the wrapped window: the event loop, GL context creation, the
window proc subclass, everything. This turned out to be a lot less
work than expected because SDL3 does the hard part on its own.

`Sdl3WindowReg` picks up two flags:

```csharp
public bool OwnsWindow = true;
public bool IsExternalWindow;
```

The flags are set in `WinThreadWinCreate` right after
`WinThreadSetupWindow` runs. `CmdWinDestroy` carries `OwnsWindow`
through the cross thread command channel so `WinThreadWinDestroy`
can check it before calling `SDL_DestroyWindow`. If we do not own
the window, we skip the destroy. The host is responsible for the
underlying HWND.

### The input forwarding surprise

Early designs assumed input forwarding was going to be hard and
carried a whole "Tier 3" plan for manually subclassing the window
proc on the host side and posting `SDL_Event` structs into SDL3's
queue. None of that turned out to be needed.

When SDL3 wraps an external HWND it installs its own window proc
subclass on that HWND automatically, and mouse and keyboard events
flow into RT's normal event pipeline for free. `IInputManager`
dispatches them to controls the usual way, keybinds resolve through
the usual path, everything works without host side help.

The reason this was not obvious at first is that an embedded RT
starts out in a state with no visible UI elements for clicks to hit,
so it looks like nothing is happening. Adding a single control with
a `MouseFilter` of `Stop` and watching its `MouseMove` override fire
confirms events are arriving.

Net effect: no changes to `InputManager`, no `IInputSource`
abstraction, no manual message translation. The existing SDL3 path
handles it.

### Clyde side plumbing

`IClydeInternal` gains one property:

```csharp
IntPtr? ExternalMainWindowHwnd { get; set; }
```

`Clyde` stores it as a plain auto property, `ClydeHeadless` has a
stub implementation to keep the interface satisfied, and
`TryInitMainWindow` in `Clyde.Windowing.cs` reads it when building
the `WindowCreateParameters` for the main window:

```csharp
var parameters = new WindowCreateParameters
{
    Width = width,
    Height = height,
    Monitor = monitor,
    Fullscreen = fullscreen,
    ExternalHWnd = ExternalMainWindowHwnd,
    WindowOwned = !ExternalMainWindowHwnd.HasValue,
};
```

Open question: should this be on `IClyde` instead of
`IClydeInternal`? I kept it internal because it's a lifecycle hook
that only the game controller should be touching, but there's a
reasonable argument for making it part of the public surface.

### GameControllerOptions

`GameControllerOptions.MainWindowExternalHwnd` is the public entry
point for embedding hosts:

```csharp
public IntPtr? MainWindowExternalHwnd { get; init; }
```

In `GameController.StartupContinue`, the value is copied onto
`_clyde.ExternalMainWindowHwnd` right before `InitializePostWindowing`
runs. That's the latest point where Clyde can still be told where to
put its main window.

The full call flow is: host populates `GameControllerOptions`, calls
`ContentStart.StartLibrary`, which eventually reaches
`GameController.StartupContinue`, which hands the HWND to Clyde,
which hands it to the SDL3 backend, which gives it to SDL3 itself.
No other code paths exist, and no existing callers pass anything
that would change behavior.

## PostInitCallback

Commit: `Add PostInitCallback hook to GameControllerOptions`.

Embedding hosts need to be able to run setup code on the game thread
after RT has finished initializing but before the main loop starts
draining frames. Switching state, loading a map, wiring up a bridge
object, resolving IoC dependencies: all of these are much simpler if
they happen in one well defined place instead of by subclassing
`GameController` or polling from a different thread.

```csharp
public Action? PostInitCallback { get; init; }
```

Fired in `ContinueStartupAndLoop`, in between `StartupContinue()`
and `_mainLoop!.Run()`:

```csharp
if (Options.PostInitCallback is { } postInit)
{
    try
    {
        postInit();
    }
    catch (Exception ex)
    {
        _logger.Error($"GameControllerOptions.PostInitCallback threw: {ex}");
    }
}
```

The callback runs on the game thread, so it's free to call
`IoCManager.Resolve`, spin up entity systems, touch `EntityManager`,
anything. Exceptions get logged and swallowed, on the theory that a
buggy host callback shouldn't be able to crash RT startup. That is
debatable and easy to change if you'd rather have it propagate and
fail loud.

The editor uses this hook for a specific sequence: call
`IBaseClient.StartSinglePlayer()` to transition RunLevel to
`SinglePlayerGame`, create a paused map, set up the editor Eye,
publish an `EditorContext` bridge object that WPF code will observe,
and finally request the custom state. Ordering matters: the bridge
must be populated before the state switch, because the state's
`Startup` reads from it.

### Runlevel note

`GameController.Update` gates `EntityManager.FrameUpdate` on
`RunLevel >= Connected`. An embedded editor that just calls
`EntityManager.Startup` and `MapManager.Startup` directly gets a
working ECS but silently dead per frame ticking. `IconSmoothSystem`
never updates (walls don't smooth), sprite animations freeze, and
anything else doing per frame work breaks in subtle ways. The fix is
to go through `IBaseClient.StartSinglePlayer()` which does the
runlevel transition as a bundle. This is not an RT change, it's a
thing embedding hosts need to know, but it took real debugging time
to figure out so it belongs in this document.

## ContentAccessAllowedAttribute visibility

Commit: `Make ContentAccessAllowedAttribute public`.

One line change. `DynamicTypeFactory.CreateInstance` calls
`IsContentTypeAccessAllowed` which returns true for types in content
assemblies and for types tagged with `[ContentAccessAllowed]`. The
attribute was internal, which meant only code inside `Robust.Shared`
could apply it.

The editor has a custom `State` subclass and a custom
`ViewportContainer` subclass, both of which need to be dynamically
instantiated via `IStateManager.RequestStateChange` or similar.
They live in `MapEditor.RTBridge`, which is outside the Content
load path. Without the attribute, the sandbox rejects them and the
state switch throws. Tagging them works as soon as the attribute is
publicly visible.

Promoting the attribute from `internal` to `public` is a no op for
existing code (nothing outside Robust.Shared was tagging types
anyway, because it couldn't) and unblocks the embedding case.

Note that `GameControllerOptions.Sandboxing = false` alone is not
enough. The content type access check runs independently of the
sandbox flag. The attribute is the documented path.

## What did not need to change

Tracking these because they're the scary ones that could have blocked
the whole effort and didn't:

`InputManager` and the input pipeline. SDL3 handles external HWND
input on its own, nothing host side or engine side needed changing.

`MainMenu` and the state machine. The editor's `PostInitCallback`
switches to a custom state before the first frame is rendered. The
main menu is instantiated (its `Startup` runs) but never drawn.
There's no visible flash of the main menu.

OLE and COM initialization. An embedded RT on an STA thread logs
`OleInitialize() failed: 0x80010106 (RPC_E_CHANGED_MODE)` when the
host (for example WPF) has already initialized OLE on the thread.
SDL3 falls back to a non-OLE drag and drop path and continues
normally.

DPI handling. SDL3 picks up the DPI from the wrapped HWND correctly.
Sprites render at the right size.

OpenGL context creation. SDL3 creates a standard GL context against
the wrapped HWND with no host side cooperation. The only requirement
is that the host window class has `CS_OWNDC` so OpenGL gets a
dedicated device context, which any sane native window class will.

## Things the host still has to do

These do not belong in RT, listing them so the embedding picture is
complete.

STA worker thread. RT's game thread needs STA on Windows. `Task.Run`
gives you an MTA thread pool worker which breaks Win32 windowing. The
host creates a dedicated `Thread` with
`SetApartmentState(ApartmentState.STA)` and `IsBackground = true`.

Native window class. The host registers its own Win32 window class
with `CS_OWNDC` and a plain `DefWindowProc`, creates a child window
of that class inside an `HwndHost` (or equivalent), and hands the
resulting HWND to RT via `MainWindowExternalHwnd`. Using the built in
`STATIC` class works for rendering but its `CS_PARENTDC` reflection
behavior interacts oddly with SDL3's subclassing, a custom class is
cleaner.

Output path convention. RT's dev mode resource mount code assumes
the exe sits two levels below the repo root so `../../Resources/`
resolves. The host csproj needs an `<OutputPath>` that matches.

`IBaseClient.StartSinglePlayer()` instead of manual entity system
startup. See the runlevel note above.

`[ContentAccessAllowed]` on any `State` subclass or other type the
host dynamically instantiates via RT's type factory.

`Eye.DrawLight = false` and `DrawFov = false` on the editor eye,
otherwise unlit editor maps render black because nothing on a fresh
paused map is producing light.

Non fatal content system noise. A stock client has some systems
(text to speech, guidebook, etc.) that raise network events during
`Initialize`. Without a server those log errors but don't block
anything. The embedding host lives with the log noise or excludes
those systems from its content bundle.

## Files touched

```
Robust.Client/GameControllerOptions.cs
Robust.Client/GameController/GameController.cs
Robust.Client/GameController/GameController.Standalone.cs
Robust.Client/Graphics/IClydeInternal.cs
Robust.Client/Graphics/WindowCreateParameters.cs
Robust.Client/Graphics/Clyde/Clyde.cs
Robust.Client/Graphics/Clyde/Clyde.Windowing.cs
Robust.Client/Graphics/Clyde/ClydeHeadless.cs
Robust.Client/Graphics/Clyde/Windowing/Sdl3.Window.cs
Robust.Client/Graphics/Clyde/Windowing/Sdl3.WindowThread.cs
Robust.Shared/ContentPack/ContentAccessAllowedAttribute.cs
```

About a hundred lines of code plus comments. Every existing test
passes. Nothing in the non embedding code paths is affected.
