using System.Linq;
using Content.Server.Administration.BanList;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

/// <summary>
///     Lists someones active Ban Ids or opens a window to see them.
/// </summary>
[AdminCommand(AdminFlags.Ban)]
public sealed class BanListCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;

    public override string Command => "banlist";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        var data = await _locator.LookupIdByNameOrIdAsync(args[0]);

        if (data == null)
        {
            shell.WriteError(Loc.GetString("cmd-ban-player"));
            return;
        }

        if (shell.Player is not { } player)
        {
            var bans = await _dbManager.GetServerBansAsync(data.LastAddress, data.UserId, data.LastHWId, false);

            if (bans.Count == 0)
            {
                shell.WriteLine(Loc.GetString("cmd-banlist-empty", ("user", data.Username)));
                return;
            }

            foreach (var ban in bans)
            {
                var msg = $"{ban.Id}: {ban.Reason}";
                shell.WriteLine(msg);
            }

            return;
        }

        var ui = new BanListEui();
        _eui.OpenEui(ui, player);
        await ui.ChangeBanListPlayer(data.UserId);
    }


    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var playerMgr = IoCManager.Resolve<IPlayerManager>();
        var options = playerMgr.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
        return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-banlist-hint"));
    }
}
