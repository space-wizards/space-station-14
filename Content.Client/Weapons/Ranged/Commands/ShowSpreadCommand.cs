using Content.Client.Weapons.Ranged.Systems;
using Robust.Shared.Console;

namespace Content.Client.Weapons.Ranged.Commands;

public sealed class ShowSpreadCommand : LocalizedEntityCommands
{
    [Dependency] private readonly GunSystem _gunSystem = default!;

    public override string Command => "showgunspread";

    public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _gunSystem.SpreadOverlay ^= true;

        shell.WriteLine(Loc.GetString($"cmd-{Command}-status", ("status", _gunSystem.SpreadOverlay)));
    }
}
