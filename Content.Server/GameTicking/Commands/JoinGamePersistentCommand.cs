using Content.Server.Administration.Managers;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.GameTicking.Commands
{
    [AnyCommand]
    sealed class JoinGamePersistentCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public string Command => "joingamepersistent";
        public string Description => "Saves a character and puts it in game.";
        public string Help => "Do not manually execute.";

        public JoinGamePersistentCommand()
        {
            IoCManager.InjectDependencies(this);
        }
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {


            var player = shell.Player;

            if (player == null)
            {
                return;
            }

            var ticker = _entManager.System<GameTicker>();
            var stationJobs = _entManager.System<StationJobsSystem>();

            if (ticker.PlayerGameStatuses.TryGetValue(player.UserId, out var status) && status == PlayerGameStatus.JoinedGame)
            {
                Logger.InfoS("security", $"{player.Name} ({player.UserId}) attempted to latejoin while in-game.");
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
                if (_adminManager.IsAdmin(player) && _cfg.GetCVar(CCVars.AdminDeadminOnJoin))
                {
                    _adminManager.DeAdmin(player);
                }
                if (args.Count() > 0)
                {
                    bool stat = bool.Parse(args[0]);
                    if (stat)
                    {
                        ticker.MakeJoinGamePersistentLoad(player);
                    }
                    else
                    {
                        ticker.MakeJoinGamePersistent(player);
                    }
                }
                return;
            }
            
               
        }
    }
}
