using Content.Shared.Administration;
using Robust.Shared.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.EUI;
using Robust.Server.Player;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class BanPanelCommand : LocalizedCommands
{

    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly EuiManager _euis = default!;

    public override string Command => "banpanel";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not IPlayerSession player)
        {
            shell.WriteError(Loc.GetString("cmd-banpanel-server"));
            return;
        }

        switch (args.Length)
        {
            case 0:
                _euis.OpenEui(new BanPanelEui(), player);
                break;
            case 1:
                var located = await _locator.LookupIdByNameOrIdAsync(args[0]);
                if (located is null)
                {
                    shell.WriteError(Loc.GetString("cmd-banpanel-player-err"));
                    return;
                }
                var ui = new BanPanelEui();
                _euis.OpenEui(ui, player);
                ui.ChangePlayer(located.UserId, located.Username, located.LastAddress, located.LastHWId);
                break;
            default:
                shell.WriteLine(Loc.GetString("cmd-ban-invalid-arguments"));
                shell.WriteLine(Help);
                return;
        }
    }
}
