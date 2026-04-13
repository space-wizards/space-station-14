using Content.Client.UserInterface.Controls;
using Content.MapEditor.UI;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.MapEditor;

public sealed class MapEditorState : State
{
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    private ISawmill _sawmill = default!;

    public MapEditorState()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Startup()
    {
        _sawmill = _logManager.GetSawmill("map_editor");
        _sawmill.Info("MapEditorState started");

        _uiManager.LoadScreen<MapEditorScreen>();

        // Wire the viewport as the main eye viewport so the engine renders into it.
        var screen = (MapEditorScreen) _uiManager.ActiveScreen!;
        _eyeManager.MainViewport = screen.MainViewport.Viewport;
    }

    protected override void Shutdown()
    {
        _uiManager.UnloadScreen();
        _sawmill.Info("MapEditorState shutdown");
    }
}
