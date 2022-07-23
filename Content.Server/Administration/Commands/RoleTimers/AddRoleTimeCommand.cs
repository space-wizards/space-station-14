using Content.Server.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.RoleTimers;

public sealed class AddRoleTimeCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly RoleTimerManager _roleTimerManager = default!;

    public string Command => "addroletime";
    public string Description => Loc.GetString("cmd-addroletime-desc");
    public string Help => Loc.GetString("cmd-addroletime-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Loc.GetString("cmd-addroletime-error-args"));
            return;
        }

        var userName = args[0];
        if (!_playerManager.TryGetUserId(userName, out var userId))
        {
            shell.WriteError(Loc.GetString("parse-session-fail", ("username", userName)));
            return;
        }

        var role = args[1];

        var m = args[2];
        if (!int.TryParse(m, out var minutes))
        {
            shell.WriteError(Loc.GetString("parse-minutes-fail", ("minutes", minutes)));
            return;
        }

        _roleTimerManager.AddTimeToRole(userId, role, TimeSpan.FromMinutes(minutes));
#pragma warning disable RA0004
        var timers = _roleTimerManager.GetOverallPlaytime(userId).Result;
#pragma warning restore RA0004
        shell.WriteLine(Loc.GetString("cmd-addroletime-succeed",
            ("username", userName),
            ("role", role),
            ("time", timers.TotalMinutes)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-addroletime-arg-user"));
        }

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString("cmd-addroletime-arg-role"));

        if (args.Length == 3)
            return CompletionResult.FromHint(Loc.GetString("cmd-addroletime-arg-minutes"));

        return CompletionResult.Empty;
    }
}
