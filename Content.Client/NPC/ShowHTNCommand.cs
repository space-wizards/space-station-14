using Content.Client.NPC.HTN;
using Robust.Shared.Console;

namespace Content.Client.NPC;

public sealed class ShowHtnCommand : LocalizedEntityCommands
{
    [Dependency] private readonly HTNSystem _htnSystem = default!;

    public override string Command => "showhtn";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _htnSystem.EnableOverlay ^= true;
    }
}
