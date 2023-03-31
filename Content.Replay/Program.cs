using Robust.Client;

namespace Content.Replay;

internal static class Program
{
    public static void Main(string[] args)
    {
        ContentStart.StartLibrary(args, new GameControllerOptions()
        {
            // TODO REPLAYS fix sandbox
            // res.typecheck: Sandbox violation: Access to type not allowed: [Robust.NetSerializer]NetSerializer.NetListAsArray`1
            // res.typecheck: Sandbox violation: Access to method not allowed: int32 [System.Runtime]System.Array.BinarySearch(!!0[], !!0)
            // res.typecheck: Sandbox violation: Access to type not allowed: [Prometheus.NetStandard]Prometheus.Histogram

            Sandboxing = false,
            ContentModulePrefix = "Content.",
            ContentBuildDirectory = "Content.Replay",
            DefaultWindowTitle = "SS14 Replay",
            UserDataDirectoryName = "Space Station 14",
            ConfigFileName = "replay.toml"
        });
    }
}
