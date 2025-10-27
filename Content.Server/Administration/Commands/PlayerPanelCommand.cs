using System.Linq;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class PlayerPanelCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly EuiManager _euis = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public override string Command => "playerpanel";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } admin)
        {
            shell.WriteError(Loc.GetString("cmd-playerpanel-server"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-playerpanel-invalid-arguments"));
            return;
        }

        var queriedPlayer = await _locator.LookupIdByNameOrIdAsync(args[0]);

        if (queriedPlayer == null)
        {
            shell.WriteError(Loc.GetString("cmd-playerpanel-invalid-player"));
            return;
        }

        var ui = new PlayerPanelEui(queriedPlayer);
        _euis.OpenEui(ui, admin);
        ui.SetPlayerState();
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _players.Sessions.OrderBy(c => c.Name).Select(c => c.Name).ToArray();

            return CompletionResult.FromHintOptions(options, LocalizationManager.GetString("cmd-playerpanel-completion"));
        }

        return CompletionResult.Empty;
    }
}
