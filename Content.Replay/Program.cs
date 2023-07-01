using Robust.Client;

namespace Content.Replay;

internal static class Program
{
    public static void Main(string[] args)
    {
        ContentStart.StartLibrary(args, new GameControllerOptions()
        {
            Sandboxing = true,
            ContentModulePrefix = "Content.",
            ContentBuildDirectory = "Content.Replay",
            DefaultWindowTitle = "SS14 Replay",
            UserDataDirectoryName = "Space Station 14",
            ConfigFileName = "replay.toml"
        });
    }
}
