using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Content.Server.Database;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.MassBan)]
public sealed class BanMassCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IBanManager _bans = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    public override string Command => "banmass";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var severity = _bans.GetServerBanSeverity();

        if (args.Length < 3)
        {
            shell.WriteLine(Loc.GetString("cmd-ban-invalid-arguments"));
            shell.WriteLine(Help);
            return;
        }

        var reason = args[0];

        if (!uint.TryParse(args[1], out var minutes))
        {
            shell.WriteLine(Loc.GetString("cmd-ban-invalid-minutes", ("minutes", args[1])));
            shell.WriteLine(Help);
            return;
        }

        var player = shell.Player;
        var allTargets = args.Skip(2).Where(arg => !string.IsNullOrWhiteSpace(arg)).ToArray();

        foreach (var target in allTargets)
        {
            var trimmedTarget = target.Trim();
            if (string.IsNullOrWhiteSpace(trimmedTarget))
                continue;

            var located = await _locator.LookupIdByNameOrIdAsync(trimmedTarget);

            if (located == null)
            {
                shell.WriteError(Loc.GetString("cmd-ban-player"));
                continue;
            }

            var targetUid = located.UserId;
            var targetHWid = located.LastHWId;

            _bans.CreateServerBan(targetUid, trimmedTarget, player?.UserId, null, targetHWid, minutes, severity, reason);
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length >= 3)
        {
            var options = _playerManager.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
            return CompletionResult.FromHintOptions(
                options,
                Loc.GetString("cmd-ban-hint"));
        }

        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("cmd-ban-hint-reason"));

        if (args.Length == 2)
        {
            var durations = _bans.BanDurations;

            return CompletionResult.FromHintOptions(durations, Loc.GetString("cmd-ban-hint-duration"));
        }

        return CompletionResult.Empty;
    }
}
