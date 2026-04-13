using Content.Server.Administration;
using Content.Server.Administration.AuditLog;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Content.Server.Administration.AuditLog;

[AdminCommand(AdminFlags.Logs)]
public sealed class OpenAuditLogsCommand : IConsoleCommand
{
    public string Command => "auditlogs";
    public string Description => "Opens the admin audit log viewer.";
    public string Help => "auditlogs";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteError("This command cannot be run from the server console.");
            return;
        }

        var eui = IoCManager.Resolve<EuiManager>();
        var ui = new AdminAuditLogsEui();
        eui.OpenEui(ui, player);
    }
}
