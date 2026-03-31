using Content.Server.Administration.UI;
using Content.Server.Commands;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class AnnounceUiCommand : LocalizedEntityCommands
{
    [Dependency] private readonly EuiManager _euiManager = default!;

    public override string Command => "announceui";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (CommandChecks.MustNotBeServer(shell, out var player))
            _euiManager.OpenEui(new AdminAnnounceEui(), player);
    }
}
