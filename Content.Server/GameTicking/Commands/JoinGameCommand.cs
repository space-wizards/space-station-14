using Content.Server.Administration.Managers;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Commands
{
    [AnyCommand]
    sealed class JoinGameCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly StationJobsSystem _stationJobsSystem = default!;

        private ISawmill Sawmill => _sawmill ??= _logManager.GetSawmill("security");
        private ISawmill? _sawmill;

        public override string Command => "joingame";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
                return;
            }

            var player = shell.Player;

            if (player == null)
            {
                return;
            }

            if (_gameTicker.PlayerGameStatuses.TryGetValue(player.UserId, out var status) && status == PlayerGameStatus.JoinedGame)
            {
                Sawmill.Info($"{player.Name} ({player.UserId}) attempted to latejoin while in-game.");
                shell.WriteError(Loc.GetString("cmd-joingame-not-in-lobby", ("player", player.Name)));
                return;
            }

            if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
            {
                shell.WriteLine(Loc.GetString("cmd-joingame-round-not-started"));
                return;
            }
            else if (_gameTicker.RunLevel == GameRunLevel.InRound)
            {
                string id = args[0];

                if (!int.TryParse(args[1], out var sid))
                {
                    shell.WriteError(Loc.GetString("shell-argument-must-be-number"));
                }

                var station = _entManager.GetEntity(new NetEntity(sid));
                var jobPrototype = _prototypeManager.Index<JobPrototype>(id);
                if(_stationJobsSystem.TryGetJobSlot(station, jobPrototype, out var slots) == false || slots == 0)
                {
                    shell.WriteLine(Loc.GetString("cmd-joingame-no-available-slots", ("job", jobPrototype.LocalizedName)));
                    return;
                }

                if (_adminManager.IsAdmin(player) && _cfg.GetCVar(CCVars.AdminDeadminOnJoin))
                {
                    _adminManager.DeAdmin(player);
                }

                _gameTicker.MakeJoinGame(player, station, id);
                return;
            }

            _gameTicker.MakeJoinGame(player, EntityUid.Invalid);
        }
    }
}
