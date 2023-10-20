using Content.Shared.Mind;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.GameTicking.Commands
{
    sealed class RespawnCommand : IConsoleCommand
    {
        public string Command => "respawn";
        public string Description => "Respawns a player, kicking them back to the lobby.";
        public string Help => "respawn [player]";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (args.Length > 1)
            {
                shell.WriteLine("Must provide <= 1 argument.");
                return;
            }

            var playerMgr = IoCManager.Resolve<IPlayerManager>();
            var sysMan = IoCManager.Resolve<IEntitySystemManager>();
            var ticker = sysMan.GetEntitySystem<GameTicker>();
            var mind = sysMan.GetEntitySystem<SharedMindSystem>();

            NetUserId userId;
            if (args.Length == 0)
            {
                if (player == null)
                {
                    shell.WriteLine("If not a player, an argument must be given.");
                    return;
                }

                userId = player.UserId;
            }
            else if (!playerMgr.TryGetUserId(args[0], out userId))
            {
                shell.WriteLine("Unknown player");
                return;
            }

            if (!playerMgr.TryGetSessionById(userId, out var targetPlayer))
            {
                if (!playerMgr.TryGetPlayerData(userId, out var data))
                {
                    shell.WriteLine("Unknown player");
                    return;
                }

                mind.WipeMind(data.ContentData()?.Mind);
                shell.WriteLine("Player is not currently online, but they will respawn if they come back online");
                return;
            }

            ticker.Respawn(targetPlayer);
        }
    }
}
