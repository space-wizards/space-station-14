#nullable enable
using JetBrains.Annotations;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Commands
{
    [UsedImplicitly]
    public sealed class PromoteHostCommand : IClientCommand
    {
        public string Command => "promotehost";
        public string Description => "Grants client temporary full host admin privileges. Use this to bootstrap admins.";
        public string Help => "Usage promotehost <player>";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Expected exactly one argument.");
                return;
            }

            var plyMgr = IoCManager.Resolve<IPlayerManager>();
            if (!plyMgr.TryGetSessionByUsername(args[0], out var targetPlayer))
            {
                shell.SendText(player, "Unable to find a player by that name.");
                return;
            }

            var adminMgr = IoCManager.Resolve<IAdminManager>();
            adminMgr.PromoteHost(targetPlayer);
        }
    }
}
