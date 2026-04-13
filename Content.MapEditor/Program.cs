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
            // Use our output directory so the mod loader finds all Content.* DLLs
            // (Content.Client, Content.Shared, Content.MapEditor) and registers
            // them as content assemblies — required for State instantiation.
            ContentBuildDirectory = "Content.MapEditor",
            Sandboxing = false,
            PostInitCallback = () =>
            {
                // Content.Client's EntryPoint has already run PreInit/Init/PostInit
                // (registering IoC, components, prototypes, etc.) and switched to its
                // default state.  Our EntryPoint (empty) ran as well.
                // Override the state with our editor state.
                var stateManager = IoCManager.Resolve<Robust.Client.State.IStateManager>();
                stateManager.RequestStateChange<MapEditorState>();
            }
        });
    }
}
