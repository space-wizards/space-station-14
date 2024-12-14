using Content.Server.EUI;
using Content.Server.Fax.AdminUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class FaxUiCommand : IConsoleCommand
{
    public string Command => "faxui";

    public string Description => Loc.GetString("cmd-faxui-desc");
    public string Help => Loc.GetString("cmd-faxui-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var eui = IoCManager.Resolve<EuiManager>();
        var ui = new AdminFaxEui();
        eui.OpenEui(ui, player);
    }
}
