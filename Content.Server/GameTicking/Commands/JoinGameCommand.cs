using System.Collections.Generic;
using Content.Server.Administration;
using Content.Server.Roles;
using Content.Server.Station;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
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
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            var player = shell.Player as IPlayerSession;

            if (player == null)
            {
                return;
            }

            var ticker = EntitySystem.Get<GameTicker>();
            var stationSystem = EntitySystem.Get<StationSystem>();

            if (!ticker.PlayersInLobby.ContainsKey(player))
            {
                shell.WriteError($"{player.Name} is not in the lobby.   This incident will be reported.");
                return;
            }

            if (ticker.RunLevel == GameRunLevel.PreRoundLobby)
            {
                shell.WriteLine("Round has not started.");
                return;
            }
            else if (ticker.RunLevel == GameRunLevel.InRound)
            {
                string id = args[0];

                if (!uint.TryParse(args[1], out var sid))
                {
                    shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
                }

                var stationId = new StationId(sid);
                var jobPrototype = _prototypeManager.Index<JobPrototype>(id);
                if(!stationSystem.IsJobAvailableOnStation(stationId, jobPrototype))
                {
                    shell.WriteLine($"{jobPrototype.Name} has no available slots.");
                    return;
                }
                ticker.MakeJoinGame(player, stationId, id);
                return;
            }

            ticker.MakeJoinGame(player, StationId.Invalid);
        }
    }
}
