using Robust.Client.Graphics;
using Robust.Shared.Console;

namespace Content.Client.Audio;

public sealed class AmbientOverlayCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override string Command => "showambient";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var existing = _overlay.RemoveOverlay<AmbientSoundOverlay>();
        if (!existing)
            _overlay.AddOverlay(new AmbientSoundOverlay(EntityManager, _ambient, _lookup));

        shell.WriteLine(Loc.GetString($"cmd-showambient-status", ("status", existing)));
    }
}
