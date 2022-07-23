using Content.Server.Roles;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.RoleTimers
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class GetRoleTimerCommand : IConsoleCommand
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly RoleTimerManager _roleTimerManager = default!;

        public string Command => "getroletimers";
        public string Description => Loc.GetString("cmd-getroletimers-desc");
        public string Help => Loc.GetString("cmd-getroletimers-help", ("command", Command));

        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length is not (1 or 2))
            {
                shell.WriteLine(Loc.GetString("cmd-getroletimers-error-args"));
                return;
            }

            var userName = args[0];
            if (!_playerManager.TryGetSessionByUsername(userName, out var session))
            {
                shell.WriteError(Loc.GetString("parser-session-fail", ("username", userName)));
                return;
            }

            if (args.Length == 1)
            {
                var timers = _roleTimerManager.GetRolePlaytimes(session.UserId).Result;

                if (timers.Count == 0)
                {
                    shell.WriteLine(Loc.GetString("cmd-getroletimers-no"));
                    return;
                }

                foreach (var (role, time) in timers)
                {
                    shell.WriteLine(Loc.GetString("cmd-getroletimers-role", ("role", role), ("time", time.TotalMinutes)));
                }
            }

            if (args.Length >= 2)
            {
                if (args[1] == "Overall")
                {
                    var timer = _roleTimerManager.GetOverallPlaytime(session.UserId).Result;
                    shell.WriteLine(Loc.GetString("cmd-getroletimers-overall", ("time", timer.TotalMinutes)));
                    return;
                }

                var time = _roleTimerManager.GetPlayTimeForRole(session.UserId, args[1]).Result;
                shell.WriteLine(Loc.GetString("cmd-getroletimers-succeed", ("username", session.Name), ("time", time.TotalMinutes)));
            }
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                return CompletionResult.FromHintOptions(
                    CompletionHelper.SessionNames(players: _playerManager),
                    Loc.GetString("cmd-getroletimers-arg-user"));
            }

            if (args.Length == 2)
                return CompletionResult.FromHint("cmd-getroletimers-arg-role");

            return CompletionResult.Empty;
        }
    }
}
