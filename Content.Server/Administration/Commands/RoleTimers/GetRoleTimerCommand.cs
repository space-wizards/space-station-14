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
        public string Description => "Gets all or one role timers from a player";
        public string Help => $"Usage: {Command} <name or user ID> [role]";

        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length == 0)
            {
                shell.WriteLine("Name a player to get the role timer information from");
                return;
            }

            // TODO: Loc

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
                    shell.WriteLine("Found no role timers");
                    return;
                }

                foreach (var (role, time) in timers)
                {
                    shell.WriteLine($"Role: {role}, Playtime: {time}");
                }
            }

            if (args.Length >= 2)
            {
                if (args[1] == "Overall")
                {
                    var timer = roles.GetOverallPlaytime(pSession.UserId).Result;
                    shell.WriteLine($"Overall playtime is {timer.TotalMinutes}");
                    return;
                }

                var time = roles.GetPlayTimeForRole(pSession.UserId, args[1]).Result;
                shell.WriteLine($"Playtime for {args[1]} is: {time}");
            }
        }
    }
}
