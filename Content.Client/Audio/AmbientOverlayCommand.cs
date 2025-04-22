using Robust.Shared.Console;

namespace Content.Client.Audio;

public sealed class AmbientOverlayCommand : LocalizedEntityCommands
{
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;

    public override string Command => "showambient";
    public override string Description => "Shows all AmbientSoundComponents in the viewport";
    public override string Help => $"{Command}";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _ambient.OverlayEnabled ^= true;
        shell.WriteLine($"Ambient sound overlay set to {_ambient.OverlayEnabled}");
    }
}
