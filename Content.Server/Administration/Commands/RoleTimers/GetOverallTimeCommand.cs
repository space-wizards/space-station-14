using Content.Server.Roles;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.RoleTimers;

[AdminCommand(AdminFlags.Admin)]
public sealed class GetOverallTimeCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly RoleTimerManager _roleTimerManager = default!;

    public string Command => "getoveralltime";
    public string Description => Loc.GetString("cmd-getoveralltime-desc");
    public string Help => Loc.GetString("cmd-getoveralltime-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-getoveralltime-error-args"));
            return;
        }

        var userName = args[0];
        if (!_playerManager.TryGetUserId(userName, out var userId))
        {
            shell.WriteError(Loc.GetString("parser-session-fail", ("username", userName)));
            return;
        }

        var timers = _roleTimerManager.GetOverallPlaytime(userId).Result;
        shell.WriteLine(Loc.GetString(
            "cmd-getoveralltime-success",
            ("username", userName),
            ("time", timers.TotalMinutes)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-getoveralltime-arg-user"));
        }

        return CompletionResult.Empty;
    }
}
