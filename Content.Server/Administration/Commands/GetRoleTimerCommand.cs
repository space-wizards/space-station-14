using Content.Server.Database;
using Content.Server.RoleTimers;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
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
            };

            var playerManager = IoCManager.Resolve<IPlayerManager>();

            var target = args[0];
            IPlayerSession targetSessionInst;
            if (playerManager.TryGetSessionByUsername(target, out var targetSession))
            {
                targetSessionInst = targetSession;
            }
            else
            {
                shell.WriteLine("Not a valid player");
                return;
            }

            if (args.Length == 1)
            {
                var rt = IoCManager.Resolve<RoleTimerManager>();
                var timers = rt.GetCachedRoleTimersForPlayer(targetSessionInst.UserId);
                if (timers == null)
                {
                    shell.WriteLine("Couldn't get any information from cache (player info may not be cached yet)");
                    return;
                }

                foreach (var (role, time) in timers)
                {
                    shell.WriteLine($"Role: {role}, Playtime: {time}");
                }
            }

            if (args.Length >= 2)
            {
                var rt = IoCManager.Resolve<RoleTimerManager>();
                var time = rt.GetPlayTimeForRole(targetSessionInst.UserId, args[1]);
                if (time != null)
                {
                    shell.WriteLine($"Playtime: {time}");
                }
                else
                {
                    shell.WriteLine("Couldn't find that role in the cache, id may be misspelled or user isn't cached yet.");
                }
            }
        }
    }
}
