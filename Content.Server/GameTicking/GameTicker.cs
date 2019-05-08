using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Markers;
using Content.Server.GameTicking.GamePresets;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs;
using Content.Server.Players;
using Content.Shared;
using Content.Shared.GameObjects.Components.Inventory;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Maps;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameTicking
{
    public class GameTicker : SharedGameTicker, IGameTicker
    {
        [ViewVariables]
        public GameRunLevel RunLevel
        {
            get => _runLevel;
            private set
            {
                if (_runLevel == value)
                {
                    return;
                }

                var old = _runLevel;
                _runLevel = value;

                OnRunLevelChanged?.Invoke(new GameRunLevelChangedEventArgs(old, value));
            }
        }

        public event Action<GameRunLevelChangedEventArgs> OnRunLevelChanged;

        private const string PlayerPrototypeName = "HumanMob_Content";
        private const string ObserverPrototypeName = "MobObserver";
        private const string MapFile = "Maps/stationstation.yml";

        // Seconds.
        private const float LobbyDuration = 20;

        [ViewVariables] private bool _initialized;
        [ViewVariables(VVAccess.ReadWrite)] private GridCoordinates _spawnPoint;
        [ViewVariables] private GameRunLevel _runLevel;

        [ViewVariables] private bool LobbyEnabled => _configurationManager.GetCVar<bool>("game.lobbyenabled");

        // Value is whether they're ready.
        [ViewVariables]
        private readonly Dictionary<IPlayerSession, bool> _playersInLobby = new Dictionary<IPlayerSession, bool>();

        [ViewVariables] private bool _roundStartCountdownHasNotStartedYetDueToNoPlayers;
        private DateTime _roundStartTimeUtc;

        private readonly Random _spawnRandom = new Random();

        [ViewVariables] private readonly List<GameRule> _gameRules = new List<GameRule>();

#pragma warning disable 649
        [Dependency] private IEntityManager _entityManager;
        [Dependency] private IMapManager _mapManager;
        [Dependency] private IMapLoader _mapLoader;
        [Dependency] private IGameTiming _gameTiming;
        [Dependency] private IConfigurationManager _configurationManager;
        [Dependency] private IPlayerManager _playerManager;
        [Dependency] private IChatManager _chatManager;
        [Dependency] private IServerNetManager _netManager;
        [Dependency] private IDynamicTypeFactory _dynamicTypeFactory;
#pragma warning restore 649

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _configurationManager.RegisterCVar("game.lobbyenabled", false, CVar.ARCHIVE);
            _playerManager.PlayerStatusChanged += _handlePlayerStatusChanged;

            _netManager.RegisterNetMessage<MsgTickerJoinLobby>(nameof(MsgTickerJoinLobby));
            _netManager.RegisterNetMessage<MsgTickerJoinGame>(nameof(MsgTickerJoinGame));
            _netManager.RegisterNetMessage<MsgTickerLobbyStatus>(nameof(MsgTickerLobbyStatus));

            RestartRound();

            _initialized = true;
        }

        public void Update(FrameEventArgs frameEventArgs)
        {
            if (RunLevel != GameRunLevel.PreRoundLobby || _roundStartTimeUtc > DateTime.UtcNow ||
                _roundStartCountdownHasNotStartedYetDueToNoPlayers)
            {
                return;
            }

            StartRound();
        }

        public void RestartRound()
        {
            Logger.InfoS("ticker", "Restarting round!");

            RunLevel = GameRunLevel.PreRoundLobby;
            _resettingCleanup();
            _preRoundSetup();

            if (!LobbyEnabled)
            {
                StartRound();
            }
            else
            {
                if (_playerManager.PlayerCount == 0)
                {
                    _roundStartCountdownHasNotStartedYetDueToNoPlayers = true;
                }
                else
                {
                    _roundStartTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(LobbyDuration);
                }

                _sendStatusToAll();
            }
        }

        public void StartRound()
        {
            DebugTools.Assert(RunLevel == GameRunLevel.PreRoundLobby);
            Logger.InfoS("ticker", "Starting round!");

            RunLevel = GameRunLevel.InRound;

            // TODO: Allow other presets to be selected.
            var preset = _dynamicTypeFactory.CreateInstance<PresetTraitor>();
            preset.Start();

            foreach (var (playerSession, ready) in _playersInLobby.ToList())
            {
                if (LobbyEnabled && !ready)
                {
                    continue;
                }

                _spawnPlayer(playerSession);
            }

            _sendStatusToAll();
        }

        public void EndRound()
        {
            DebugTools.Assert(RunLevel == GameRunLevel.InRound);
            Logger.InfoS("ticker", "Ending round!");

            RunLevel = GameRunLevel.PostRound;
        }

        public void Respawn(IPlayerSession targetPlayer)
        {
            targetPlayer.ContentData().WipeMind();

            if (LobbyEnabled)
            {
                _playerJoinLobby(targetPlayer);
            }
            else
            {
                _spawnPlayer(targetPlayer);
            }
        }

        public void MakeObserve(IPlayerSession player)
        {
            if (!_playersInLobby.ContainsKey(player))
            {
                return;
            }

            _spawnObserver(player);
        }

        public void MakeJoinGame(IPlayerSession player)
        {
            if (!_playersInLobby.ContainsKey(player))
            {
                return;
            }

            _spawnPlayer(player);
        }

        public void ToggleReady(IPlayerSession player, bool ready)
        {
            if (!_playersInLobby.ContainsKey(player))
            {
                return;
            }

            _playersInLobby[player] = ready;
            _netManager.ServerSendMessage(_getStatusMsg(player), player.ConnectedClient);
        }

        public T AddGameRule<T>() where T : GameRule, new()
        {
            var instance = _dynamicTypeFactory.CreateInstance<T>();

            _gameRules.Add(instance);
            instance.Added();

            return instance;
        }

        public void RemoveGameRule(GameRule rule)
        {
            if (_gameRules.Contains(rule))
            {
                return;
            }

            rule.Removed();

            _gameRules.Remove(rule);
        }

        public IEnumerable<GameRule> ActiveGameRules => _gameRules;

        private IEntity _spawnPlayerMob()
        {
            var entity = _entityManager.ForceSpawnEntityAt(PlayerPrototypeName, _getLateJoinSpawnPoint());
            var shoes = _entityManager.SpawnEntity("ShoesItem");
            var uniform = _entityManager.SpawnEntity("UniformAssistant");
            if (entity.TryGetComponent(out InventoryComponent inventory))
            {
                inventory.Equip(EquipmentSlotDefines.Slots.INNERCLOTHING, uniform.GetComponent<ClothingComponent>());
                inventory.Equip(EquipmentSlotDefines.Slots.SHOES, shoes.GetComponent<ClothingComponent>());
            }

            return entity;
        }

        private IEntity _spawnObserverMob()
        {
            return _entityManager.ForceSpawnEntityAt(ObserverPrototypeName, _getLateJoinSpawnPoint());
        }

        private GridCoordinates _getLateJoinSpawnPoint()
        {
            var location = _spawnPoint;

            var possiblePoints = new List<GridCoordinates>();
            foreach (var entity in _entityManager.GetEntities(new TypeEntityQuery(typeof(SpawnPointComponent))))
            {
                var point = entity.GetComponent<SpawnPointComponent>();
                if (point.SpawnType == SpawnPointType.LateJoin)
                {
                    possiblePoints.Add(entity.Transform.GridPosition);
                }
            }

            if (possiblePoints.Count != 0)
            {
                location = _spawnRandom.Pick(possiblePoints);
            }

            return location;
        }

        /// <summary>
        ///     Cleanup that has to run to clear up anything from the previous round.
        ///     Stuff like wiping the previous map clean.
        /// </summary>
        private void _resettingCleanup()
        {
            // Delete all entities.
            foreach (var entity in _entityManager.GetEntities().ToList())
            {
                // TODO: Maybe something less naive here?
                // FIXME: Actually, definitely.
                entity.Delete();
            }

            // Delete all maps outside of nullspace.
            foreach (var map in _mapManager.GetAllMaps().ToList())
            {
                // TODO: Maybe something less naive here?
                if (map.Index != MapId.Nullspace)
                {
                    _mapManager.DeleteMap(map.Index);
                }
            }

            // Delete the minds of everybody.
            // TODO: Maybe move this into a separate manager?
            foreach (var unCastData in _playerManager.GetAllPlayerData())
            {
                unCastData.ContentData().WipeMind();
            }

            // Clear up any game rules.
            foreach (var rule in _gameRules)
            {
                rule.Removed();
            }

            _gameRules.Clear();

            // Move everybody currently in the server to lobby.
            foreach (var player in _playerManager.GetAllPlayers())
            {
                if (_playersInLobby.ContainsKey(player))
                {
                    continue;
                }

                _playerJoinLobby(player);
            }
        }

        private void _preRoundSetup()
        {
            var newMap = _mapManager.CreateMap();
            var startTime = _gameTiming.RealTime;
            var grid = _mapLoader.LoadBlueprint(newMap, MapFile);

            _spawnPoint = new GridCoordinates(Vector2.Zero, grid);

            var timeSpan = _gameTiming.RealTime - startTime;
            Logger.InfoS("ticker", $"Loaded map in {timeSpan.TotalMilliseconds:N2}ms.");
        }

        private void _handlePlayerStatusChanged(object sender, SessionStatusEventArgs args)
        {
            var session = args.Session;

            switch (args.NewStatus)
            {
                case SessionStatus.Connected:
                {
                    // Always make sure the client has player data. Mind gets assigned on spawn.
                    if (session.Data.ContentDataUncast == null)
                    {
                        session.Data.ContentDataUncast = new PlayerData(session.SessionId);
                    }

                    // timer time must be > tick length
                    Timer.Spawn(0, args.Session.JoinGame);

                    _chatManager.DispatchServerAnnouncement($"Player {args.Session.SessionId} joined server!");

                    if (LobbyEnabled && _roundStartCountdownHasNotStartedYetDueToNoPlayers)
                    {
                        _roundStartCountdownHasNotStartedYetDueToNoPlayers = false;
                        _roundStartTimeUtc = DateTime.UtcNow + TimeSpan.FromSeconds(LobbyDuration);
                    }

                    break;
                }

                case SessionStatus.InGame:
                {
                    var data = session.ContentData();
                    if (data.Mind == null)
                    {
                        if (LobbyEnabled)
                        {
                            _playerJoinLobby(session);
                            return;
                        }

                        _spawnPlayer(session);
                    }
                    else
                    {
                        if (data.Mind.CurrentEntity == null)
                        {
                            _spawnPlayer(session);
                        }
                        else
                        {
                            session.AttachToEntity(data.Mind.CurrentEntity);
                            _playerJoinGame(session);
                        }
                    }

                    _chatManager.DispatchServerAnnouncement($"Player {args.Session.SessionId} joined game!");
                    break;
                }

                case SessionStatus.Disconnected:
                {
                    if (_playersInLobby.ContainsKey(session))
                    {
                        _playersInLobby.Remove(session);
                    }

                    _chatManager.DispatchServerAnnouncement($"Player {args.Session.SessionId} left server!");
                    break;
                }
            }
        }

        private void _spawnPlayer(IPlayerSession session)
        {
            _playerJoinGame(session);
            var data = session.ContentData();
            data.WipeMind();
            data.Mind = new Mind(session.SessionId);

            var mob = _spawnPlayerMob();
            data.Mind.TransferTo(mob);
        }

        private void _spawnObserver(IPlayerSession session)
        {
            _playerJoinGame(session);
            var data = session.ContentData();
            data.WipeMind();
            data.Mind = new Mind(session.SessionId);

            var mob = _spawnObserverMob();
            data.Mind.TransferTo(mob);
        }

        private void _playerJoinLobby(IPlayerSession session)
        {
            _playersInLobby.Add(session, false);

            _netManager.ServerSendMessage(_netManager.CreateNetMessage<MsgTickerJoinLobby>(), session.ConnectedClient);
            _netManager.ServerSendMessage(_getStatusMsg(session), session.ConnectedClient);
        }

        private void _playerJoinGame(IPlayerSession session)
        {
            if (_playersInLobby.ContainsKey(session))
            {
                _playersInLobby.Remove(session);
            }

            _netManager.ServerSendMessage(_netManager.CreateNetMessage<MsgTickerJoinGame>(), session.ConnectedClient);
        }

        private MsgTickerLobbyStatus _getStatusMsg(IPlayerSession session)
        {
            _playersInLobby.TryGetValue(session, out var ready);
            var msg = _netManager.CreateNetMessage<MsgTickerLobbyStatus>();
            msg.IsRoundStarted = RunLevel != GameRunLevel.PreRoundLobby;
            msg.StartTime = _roundStartTimeUtc;
            msg.YouAreReady = ready;
            return msg;
        }

        private void _sendStatusToAll()
        {
            foreach (var player in _playersInLobby.Keys)
            {
                _netManager.ServerSendMessage(_getStatusMsg(player), player.ConnectedClient);
            }
        }
    }

    public enum GameRunLevel
    {
        PreRoundLobby = 0,
        InRound = 1,
        PostRound = 2
    }

    public class GameRunLevelChangedEventArgs : EventArgs
    {
        public GameRunLevel OldRunLevel { get; }
        public GameRunLevel NewRunLevel { get; }

        public GameRunLevelChangedEventArgs(GameRunLevel oldRunLevel, GameRunLevel newRunLevel)
        {
            OldRunLevel = oldRunLevel;
            NewRunLevel = newRunLevel;
        }
    }

    class StartRoundCommand : IClientCommand
    {
        public string Command => "startround";
        public string Description => "Ends PreRoundLobby state and starts the round.";
        public string Help => String.Empty;

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var ticker = IoCManager.Resolve<IGameTicker>();

            if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
            {
                shell.SendText(player, "This can only be executed while the game is in the pre-round lobby.");
                return;
            }

            ticker.StartRound();
        }
    }

    class EndRoundCommand : IClientCommand
    {
        public string Command => "endround";
        public string Description => "Ends the round and moves the server to PostRound.";
        public string Help => String.Empty;

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var ticker = IoCManager.Resolve<IGameTicker>();

            if (ticker.RunLevel != GameRunLevel.InRound)
            {
                shell.SendText(player, "This can only be executed while the game is in a round.");
                return;
            }

            ticker.EndRound();
        }
    }

    class NewRoundCommand : IClientCommand
    {
        public string Command => "restartround";
        public string Description => "Moves the server from PostRound to a new PreRoundLobby.";
        public string Help => String.Empty;

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var ticker = IoCManager.Resolve<IGameTicker>();
            ticker.RestartRound();
        }
    }

    class RespawnCommand : IClientCommand
    {
        public string Command => "respawn";
        public string Description => "Respawns a player, kicking them back to the lobby.";
        public string Help => "respawn <player>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Must provide exactly one argument.");
                return;
            }

            var playerMgr = IoCManager.Resolve<IPlayerManager>();
            var ticker = IoCManager.Resolve<IGameTicker>();

            var arg = new NetSessionId(args[0]);
            if (!playerMgr.TryGetSessionById(arg, out var targetPlayer))
            {
                if (!playerMgr.TryGetPlayerData(arg, out var data))
                {
                    shell.SendText(player, "Unknown player");
                    return;
                }

                data.ContentData().WipeMind();
                shell.SendText(player,
                    "Player is not currently online, but they will respawn if they come back online");
                return;
            }

            ticker.Respawn(targetPlayer);
        }
    }

    class ObserveCommand : IClientCommand
    {
        public string Command => "observe";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                return;
            }

            var ticker = IoCManager.Resolve<IGameTicker>();
            ticker.MakeObserve(player);
        }
    }

    class JoinGameCommand : IClientCommand
    {
        public string Command => "joingame";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                return;
            }

            var ticker = IoCManager.Resolve<IGameTicker>();
            ticker.MakeJoinGame(player);
        }
    }

    class ToggleReadyCommand : IClientCommand
    {
        public string Command => "toggleready";
        public string Description => "";
        public string Help => "";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (player == null)
            {
                return;
            }

            var ticker = IoCManager.Resolve<IGameTicker>();
            ticker.ToggleReady(player, bool.Parse(args[0]));
        }
    }
}
