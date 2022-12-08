using System.Linq;
using System.Threading;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

/// <summary>
/// Lists someones active Ban Ids.
/// </summary>
[AdminCommand(AdminFlags.Ban)]
public sealed class ListBansCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerLocator _locater = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    public string Command => $"listbans";
    public string Description => Loc.GetString("cmd-banlist-desc");
    public string Help => Loc.GetString("cmd-banlist-help", ("Command", Command));
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            return;
        }

        var data = await _locater.LookupIdByNameOrIdAsync(args[0], CancellationToken.None);

        if (data == null)
        {
            shell.WriteError(Loc.GetString("cmd-ban-player"));
            return;
        }

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
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var playerMgr = IoCManager.Resolve<IPlayerManager>();
            var options = playerMgr.ServerSessions.Select(c => c.Name).OrderBy(c => c).ToArray();
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-ban-hint"));
        }

        return CompletionResult.Empty;
    }
}
