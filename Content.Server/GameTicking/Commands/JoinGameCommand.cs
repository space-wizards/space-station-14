using System.Collections.Generic;
using Content.Server.Administration;
using Content.Server.Roles;
using Content.Server.Station;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
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

            var ticker = EntitySystem.Get<GameTicker>();
            var stationSystem = EntitySystem.Get<StationSystem>();
            if (ticker.RunLevel == GameRunLevel.PreRoundLobby)
            {
                shell.WriteLine("Round has not started.");
                return;
            }
            else if(ticker.RunLevel == GameRunLevel.InRound)
            {
                string ID = args[0];
                var stationId = new StationSystem.StationId(uint.Parse(args[1]));

                if(!stationSystem.IsJobAvailableOnStation(stationId, ID))
                {
                    var jobPrototype = _prototypeManager.Index<JobPrototype>(ID);
                    shell.WriteLine($"{jobPrototype.Name} has no available slots.");
                    return;
                }
                ticker.MakeJoinGame(player, stationId, ID);
                return;
            }

            ticker.MakeJoinGame(player, StationSystem.StationId.Invalid);
        }
    }
}
