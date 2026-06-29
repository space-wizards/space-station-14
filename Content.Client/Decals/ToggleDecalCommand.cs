using Robust.Shared.Console;

namespace Content.Client.Decals;

public sealed partial class ToggleDecalCommand : LocalizedEntityCommands
{
    [Dependency] private DecalSystem _decal = default!;

    public override string Command => "toggledecals";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _decal.ToggleOverlay();
    }
}
