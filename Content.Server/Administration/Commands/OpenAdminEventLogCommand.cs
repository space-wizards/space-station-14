using Content.Server.Administration.AdminEventLog;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class OpenAdminEventLogCommand : LocalizedCommands
{
    [Dependency] private EuiManager _euiManager = default!;

    public override string Command => "eventlog";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var ui = new AdminEventLogEui();
        _euiManager.OpenEui(ui, player);
    }
}
