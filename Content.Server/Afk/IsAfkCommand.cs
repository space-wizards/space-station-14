using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Content.Server.Afk
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class IsAfkCommand : IConsoleCommand
    {
        public string Command => "isafk";
        public string Description => "Checks if a specified player is AFK";
        public string Help => "Usage: isafk <playerName>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var playerManager = IoCManager.Resolve<IPlayerManager>();
            var afkManager = IoCManager.Resolve<IAfkManager>();

            if (args.Length == 0)
            {
                shell.WriteError("Need one argument");
                return;
            }

            if (!playerManager.TryGetSessionByUsername(args[0], out var player))
            {
                shell.WriteError("Unable to find that player");
                return;
            }

            shell.WriteLine(afkManager.IsAfk(player) ? "They are indeed AFK" : "They are not AFK");
        }
    }
}
