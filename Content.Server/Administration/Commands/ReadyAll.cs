#nullable enable
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.Administration;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Server)]
    public class ReadyAll : IClientCommand
    {
        public string Command => "readyall";
        public string Description => "Readies up all players in the lobby.";
        public string Help => $"{Command} | ̣{Command} <ready>";
        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            var ready = true;

            if (args.Length > 0)
            {
                ready = bool.Parse(args[0]);
            }

            var gameTicker = IoCManager.Resolve<IGameTicker>();
            var playerManager = IoCManager.Resolve<IPlayerManager>();


            if (gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
            {
                shell.SendText(player, "This command can only be ran while in the lobby!");
                return;
            }

            foreach (var p in playerManager.GetAllPlayers())
            {
                gameTicker.ToggleReady(p, ready);
            }
        }
    }
}
