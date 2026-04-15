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
                // Start single-player to initialize the ECS. MapEditorState will then
                // launch a headless server and connect to it for proper entity support.
                var baseClient = IoCManager.Resolve<IBaseClient>();
                baseClient.StartSinglePlayer();

                var stateManager = IoCManager.Resolve<Robust.Client.State.IStateManager>();
                stateManager.RequestStateChange<MapEditorState>();
            }
        });
    }
}
