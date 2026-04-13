using Robust.Client.State;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Content.MapEditor;

public sealed class MapEditorState : State
{
    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;

    public MapEditorState()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Startup()
    {
        _sawmill = _logManager.GetSawmill("map_editor");
        _sawmill.Info("MapEditorState started");
    }

    protected override void Shutdown()
    {
        _sawmill.Info("MapEditorState shutdown");
    }
}
