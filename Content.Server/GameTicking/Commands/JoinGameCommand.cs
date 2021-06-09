using System.Collections.Generic;
using Content.Server.Administration;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Commands
{
    [AnyCommand]
    class JoinGameCommand : IConsoleCommand
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public string Command => "joingame";
        public string Description => "";
        public string Help => "";

        public JoinGameCommand()
        {
            IoCManager.InjectDependencies(this);
        }
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            var output = string.Join(".", args);
            if (player == null)
            {
                return;
            }

            var ticker = IoCManager.Resolve<IGameTicker>();
            if (ticker.RunLevel == GameRunLevel.PreRoundLobby)
            {
                shell.WriteLine("Round has not started.");
                return;
            }
            else if(ticker.RunLevel == GameRunLevel.InRound)
            {
                string ID = args[0];
                var positions = ticker.GetAvailablePositions();

                if(positions.GetValueOrDefault(ID, 0) == 0) //n < 0 is treated as infinite
                {
                    var jobPrototype = _prototypeManager.Index<JobPrototype>(ID);
                    shell.WriteLine($"{jobPrototype.Name} has no available slots.");
                    return;
                }
                ticker.MakeJoinGame(player, args[0]);
                return;
            }

            ticker.MakeJoinGame(player);
        }
    }
}
