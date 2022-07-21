using Content.Server.Roles;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands.RoleTimers
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class GetRoleTimerCommand : IConsoleCommand
    {
        public string Command => "getroletimers";
        public string Description => Loc.GetString("get-role-time-desc");
        public string Help => Loc.GetString("get-role-time-help", ("command", $"{Command}"));

        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 0)
            {
                shell.WriteLine(Loc.GetString("get-role-time-help-plain"));
                return;
            }

            if (shell.Player == null)
            {
                // TODO: Error that it can't run on server.
                return;
            }

            var pSession = (IPlayerSession) shell.Player;
            var roles = IoCManager.Resolve<RoleTimerManager>();

            if (args.Length == 1)
            {
                var timers = roles.GetRolePlaytimes(pSession.UserId).Result;

                if (timers.Count == 0)
                {
                    shell.WriteLine(Loc.GetString("get-role-time-no"));
                    return;
                }

                foreach (var (role, time) in timers)
                {
                    shell.WriteLine(Loc.GetString("get-role-time-role", ("role", role), ("time", $"{time.TotalMinutes:0}")));
                }
            }

            if (args.Length >= 2)
            {
                if (args[1] == "Overall")
                {
                    var timer = roles.GetOverallPlaytime(pSession.UserId).Result;
                    shell.WriteLine(Loc.GetString("get-role-time-overall", ("time", $"{timer.TotalMinutes:0}")));
                    return;
                }

                var time = roles.GetPlayTimeForRole(pSession.UserId, args[1]).Result;
                shell.WriteLine(Loc.GetString("get-role-time-succeed", ("userid", pSession.UserId), ("time", $"{time.TotalMinutes:0}")));
            }
        }
    }
}
