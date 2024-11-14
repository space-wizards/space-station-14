using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class AsnBanPanelCommand : LocalizedCommands
{

    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly EuiManager _euis = default!;

    public override string Command => "asnbanpanel";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        switch (args.Length)
        {
            case 0:
                _euis.OpenEui(new AsnBanPanelEui(), player);
                break;
            default:
                shell.WriteLine(Loc.GetString("cmd-asnbanpanel-invalid-arguments"));
                shell.WriteLine(Help);
                return;
        }
    }
}
