using Content.Client.Weapons.Ranged.Systems;
using Robust.Shared.Console;

namespace Content.Client.Weapons.Ranged.Commands;

public sealed class ShowSpreadCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GunSystem _gun = default!;
    public override string Command => "showgunspread";
    public override string Description => Loc.GetString($"cmd-show-spread-desc");
    public override string Help => $"{Command}";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _gun.SpreadOverlay ^= true;

        shell.WriteLine(Loc.GetString($"cmd-show-spread-status", ("status", _gun.SpreadOverlay)));
    }
}
