using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Shared.Administration;

namespace Content.Client.MusicPlayer;

[AnyCommand]
public sealed class OpenMusicPlayerCommand : IConsoleCommand
{
    public string Command => "music";
    public string Description => "Open music player window";
    public string Help => "Usage: music";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var sysMan = IoCManager.Resolve<IEntitySystemManager>();
        var sys = sysMan.GetEntitySystem<MusicPlayerSystem>();
        sys.OpenWindow();
    }
}
