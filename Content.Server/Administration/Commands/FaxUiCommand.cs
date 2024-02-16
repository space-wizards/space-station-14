using Content.Server.EUI;
using Content.Server.Fax.AdminUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class FaxUiCommand : IConsoleCommand
{
    public string Command => "faxui";

    public string Description => Loc.GetString("cmd-faxui-desc");
    public string Help => Loc.GetString("cmd-faxui-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine("shell-only-players-can-run-this-command");
            return;
        }

        var eui = IoCManager.Resolve<EuiManager>();
        var ui = new AdminFaxEui();
        eui.OpenEui(ui, player);
    }
}

