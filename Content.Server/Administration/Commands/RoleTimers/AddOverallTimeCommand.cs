using Content.Server.Roles;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.RoleTimers;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddOverallTimeCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly RoleTimerManager _roleTimerManager = default!;

    public string Command => "addoveralltime";
    public string Description => Loc.GetString("cmd-addoveralltime-desc");
    public string Help => Loc.GetString("cmd-addoveralltime-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("cmd-addoveralltime-error-args"));
            return;
        }

        if (!int.TryParse(args[1], out var minutes))
        {
            shell.WriteError(Loc.GetString("parse-minutes-fail", ("minutes", args[1])));
            return;
        }

        if (!_playerManager.TryGetUserId(args[0], out var userId))
        {
            shell.WriteError(Loc.GetString("parse-session-fail", ("username", args[0])));
            return;
        }

        _roleTimerManager.AddTimeToOverallPlaytime(userId, TimeSpan.FromMinutes(minutes));
#pragma warning disable RA0004
        var timers = _roleTimerManager.GetOverallPlaytime(userId).Result;
#pragma warning restore RA0004

        shell.WriteLine(Loc.GetString(
            "cmd-addoveralltime-succeed",
            ("username", args[0]),
            ("time", timers.TotalMinutes)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("cmd-addoveralltime-arg-user"));

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString("cmd-addoveralltime-arg-minutes"));

        return CompletionResult.Empty;
    }
}
