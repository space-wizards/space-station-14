using System.Linq;
using System.Text;
using Content.Server.Administration.BanList;
using Content.Server.EUI;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class RoleBanListCommand : IConsoleCommand
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    [Dependency] private readonly EuiManager _eui = default!;

    [Dependency] private readonly IPlayerLocator _locator = default!;

    public string Command => "rolebanlist";
    public string Description => Loc.GetString("cmd-rolebanlist-desc");
    public string Help => Loc.GetString("cmd-rolebanlist-help");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1 && args.Length != 2)
        {
            shell.WriteLine(Loc.GetString("cmd-rolebanlist-invalid-args", ("help", Help)));
            return;
        }

        var includeUnbanned = true;
        if (args.Length == 2 && !bool.TryParse(args[1], out includeUnbanned))
        {
            shell.WriteLine(Loc.GetString("cmd-rolebanlist-arg2-not-bool", ("arg", args[1])));
            return;
        }

        var data = await _locator.LookupIdByNameOrIdAsync(args[0]);

        if (data == null)
        {
            shell.WriteError(Loc.GetString("cmd-rolebanlist-player-not-found"));
            return;
        }

        if (shell.Player is not { } player)
        {

            var bans = await _dbManager.GetServerRoleBansAsync(data.LastAddress, data.UserId, data.LastLegacyHWId, data.LastModernHWIds, includeUnbanned);

            if (bans.Count == 0)
            {
                shell.WriteLine(Loc.GetString("cmd-rolebanlist-no-bans", ("user", data.Username)));
                return;
            }

            foreach (var ban in bans)
            {
                var msg = $"ID: {ban.Id}: Role: {ban.Role} Reason: {ban.Reason}";
                shell.WriteLine(msg);
            }
            return;
        }

        var ui = new BanListEui();
        _eui.OpenEui(ui, player);
        await ui.ChangeBanListPlayer(data.UserId);

    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(),
                Loc.GetString("cmd-rolebanlist-hint-1")),
            2 => CompletionResult.FromHintOptions(CompletionHelper.Booleans,
                Loc.GetString("cmd-rolebanlist-hint-2")),
            _ => CompletionResult.Empty
        };
    }
}
