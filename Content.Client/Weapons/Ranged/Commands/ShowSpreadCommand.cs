using Content.Client.Weapons.Ranged.Systems;
using Robust.Shared.Console;

namespace Content.Client.Weapons.Ranged;

public sealed class ShowSpreadCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GunSystem _gun = default!;
    public override string Command => "showgunspread";
    public override string Description => $"Shows gun spread overlay for debugging";
    public override string Help => $"{Command}";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _gun.SpreadOverlay ^= true;

        shell.WriteLine($"Set spread overlay to {_gun.SpreadOverlay}");
    }
}
