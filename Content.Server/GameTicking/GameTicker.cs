using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects;
using Content.Server.GameObjects.Components.Markers;
using Content.Server.GameTicking.GamePresets;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs;
using Content.Server.Mobs.Roles;
using Content.Server.Players;
using Content.Shared;
using Content.Shared.GameObjects.Components.Inventory;
using Content.Shared.Jobs;
using Robust.Server.Interfaces.Maps;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
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

        [ViewVariables] private readonly List<GameRule> _gameRules = new List<GameRule>();

        [ViewVariables] private Type _presetType;

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
        [Dependency] private IPrototypeManager _prototypeManager;
        [Dependency] private readonly ILocalizationManager _localization;
        [Dependency] private readonly IRobustRandom _robustRandom;
#pragma warning restore 649

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _configurationManager.RegisterCVar("game.lobbyenabled", false, CVar.ARCHIVE);
            _playerManager.PlayerStatusChanged += _handlePlayerStatusChanged;

            _netManager.RegisterNetMessage<MsgTickerJoinLobby>(nameof(MsgTickerJoinLobby));
            _netManager.RegisterNetMessage<MsgTickerJoinGame>(nameof(MsgTickerJoinGame));
            _netManager.RegisterNetMessage<MsgTickerLobbyStatus>(nameof(MsgTickerLobbyStatus));
            _netManager.RegisterNetMessage<MsgTickerLobbyInfo>(nameof(MsgTickerLobbyInfo));

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

            var preset = MakeGamePreset();
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

        public void SetStartPreset(Type type)
        {
            if (!typeof(GamePreset).IsAssignableFrom(type))
            {
                throw new ArgumentException("type must inherit GamePreset");
            }
            _presetType = type;
            UpdateInfoText();
        }

        private IEntity _spawnPlayerMob(Job job)
        {
            var entity = _entityManager.SpawnEntityAt(PlayerPrototypeName, _getLateJoinSpawnPoint());
            if (entity.TryGetComponent(out InventoryComponent inventory))
            {
                var gear = _prototypeManager.Index<StartingGearPrototype>(job.StartingGear).Equipment;

                foreach (var (slotStr, equipmentStr) in gear)
                {
                    if (!Enum.TryParse(slotStr.ToUpper(), out EquipmentSlotDefines.Slots slot))
                    {
                        Logger.Error("{0} is an invalid equipment slot.", slotStr);
                        continue;
                    }
                    var equipmentEntity = _entityManager.SpawnEntity(equipmentStr, entity.Transform.GridPosition);
                    inventory.Equip(slot, equipmentEntity.GetComponent<ClothingComponent>());
                }
            }

            return entity;
        }

        private IEntity _spawnObserverMob()
        {
            return _entityManager.SpawnEntityAt(ObserverPrototypeName, _getLateJoinSpawnPoint());
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
                location = _robustRandom.Pick(possiblePoints);
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
            foreach (var mapId in _mapManager.GetAllMapIds().ToList())
            {
                // TODO: Maybe something less naive here?
                if (mapId != MapId.Nullspace)
                {
                    _mapManager.DeleteMap(mapId);
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
            var newMapId = _mapManager.CreateMap();
            var startTime = _gameTiming.RealTime;
            var grid = _mapLoader.LoadBlueprint(newMapId, MapFile);

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
            //TODO Replace "Assistant" with the job when char preference are done
            var job = new Job(data.Mind, _prototypeManager.Index<JobPrototype>("Assistant"));
            data.Mind.AddRole(job);

            var mob = _spawnPlayerMob(job);
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
            _netManager.ServerSendMessage(GetInfoMsg(), session.ConnectedClient);
        }

        private void _playerJoinGame(IPlayerSession session)
        {
            _chatManager.DispatchServerMessage(session, $"Welcome to Space Station 14! If this is your first time checking out the game, be sure to check out the tutorial in the top left!");
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

        private MsgTickerLobbyInfo GetInfoMsg()
        {
            var msg = _netManager.CreateNetMessage<MsgTickerLobbyInfo>();
            msg.TextBlob = GetInfoText();
            return msg;
        }

        private void _sendStatusToAll()
        {
            foreach (var player in _playersInLobby.Keys)
            {
                _netManager.ServerSendMessage(_getStatusMsg(player), player.ConnectedClient);
            }
        }

        private string GetInfoText()
        {
            var gameMode = MakeGamePreset().Description;
            return _localization.GetString(@"Hi and welcome to [color=white]Space Station 14![/color]

The current game mode is [color=white]{0}[/color]", gameMode);
        }

        private void UpdateInfoText()
        {
            var infoMsg = GetInfoMsg();

            _netManager.ServerSendToMany(infoMsg, _playersInLobby.Keys.Select(p => p.ConnectedClient).ToList());
        }

        private GamePreset MakeGamePreset()
        {
            return _dynamicTypeFactory.CreateInstance<GamePreset>(_presetType ?? typeof(PresetSandbox));
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
}
