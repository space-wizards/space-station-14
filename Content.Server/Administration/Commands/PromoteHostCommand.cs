using Content.Server.Administration.Managers;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [UsedImplicitly]
    public sealed class PromoteHostCommand : IConsoleCommand
    {
        public string Command => "promotehost";
        public string Description => "Grants client temporary full host admin privileges. Use this to bootstrap admins.";
        public string Help => "Usage promotehost <player>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine("Expected exactly one argument.");
                return;
            }

            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            if (!plyMgr.TryGetSessionByUsername(args[0], out var targetPlayer))
            {
                shell.WriteLine("Unable to find a player by that name.");
                return;
            }

            var adminMgr = IoCManager.Resolve<IAdminManager>();
            adminMgr.PromoteHost(targetPlayer);
        }
    }
}
