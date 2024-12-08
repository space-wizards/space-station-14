using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server.EUI;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class BanUsernamePanelCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly EuiManager _euis = default!;


    public override string Command => "banusernamepanel";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (args.Length > 0)
        {
            shell.WriteError(Loc.GetString("shell-takes-no-arguments"));
            shell.WriteLine(Help);
            return;
        }

        _euis.OpenEui(new BanUsernamePanelEui(), player);
    }
}
