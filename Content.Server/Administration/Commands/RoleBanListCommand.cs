using Content.Server.Administration.BanList;
using Content.Server.EUI;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class RoleBanListCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly EuiManager _eui = default!;

    public override string Command => "rolebanlist";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var includeUnbanned = true;
        if (args.Length == 2 && !bool.TryParse(args[1], out includeUnbanned))
        {
            shell.WriteLine($"Argument two ({args[1]}) is not a boolean.");
            return;
        }

        var data = await _locator.LookupIdByNameOrIdAsync(args[0]);

        if (data == null)
        {
            shell.WriteError(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        if (shell.Player is not { } player)
        {
            var bans = await _dbManager.GetServerRoleBansAsync(data.LastAddress, data.UserId, data.LastLegacyHWId, data.LastModernHWIds, includeUnbanned);

            if (bans.Count == 0)
            {
                shell.WriteLine(Loc.GetString("shell-rolebanlist-no-recorded-bans"));
                return;
            }

            foreach (var ban in bans)
            {
                shell.WriteLine(Loc.GetString("cmd-rolebanlist-shell-output",
                    ("banId", $"{ban.Id}"),
                    ("role", ban.Role),
                    ("reason", ban.Reason)));
            }
            return;
        }

        var ui = new BanListEui(1);
        _eui.OpenEui(ui, player);
        await ui.ChangeBanListPlayer(data.UserId);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
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
