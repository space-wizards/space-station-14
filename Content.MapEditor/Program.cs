using Robust.Client;
using Robust.Shared.IoC;

namespace Content.MapEditor;

internal static class Program
{
    public static void Main(string[] args)
    {
        ContentStart.StartLibrary(args, new GameControllerOptions
        {
            DefaultWindowTitle = "SS14 Map Editor",
            ContentBuildDirectory = "Content.MapEditor",
            Sandboxing = false,
            PostInitCallback = () =>
            {
                // Don't call StartSinglePlayer — MapEditorState will launch a real
                // headless server process and connect to it so that server-side systems
                // (NodeGroupSystem, CableVisSystem, etc.) run properly.
                var stateManager = IoCManager.Resolve<Robust.Client.State.IStateManager>();
                stateManager.RequestStateChange<MapEditorState>();
            }
        });
    }
}
