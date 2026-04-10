using System;
using System.Numerics;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Shared.Asynchronous;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;

namespace MapEditor.RTBridge;

/// <summary>
///     Entry point for booting Robust Toolbox inside the WPF host.
/// </summary>
/// <remarks>
///     The host provides an existing HWND (for example from a WPF
///     <see cref="System.Windows.Interop.HwndHost"/>) and we configure RT to
///     wrap it as its main window instead of creating a new OS window.
///     After this call returns, RT has finished booting and the main loop
///     is running on the calling thread.
/// </remarks>
public static class EditorBootstrap
{
    /// <summary>
    ///     Boots RT with the given HWND as its main window. Blocks for the
    ///     lifetime of the RT game loop, so callers should invoke it on a
    ///     background STA thread.
    /// </summary>
    public static void Start(IntPtr externalHwnd)
    {
        if (externalHwnd == IntPtr.Zero)
            throw new ArgumentException("externalHwnd must not be zero", nameof(externalHwnd));

        var options = new GameControllerOptions
        {
            UserDataDirectoryName = "MapEditor",
            MainWindowExternalHwnd = externalHwnd,
            PostInitCallback = OnRtInitialized,
            // The editor publishes its own State subclass and a
            // ViewportContainer from MapEditor.RTBridge, which lives outside
            // the Content load path. The sandbox check in
            // DynamicTypeFactory would reject those if sandboxing was on,
            // so we disable it. The types are still marked
            // [ContentAccessAllowed] for the attribute based escape hatch.
            Sandboxing = false,
        };

        // ContentStart.StartLibrary is the supported entry point for
        // embedding RT inside another .NET process. It blocks until the
        // game loop exits.
        ContentStart.StartLibrary(Array.Empty<string>(), options);
    }

    /// <summary>
    ///     Runs once on the RT game thread after initialization finishes
    ///     and before the main loop starts. This is where we swap to the
    ///     editor state, create the editor eye, and publish the
    ///     EditorContext so the host can start dispatching commands.
    /// </summary>
    private static void OnRtInitialized()
    {
        var sawmill = Logger.GetSawmill("map_editor");
        sawmill.Info("PostInitCallback: entering single player runlevel");

        // IBaseClient.StartSinglePlayer transitions RunLevel from
        // Initialize to SinglePlayerGame. It does EntityManager.Startup
        // and MapManager.Startup under the hood, AND satisfies the
        // RunLevel >= Connected gate in GameController that lets entity
        // system FrameUpdates actually run. Without this, per frame work
        // like IconSmoothSystem (walls smoothing into their neighbors)
        // silently breaks because FrameUpdate is never called on content
        // systems.
        var baseClient = IoCManager.Resolve<IBaseClient>();
        baseClient.StartSinglePlayer();

        var entityManager = IoCManager.Resolve<IEntityManager>();
        var mapSys = entityManager.System<SharedMapSystem>();
        var eyeManager = IoCManager.Resolve<IEyeManager>();

        // Create a paused map so nothing ticks game logic on the editor
        // map while the user is editing.
        mapSys.CreateMap(out var mapId, runMapInit: false);
        sawmill.Info($"Created editor map {mapId}");

        // Put the eye on the empty map. Zoom 0.25 = one tile takes 128
        // pixels on screen, DrawLight/DrawFov disabled so tiles render
        // without lighting (there are no light sources on a fresh map,
        // so without this everything looks black).
        var editorEye = new Eye
        {
            Position = new MapCoordinates(new Vector2(0, 0), mapId),
            Zoom = new Vector2(0.25f, 0.25f),
            DrawLight = false,
            DrawFov = false,
        };
        eyeManager.CurrentEye = editorEye;
        sawmill.Info($"Editor eye on map {mapId} at {editorEye.Position.Position}");

        // Publish the EditorContext BEFORE switching state.
        // EditorViewportState.Startup reads EditorContext.Current.EditorEye,
        // so it must be populated or the viewport ends up holding a stale
        // fallback eye that is not the one being rendered.
        var taskManager = IoCManager.Resolve<ITaskManager>();
        var context = new EditorContext(taskManager, entityManager, eyeManager)
        {
            EditorEye = editorEye,
        };
        EditorContext.PublishReady(context);
        sawmill.Info("EditorContext published");

        // State switch happens last so Startup sees Current populated.
        sawmill.Info("Switching to EditorViewportState");
        var stateManager = IoCManager.Resolve<IStateManager>();
        stateManager.RequestStateChange<EditorViewportState>();
    }
}
