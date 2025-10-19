using Robust.Shared.Console;

namespace Content.Client.Decals;

public sealed class ToggleDecalCommand : LocalizedEntityCommands
{
    [Dependency] private readonly DecalSystem _decal = default!;

    public override string Command => "toggledecals";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _decal.ToggleOverlay();
    }
}
