using System;
using Content.Server.Administration;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.Administration;
using Robust.Server.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;

namespace Content.Server.Commands.GameTicking
{
    [AdminCommand(AdminFlags.Server)]
    public class NewRoundCommand : IServerCommand
    {
        public string Command => "restartround";
        public string Description => "Moves the server from PostRound to a new PreRoundLobby.";
        public string Help => String.Empty;

        public void Execute(IServerConsoleShell shell, string argStr, string[] args)
        {
            var ticker = IoCManager.Resolve<IGameTicker>();
            ticker.RestartRound();
        }
    }
}