using Robust.Shared.Console;

namespace Content.Client.Audio;

public sealed partial class AmbientOverlayCommand : LocalizedEntityCommands
{
    [Dependency] private AmbientSoundSystem _ambient = default!;

    public override string Command => "showambient";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _ambient.OverlayEnabled ^= true;

        shell.WriteLine(Loc.GetString($"cmd-showambient-status", ("status", _ambient.OverlayEnabled)));
    }
}
