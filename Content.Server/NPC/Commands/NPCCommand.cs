using Content.Server.Administration;
using Content.Server.Commands;
using Content.Server.EUI;
using Content.Server.NPC.UI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.NPC.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class NpcCommand : LocalizedEntityCommands
{
    [Dependency] private readonly EuiManager _euiManager = default!;

    public override string Command => "npc";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (CommandChecks.MustNotBeServer(shell, out var player))
            _euiManager.OpenEui(new NPCEui(), player);
    }
}
