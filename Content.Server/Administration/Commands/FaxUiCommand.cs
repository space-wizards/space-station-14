using Content.Server.EUI;
using Content.Server.Fax.AdminUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class FaxUiCommand : LocalizedEntityCommands
{
    [Dependency] private readonly EuiManager _euiManager = default!;

    public override string Command => "faxui";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var ui = new AdminFaxEui();
        _euiManager.OpenEui(ui, player);
    }
}
