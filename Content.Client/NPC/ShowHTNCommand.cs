using Content.Client.NPC.HTN;
using Robust.Shared.Console;

namespace Content.Client.NPC;

public sealed class ShowHTNCommand : LocalizedEntityCommands
{
    [Dependency] private readonly HTNSystem _htn = default!;

    public override string Command => "showhtn";
    public override string Description => "Shows the current status for HTN NPCs";
    public override string Help => $"{Command}";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _htn.EnableOverlay ^= true;
    }
}
