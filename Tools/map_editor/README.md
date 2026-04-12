# SS14 Map Editor (Experimental)

A prototype native desktop map editor that embeds **Robust Toolbox** as the
rendering backend, so the editor produces pixel perfect in game visuals
instead of approximating them with a custom renderer.

This tool exists to prove out a few things:

1. Can RT be embedded as a library inside another .NET host process and
   render into that host's window?
2. What does the minimum set of RT changes look like to make that work?
3. What does the communication layer between a host UI (WPF) and an
   embedded RT look like in practice?

The editor is not a replacement for the in game mapping tools. It is a
research prototype that happens to be usable.

## Layout

```
Tools/map_editor/
  map_editor.slnx
  src/
    MapEditor.Desktop/        WPF host app (net10.0-windows, WinExe)
      RtHwndHost.cs           HwndHost control that exposes a child HWND
      MainWindow.xaml(.cs)    Sidebar + viewport layout
      App.xaml(.cs)           WPF entry point
    MapEditor.RTBridge/       RT bootstrap and command bridge
      EditorBootstrap.cs      Starts RT with the host HWND
  docs/                       Design docs and RT change log
```

## Building

Requires the local RT branch `map-editor-external-hwnd` in the
`RobustToolbox/` submodule, which adds the external HWND support the
editor depends on.

```powershell
dotnet build Tools/map_editor/map_editor.slnx
```

## Running

```powershell
dotnet run --project Tools/map_editor/src/MapEditor.Desktop
```
