using System.Collections.Generic;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.Roles;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Commands.GameTicking
{
    [AnyCommand]
    class JoinGameCommand : IClientCommand
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public string Command => "joingame";
        public string Description => "";
        public string Help => "";

        public JoinGameCommand()
        {
            IoCManager.InjectDependencies(this);
        }
        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var output = string.Join(".", args);
            if (player == null)
            {
                return;
            }

            var ticker = IoCManager.Resolve<IGameTicker>();
            if (ticker.RunLevel == GameRunLevel.PreRoundLobby)
            {
                shell.SendText(player, "Round has not started.");
                return;
            }
            else if(ticker.RunLevel == GameRunLevel.InRound)
            {
                string ID = args[0];
                var positions = ticker.GetAvailablePositions();

                if(positions.GetValueOrDefault(ID, 0) == 0) //n < 0 is treated as infinite
                {
                    var jobPrototype = _prototypeManager.Index<JobPrototype>(ID);
                    shell.SendText(player, $"{jobPrototype.Name} has no available slots.");
                    return;
                }
                ticker.MakeJoinGame(player, args[0].ToString());
                return;
            }

            ticker.MakeJoinGame(player, null);
        }
    }
}