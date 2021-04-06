using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Markers;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Mobs.Speech;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.GameObjects.Components.PDA;
using Content.Server.GameTicking.GamePresets;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs;
using Content.Server.Mobs.Roles;
using Content.Server.Players;
using Content.Shared;
using Content.Shared.Audio;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Network.NetMessages;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Utility;
using Prometheus;
using Robust.Server;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking
{
    public partial class GameTicker : GameTickerBase, IGameTicker
    {
        private static readonly Counter RoundNumberMetric = Metrics.CreateCounter(
            "ss14_round_number",
            "Round number.");

        private static readonly Gauge RoundLengthMetric = Metrics.CreateGauge(
            "ss14_round_length",
            "Round length in seconds.");

        private static readonly TimeSpan UpdateRestartDelay = TimeSpan.FromSeconds(20);

        public const float PresetFailedCooldownIncrease = 30f;
        private const string PlayerPrototypeName = "HumanMob_Content";
        private const string ObserverPrototypeName = "MobObserver";
        private TimeSpan _roundStartTimeSpan;

        [ViewVariables] private readonly List<GameRule> _gameRules = new();
        [ViewVariables] private readonly List<ManifestEntry> _manifest = new();

        [ViewVariables]
        private readonly Dictionary<IPlayerSession, PlayerStatus> _playersInLobby = new();

        [ViewVariables] private bool _initialized;

        [ViewVariables] private Type? _presetType;

        [ViewVariables] private TimeSpan _pauseTime;
        [ViewVariables] private bool _roundStartCountdownHasNotStartedYetDueToNoPlayers;
        [ViewVariables] private TimeSpan _roundStartTime;
        [ViewVariables] private GameRunLevel _runLevel;
        [ViewVariables(VVAccess.ReadWrite)] private EntityCoordinates _spawnPoint;

        [ViewVariables] private bool DisallowLateJoin { get; set; } = false;

        [ViewVariables] private bool LobbyEnabled => _configurationManager.GetCVar(CCVars.GameLobbyEnabled);

        [ViewVariables] private bool _updateOnRoundEnd;
        private CancellationTokenSource? _updateShutdownCts;


        [ViewVariables] public bool Paused { get; private set; }

        [ViewVariables] public MapId DefaultMap { get; private set; }

        [ViewVariables] public GridId DefaultGridId { get; private set; }

        [ViewVariables]
        public GameRunLevel RunLevel
        {
            get => _runLevel;
            private set
            {
                if (_runLevel == value) return;

                var old = _runLevel;
                _runLevel = value;

                OnRunLevelChanged?.Invoke(new GameRunLevelChangedEventArgs(old, value));
            }
        }

        [ViewVariables]
        public GamePreset? Preset
        {
            get => _preset ?? MakeGamePreset(new Dictionary<NetUserId, HumanoidCharacterProfile>());
            set => _preset = value;
        }

        public ImmutableDictionary<string, Type> Presets { get; private set; } = default!;

        private GamePreset? _preset;

        public event Action<GameRunLevelChangedEventArgs>? OnRunLevelChanged;
        public event Action<GameRuleAddedEventArgs>? OnRuleAdded;

        private TimeSpan LobbyDuration =>
            TimeSpan.FromSeconds(_configurationManager.GetCVar(CCVars.GameLobbyDuration));

        private SoundCollectionPrototype _lobbyCollection = default!;
        [ViewVariables] public string LobbySong { get; private set; } = default!;

        public override void Initialize()
        {
            base.Initialize();

            DebugTools.Assert(!_initialized);

            var presets = new Dictionary<string, Type>();

            foreach (var type in _reflectionManager.FindTypesWithAttribute<GamePresetAttribute>())
            {
                var attribute = type.GetCustomAttribute<GamePresetAttribute>();

                presets.Add(attribute!.Id.ToLowerInvariant(), type);

                foreach (var alias in attribute.Aliases)
                {
                    presets.Add(alias.ToLowerInvariant(), type);
                }
            }

            Presets = presets.ToImmutableDictionary();

            _lobbyCollection = _prototypeManager.Index<SoundCollectionPrototype>("LobbyMusic");
            LobbySong = _robustRandom.Pick(_lobbyCollection.PickFiles);

            _netManager.RegisterNetMessage<MsgTickerJoinLobby>(nameof(MsgTickerJoinLobby));
            _netManager.RegisterNetMessage<MsgTickerJoinGame>(nameof(MsgTickerJoinGame));
            _netManager.RegisterNetMessage<MsgTickerLobbyStatus>(nameof(MsgTickerLobbyStatus));
            _netManager.RegisterNetMessage<MsgTickerLobbyInfo>(nameof(MsgTickerLobbyInfo));
            _netManager.RegisterNetMessage<MsgTickerLobbyCountdown>(nameof(MsgTickerLobbyCountdown));
            _netManager.RegisterNetMessage<MsgTickerLobbyReady>(nameof(MsgTickerLobbyReady));
            _netManager.RegisterNetMessage<MsgRoundEndMessage>(nameof(MsgRoundEndMessage));
            _netManager.RegisterNetMessage<MsgRequestWindowAttention>(nameof(MsgRequestWindowAttention));
            _netManager.RegisterNetMessage<MsgTickerLateJoinStatus>(nameof(MsgTickerLateJoinStatus));
            _netManager.RegisterNetMessage<MsgTickerJobsAvailable>(nameof(MsgTickerJobsAvailable));

            SetStartPreset(_configurationManager.GetCVar(CCVars.GameLobbyDefaultPreset));

            RestartRound();

            _initialized = true;

            JobControllerInit();

            _watchdogApi.UpdateReceived += WatchdogApiOnUpdateReceived;
        }

        private void WatchdogApiOnUpdateReceived()
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString(
                "Update has been received, server will automatically restart for update at the end of this round."));
            _updateOnRoundEnd = true;
            ServerEmptyUpdateRestartCheck();
        }

        public void Update(FrameEventArgs frameEventArgs)
        {
            if (RunLevel == GameRunLevel.InRound)
            {
                RoundLengthMetric.Inc(frameEventArgs.DeltaSeconds);
            }

            if (RunLevel != GameRunLevel.PreRoundLobby ||
                Paused ||
                _roundStartTime > _gameTiming.CurTime ||
                _roundStartCountdownHasNotStartedYetDueToNoPlayers)
            {
                return;
            }

            StartRound();
        }

        public void RestartRound()
        {
            if (_updateOnRoundEnd)
            {
                _baseServer.Shutdown(
                    Loc.GetString("Server is shutting down for update and will automatically restart."));
                return;
            }

            Logger.InfoS("ticker", "Restarting round!");

            SendServerMessage("Restarting round...");

            RoundNumberMetric.Inc();

            RunLevel = GameRunLevel.PreRoundLobby;
            LobbySong = _robustRandom.Pick(_lobbyCollection.PickFiles);
            _resettingCleanup();
            _preRoundSetup();

            if (!LobbyEnabled)
            {
                StartRound();
            }
            else
            {
                Preset = null;

                if (PlayerManager.PlayerCount == 0)
                    _roundStartCountdownHasNotStartedYetDueToNoPlayers = true;
                else
                    _roundStartTime = _gameTiming.CurTime + LobbyDuration;

                _sendStatusToAll();

                ReqWindowAttentionAll();
            }
        }

        private void ReqWindowAttentionAll()
        {
            foreach (var player in PlayerManager.GetAllPlayers())
            {
                player.RequestWindowAttention();
            }
        }

        public void StartRound(bool force = false)
        {
            DebugTools.Assert(RunLevel == GameRunLevel.PreRoundLobby);
            Logger.InfoS("ticker", "Starting round!");

            SendServerMessage("The round is starting now...");

            List<IPlayerSession> readyPlayers;
            if (LobbyEnabled)
            {
                readyPlayers = _playersInLobby.Where(p => p.Value == PlayerStatus.Ready).Select(p => p.Key).ToList();
            }
            else
            {
                readyPlayers = _playersInLobby.Keys.ToList();
            }

            RunLevel = GameRunLevel.InRound;

            RoundLengthMetric.Set(0);

            // Get the profiles for each player for easier lookup.
            var profiles = _prefsManager.GetSelectedProfilesForPlayers(
                readyPlayers
                    .Select(p => p.UserId).ToList())
                    .ToDictionary(p => p.Key, p => (HumanoidCharacterProfile) p.Value);

            foreach (var readyPlayer in readyPlayers)
            {
                if (!profiles.ContainsKey(readyPlayer.UserId))
                {
                    profiles.Add(readyPlayer.UserId, HumanoidCharacterProfile.Random());
                }
            }

            var assignedJobs = AssignJobs(readyPlayers, profiles);

            // For players without jobs, give them the overflow job if they have that set...
            foreach (var player in readyPlayers)
            {
                if (assignedJobs.ContainsKey(player))
                {
                    continue;
                }

                var profile = profiles[player.UserId];
                if (profile.PreferenceUnavailable == PreferenceUnavailableMode.SpawnAsOverflow)
                {
                    assignedJobs.Add(player, OverflowJob);
                }
            }

            // Spawn everybody in!
            foreach (var (player, job) in assignedJobs)
            {
                SpawnPlayer(player, profiles[player.UserId], job, false);
            }

            // Time to start the preset.
            Preset = MakeGamePreset(profiles);

            DisallowLateJoin |= Preset.DisallowLateJoin;

            if (!Preset.Start(assignedJobs.Keys.ToList(), force))
            {
                if (_configurationManager.GetCVar(CCVars.GameLobbyFallbackEnabled))
                {
                    SetStartPreset(_configurationManager.GetCVar(CCVars.GameLobbyFallbackPreset));
                    var newPreset = MakeGamePreset(profiles);
                    _chatManager.DispatchServerAnnouncement(
                        $"Failed to start {Preset.ModeTitle} mode! Defaulting to {newPreset.ModeTitle}...");
                    if (!newPreset.Start(readyPlayers, force))
                    {
                        throw new ApplicationException("Fallback preset failed to start!");
                    }

                    DisallowLateJoin = false;
                    DisallowLateJoin |= newPreset.DisallowLateJoin;
                    Preset = newPreset;
                }
                else
                {
                    SendServerMessage($"Failed to start {Preset.ModeTitle} mode! Restarting round...");
                    RestartRound();
                    DelayStart(TimeSpan.FromSeconds(PresetFailedCooldownIncrease));
                    return;
                }
            }
            Preset.OnGameStarted();

            _roundStartTimeSpan = IoCManager.Resolve<IGameTiming>().RealTime;
            _sendStatusToAll();
            ReqWindowAttentionAll();
            UpdateLateJoinStatus();
            UpdateJobsAvailable();
        }

        private void UpdateLateJoinStatus()
        {
            var msg = new MsgTickerLateJoinStatus(null!) {Disallowed = DisallowLateJoin};
            _netManager.ServerSendToAll(msg);
        }

        private void SendServerMessage(string message)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Server;
            msg.Message = message;
            IoCManager.Resolve<IServerNetManager>().ServerSendToAll(msg);
        }

        private HumanoidCharacterProfile GetPlayerProfile(IPlayerSession p)
        {
            return (HumanoidCharacterProfile) _prefsManager.GetPreferences(p.UserId).SelectedCharacter;
        }

        public void EndRound(string roundEndText = "")
        {
            DebugTools.Assert(RunLevel == GameRunLevel.InRound);
            Logger.InfoS("ticker", "Ending round!");

            RunLevel = GameRunLevel.PostRound;

            //Tell every client the round has ended.
            var roundEndMessage = _netManager.CreateNetMessage<MsgRoundEndMessage>();
            roundEndMessage.GamemodeTitle = Preset?.ModeTitle ?? string.Empty;
            roundEndMessage.RoundEndText = roundEndText + $"\n{Preset?.GetRoundEndDescription() ?? string.Empty}";

            //Get the timespan of the round.
            roundEndMessage.RoundDuration = IoCManager.Resolve<IGameTiming>().RealTime.Subtract(_roundStartTimeSpan);

            //Generate a list of basic player info to display in the end round summary.
            var listOfPlayerInfo = new List<RoundEndPlayerInfo>();
            foreach (var ply in PlayerManager.GetAllPlayers().OrderBy(p => p.Name))
            {
                var mind = ply.ContentData()?.Mind;

                if (mind != null)
                {
                    _playersInLobby.TryGetValue(ply, out var status);
                    var antag = mind.AllRoles.Any(role => role.Antagonist);
                    var playerEndRoundInfo = new RoundEndPlayerInfo()
                    {
                        PlayerOOCName = ply.Name,
                        PlayerICName = mind.CurrentEntity?.Name,
                        Role = antag
                            ? mind.AllRoles.First(role => role.Antagonist).Name
                            : mind.AllRoles.FirstOrDefault()?.Name ?? Loc.GetString("Unknown"),
                        Antag = antag,
                        Observer = status == PlayerStatus.Observer,
                    };
                    listOfPlayerInfo.Add(playerEndRoundInfo);
                }
            }

            roundEndMessage.AllPlayersEndInfo = listOfPlayerInfo;
            _netManager.ServerSendToAll(roundEndMessage);
        }

        public void Respawn(IPlayerSession targetPlayer)
        {
            targetPlayer.ContentData()?.WipeMind();

            if (LobbyEnabled)
                _playerJoinLobby(targetPlayer);
            else
                SpawnPlayer(targetPlayer);
        }

        public void MakeObserve(IPlayerSession player)
        {
            if (!_playersInLobby.ContainsKey(player)) return;

            _spawnObserver(player);
            _playersInLobby[player] = PlayerStatus.Observer;
            _netManager.ServerSendToAll(GetStatusSingle(player, PlayerStatus.Observer));
        }

        public void MakeJoinGame(IPlayerSession player, string? jobId = null)
        {
            if (!_playersInLobby.ContainsKey(player)) return;

            if (!_prefsManager.HavePreferencesLoaded(player))
            {
                return;
            }

            SpawnPlayer(player, jobId);
        }

        public void ToggleReady(IPlayerSession player, bool ready)
        {
            if (!_playersInLobby.ContainsKey(player)) return;

            if (!_prefsManager.HavePreferencesLoaded(player))
            {
                return;
            }

            var status = ready ? PlayerStatus.Ready : PlayerStatus.NotReady;
            _playersInLobby[player] = ready ? PlayerStatus.Ready : PlayerStatus.NotReady;
            _netManager.ServerSendMessage(_getStatusMsg(player), player.ConnectedClient);
            _netManager.ServerSendToAll(GetStatusSingle(player, status));
        }

        public void ToggleDisallowLateJoin(bool disallowLateJoin)
        {
            DisallowLateJoin = disallowLateJoin;
            UpdateLateJoinStatus();
            UpdateJobsAvailable();
        }

        public bool OnGhostAttempt(Mind mind, bool canReturnGlobal)
        {
            return Preset?.OnGhostAttempt(mind, canReturnGlobal) ?? false;
        }

        public T AddGameRule<T>() where T : GameRule, new()
        {
            var instance = _dynamicTypeFactory.CreateInstance<T>();

            _gameRules.Add(instance);
            instance.Added();

            OnRuleAdded?.Invoke(new GameRuleAddedEventArgs(instance));

            return instance;
        }

        public bool HasGameRule(string? name)
        {
            if (name == null)
                return false;

            foreach (var rule in _gameRules)
            {
                if (rule.GetType().Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasGameRule(Type? type)
        {
            if (type == null || !typeof(GameRule).IsAssignableFrom(type))
                return false;

            foreach (var rule in _gameRules)
            {
                if (rule.GetType().IsAssignableFrom(type))
                    return true;
            }

            return false;
        }

        public void RemoveGameRule(GameRule rule)
        {
            if (_gameRules.Contains(rule)) return;

            rule.Removed();

            _gameRules.Remove(rule);
        }

        public IEnumerable<GameRule> ActiveGameRules => _gameRules;

        public bool TryGetPreset(string name, [NotNullWhen(true)] out Type? type)
        {
            name = name.ToLowerInvariant();
            return Presets.TryGetValue(name, out type);
        }

        public void SetStartPreset(Type type, bool force = false)
        {
            if (!typeof(GamePreset).IsAssignableFrom(type)) throw new ArgumentException("type must inherit GamePreset");

            _presetType = type;
            UpdateInfoText();

            if (force)
            {
                StartRound(true);
            }
        }

        public void SetStartPreset(string name, bool force = false)
        {
            if (!TryGetPreset(name, out var type))
            {
                throw new NotSupportedException($"No preset found with name {name}");
            }

            SetStartPreset(type, force);
        }

        public bool DelayStart(TimeSpan time)
        {
            if (_runLevel != GameRunLevel.PreRoundLobby)
            {
                return false;
            }

            _roundStartTime += time;

            var lobbyCountdownMessage = _netManager.CreateNetMessage<MsgTickerLobbyCountdown>();
            lobbyCountdownMessage.StartTime = _roundStartTime;
            lobbyCountdownMessage.Paused = Paused;
            _netManager.ServerSendToAll(lobbyCountdownMessage);

            _chatManager.DispatchServerAnnouncement($"Round start has been delayed for {time.TotalSeconds} seconds.");

            return true;
        }

        public bool PauseStart(bool pause = true)
        {
            if (Paused == pause)
            {
                return false;
            }

            Paused = pause;

            if (pause)
            {
                _pauseTime = _gameTiming.CurTime;
            }
            else if (_pauseTime != default)
            {
                _roundStartTime += _gameTiming.CurTime - _pauseTime;
            }

            var lobbyCountdownMessage = _netManager.CreateNetMessage<MsgTickerLobbyCountdown>();
            lobbyCountdownMessage.StartTime = _roundStartTime;
            lobbyCountdownMessage.Paused = Paused;
            _netManager.ServerSendToAll(lobbyCountdownMessage);

            _chatManager.DispatchServerAnnouncement(Paused
                ? "Round start has been paused."
                : "Round start countdown is now resumed.");

            return true;
        }

        public bool TogglePause()
        {
            PauseStart(!Paused);
            return Paused;
        }

        private IEntity _spawnPlayerMob(Job job, HumanoidCharacterProfile? profile, bool lateJoin = true)
        {
            var coordinates = lateJoin ? GetLateJoinSpawnPoint() : GetJobSpawnPoint(job.Prototype.ID);
            var entity = _entityManager.SpawnEntity(PlayerPrototypeName, coordinates);

            if (job.StartingGear != null)
            {
                var startingGear = _prototypeManager.Index<StartingGearPrototype>(job.StartingGear);
                EquipStartingGear(entity, startingGear, profile);
            }

            if (profile != null)
            {
                entity.GetComponent<HumanoidAppearanceComponent>().UpdateFromProfile(profile);
                entity.Name = profile.Name;
            }

            return entity;
        }

        public void EquipStartingGear(IEntity entity, StartingGearPrototype startingGear, HumanoidCharacterProfile? profile)
        {
            if (entity.TryGetComponent(out InventoryComponent? inventory))
            {
                foreach (var slot in AllSlots)
                {
                    var equipmentStr = startingGear.GetGear(slot, profile);
                    if (equipmentStr != "")
                    {
                        var equipmentEntity = _entityManager.SpawnEntity(equipmentStr, entity.Transform.Coordinates);
                        inventory.Equip(slot, equipmentEntity.GetComponent<ItemComponent>());
                    }
                }
            }

            if (entity.TryGetComponent(out HandsComponent? handsComponent))
            {
                var inhand = startingGear.Inhand;
                foreach (var (hand, prototype) in inhand)
                {
                    var inhandEntity = _entityManager.SpawnEntity(prototype, entity.Transform.Coordinates);
                    handsComponent.TryPickupEntity(hand, inhandEntity, checkActionBlocker:false);
                }
            }
        }

        private IEntity _spawnObserverMob()
        {
            var coordinates = GetObserverSpawnPoint();
            return _entityManager.SpawnEntity(ObserverPrototypeName, coordinates);
        }

        public EntityCoordinates GetLateJoinSpawnPoint()
        {
            var location = _spawnPoint;

            var possiblePoints = new List<EntityCoordinates>();
            foreach (var entity in _entityManager.GetEntities(new TypeEntityQuery(typeof(SpawnPointComponent))))
            {
                var point = entity.GetComponent<SpawnPointComponent>();
                if (point.SpawnType == SpawnPointType.LateJoin) possiblePoints.Add(entity.Transform.Coordinates);
            }

            if (possiblePoints.Count != 0) location = _robustRandom.Pick(possiblePoints);

            return location;
        }

        public EntityCoordinates GetJobSpawnPoint(string jobId)
        {
            var location = _spawnPoint;

            var possiblePoints = new List<EntityCoordinates>();
            foreach (var entity in _entityManager.GetEntities(new TypeEntityQuery(typeof(SpawnPointComponent))))
            {
                var point = entity.GetComponent<SpawnPointComponent>();
                if (point.SpawnType == SpawnPointType.Job && point.Job?.ID == jobId)
                    possiblePoints.Add(entity.Transform.Coordinates);
            }

            if (possiblePoints.Count != 0) location = _robustRandom.Pick(possiblePoints);

            return location;
        }

        public EntityCoordinates GetObserverSpawnPoint()
        {
            var location = _spawnPoint;

            var possiblePoints = new List<EntityCoordinates>();
            foreach (var entity in _entityManager.GetEntities(new TypeEntityQuery(typeof(SpawnPointComponent))))
            {
                var point = entity.GetComponent<SpawnPointComponent>();
                if (point.SpawnType == SpawnPointType.Observer)
                    possiblePoints.Add(entity.Transform.Coordinates);
            }

            if (possiblePoints.Count != 0) location = _robustRandom.Pick(possiblePoints);

            return location;
        }

        /// <summary>
        ///     Cleanup that has to run to clear up anything from the previous round.
        ///     Stuff like wiping the previous map clean.
        /// </summary>
        private void _resettingCleanup()
        {
            // Move everybody currently in the server to lobby.
            foreach (var player in PlayerManager.GetAllPlayers())
            {
                _playerJoinLobby(player);
            }

            // Delete the minds of everybody.
            // TODO: Maybe move this into a separate manager?
            foreach (var unCastData in PlayerManager.GetAllPlayerData())
            {
                unCastData.ContentData()?.WipeMind();
            }

            // Delete all entities.
            foreach (var entity in _entityManager.GetEntities().ToList())
            {
                // TODO: Maybe something less naive here?
                // FIXME: Actually, definitely.
                entity.Delete();
            }

            _mapManager.Restart();

            // Clear up any game rules.
            foreach (var rule in _gameRules)
            {
                rule.Removed();
            }

            _gameRules.Clear();

            foreach (var system in _entitySystemManager.AllSystems)
            {
                if (system is IResettingEntitySystem resetting)
                {
                    resetting.Reset();
                }
            }

            _spawnedPositions.Clear();
            _manifest.Clear();
            DisallowLateJoin = false;
        }

        private string GetMap()
        {
            return _configurationManager.GetCVar(CCVars.GameMap);
        }

        private void _preRoundSetup()
        {
            DefaultMap = _mapManager.CreateMap();
            var startTime = _gameTiming.RealTime;
            var map = GetMap();
            var grid = _mapLoader.LoadBlueprint(DefaultMap, map);

            if (grid == null)
            {
                throw new InvalidOperationException($"No grid found for map {map}");
            }

            DefaultGridId = grid.Index;
            _spawnPoint = grid.ToCoordinates();

            var timeSpan = _gameTiming.RealTime - startTime;
            Logger.InfoS("ticker", $"Loaded map in {timeSpan.TotalMilliseconds:N2}ms.");
        }

        protected override void PlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            base.PlayerStatusChanged(sender, args);

            var session = args.Session;

            switch (args.NewStatus)
            {
                case SessionStatus.Connecting:
                    // Cancel shutdown update timer in progress.
                    _updateShutdownCts?.Cancel();
                    break;

                case SessionStatus.Connected:
                {
                    _chatManager.SendAdminAnnouncement(Loc.GetString("player-join-message", ("name", args.Session.Name)));

                    if (LobbyEnabled && _roundStartCountdownHasNotStartedYetDueToNoPlayers)
                    {
                        _roundStartCountdownHasNotStartedYetDueToNoPlayers = false;
                        _roundStartTime = _gameTiming.CurTime + LobbyDuration;
                    }

                    break;
                }

                case SessionStatus.InGame:
                {
                    _prefsManager.OnClientConnected(session);

                    var data = session.ContentData();

                    DebugTools.AssertNotNull(data);

                    if (data!.Mind == null)
                    {
                        if (LobbyEnabled)
                        {
                            _playerJoinLobby(session);
                            return;
                        }


                        SpawnWaitPrefs();
                    }
                    else
                    {
                        if (data.Mind.CurrentEntity == null)
                        {
                            SpawnWaitPrefs();
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
                    if (_playersInLobby.ContainsKey(session)) _playersInLobby.Remove(session);

                    _chatManager.SendAdminAnnouncement(Loc.GetString("player-leave-message", ("name", args.Session.Name)));

                    ServerEmptyUpdateRestartCheck();
                    _prefsManager.OnClientDisconnected(session);
                    break;
                }
            }

            async void SpawnWaitPrefs()
            {
                await _prefsManager.WaitPreferencesLoaded(session);
                SpawnPlayer(session);
            }
        }

        /// <summary>
        ///     Checks whether there are still players on the server,
        /// and if not starts a timer to automatically reboot the server if an update is available.
        /// </summary>
        private void ServerEmptyUpdateRestartCheck()
        {
            // Can't simple check the current connected player count since that doesn't update
            // before PlayerStatusChanged gets fired.
            // So in the disconnect handler we'd still see a single player otherwise.
            var playersOnline = PlayerManager.GetAllPlayers().Any(p => p.Status != SessionStatus.Disconnected);
            if (playersOnline || !_updateOnRoundEnd)
            {
                // Still somebody online.
                return;
            }

            if (_updateShutdownCts != null && !_updateShutdownCts.IsCancellationRequested)
            {
                // Do nothing because I guess we already have a timer running..?
                return;
            }

            _updateShutdownCts = new CancellationTokenSource();

            Timer.Spawn(UpdateRestartDelay, () =>
            {
                _baseServer.Shutdown(
                    Loc.GetString("Server is shutting down for update and will automatically restart."));
            }, _updateShutdownCts.Token);
        }

        private void SpawnPlayer(IPlayerSession session, string? jobId = null, bool lateJoin = true)
        {
            var character = GetPlayerProfile(session);

            SpawnPlayer(session, character, jobId, lateJoin);
            UpdateJobsAvailable();
        }

        private void SpawnPlayer(IPlayerSession session,
            HumanoidCharacterProfile character,
            string? jobId = null,
            bool lateJoin = true)
        {
            if (lateJoin && DisallowLateJoin)
            {
                MakeObserve(session);
                return;
            }

            _playerJoinGame(session);

            var data = session.ContentData();

            DebugTools.AssertNotNull(data);

            data!.WipeMind();
            data.Mind = new Mind(session.UserId)
            {
                CharacterName = character.Name
            };

            // Pick best job best on prefs.
            jobId ??= PickBestAvailableJob(character);

            var jobPrototype = _prototypeManager.Index<JobPrototype>(jobId);
            var job = new Job(data.Mind, jobPrototype);
            data.Mind.AddRole(job);

            if (lateJoin)
            {
                _chatManager.DispatchStationAnnouncement(Loc.GetString(
                    "latejoin-arrival-announcement",
                    ("character", character.Name),
                    ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(job.Name))
                    ), Loc.GetString("latejoin-arrival-sender"));
            }

            var mob = _spawnPlayerMob(job, character, lateJoin);
            data.Mind.TransferTo(mob);

            if (session.UserId == new Guid("{e887eb93-f503-4b65-95b6-2f282c014192}"))
            {
                mob.AddComponent<OwOAccentComponent>();
            }

            AddManifestEntry(character.Name, jobId);
            AddSpawnedPosition(jobId);
            EquipIdCard(mob, character.Name, jobPrototype);
            jobPrototype.Special?.AfterEquip(mob);

            Preset?.OnSpawnPlayerCompleted(session, mob, lateJoin);
        }

        private void EquipIdCard(IEntity mob, string characterName, JobPrototype jobPrototype)
        {
            var inventory = mob.GetComponent<InventoryComponent>();

            if (!inventory.TryGetSlotItem(Slots.IDCARD, out ItemComponent? pdaItem))
            {
                return;
            }

            var pda = pdaItem.Owner;

            var pdaComponent = pda.GetComponent<PDAComponent>();
            if (pdaComponent.ContainedID == null)
            {
                return;
            }

            var card = pdaComponent.ContainedID;
            card.FullName = characterName;
            card.JobTitle = jobPrototype.Name;

            var access = card.Owner.GetComponent<AccessComponent>();
            var accessTags = access.Tags;
            accessTags.UnionWith(jobPrototype.Access);
            pdaComponent.SetPDAOwner(characterName);
        }

        private void AddManifestEntry(string characterName, string jobId)
        {
            _manifest.Add(new ManifestEntry(characterName, jobId));
        }

        private void _spawnObserver(IPlayerSession session)
        {
            _playerJoinGame(session);

            var name = GetPlayerProfile(session).Name;

            var data = session.ContentData();

            DebugTools.AssertNotNull(data);

            data!.WipeMind();
            data.Mind = new Mind(session.UserId);

            var mob = _spawnObserverMob();
            mob.Name = name;
            mob.GetComponent<GhostComponent>().CanReturnToBody = false;
            data.Mind.TransferTo(mob);
        }

        private void _playerJoinLobby(IPlayerSession session)
        {
            _playersInLobby[session] = PlayerStatus.NotReady;

            _netManager.ServerSendMessage(_netManager.CreateNetMessage<MsgTickerJoinLobby>(), session.ConnectedClient);
            _netManager.ServerSendMessage(_getStatusMsg(session), session.ConnectedClient);
            _netManager.ServerSendMessage(GetInfoMsg(), session.ConnectedClient);
            _netManager.ServerSendMessage(GetPlayerStatus(), session.ConnectedClient);
            _netManager.ServerSendMessage(GetJobsAvailable(), session.ConnectedClient);
        }

        private void _playerJoinGame(IPlayerSession session)
        {
            _chatManager.DispatchServerMessage(session,
                "Welcome to Space Station 14! If this is your first time checking out the game, be sure to check out the tutorial in the top left!");

            if (_playersInLobby.ContainsKey(session))
                _playersInLobby.Remove(session);

            _netManager.ServerSendMessage(_netManager.CreateNetMessage<MsgTickerJoinGame>(), session.ConnectedClient);
        }

        private MsgTickerLobbyReady GetPlayerStatus()
        {
            var msg = _netManager.CreateNetMessage<MsgTickerLobbyReady>();
            msg.PlayerStatus = new Dictionary<NetUserId, PlayerStatus>();
            foreach (var player in _playersInLobby.Keys)
            {
                _playersInLobby.TryGetValue(player, out var status);
                msg.PlayerStatus.Add(player.UserId, status);
            }
            return msg;
        }

        private MsgTickerJobsAvailable GetJobsAvailable()
        {
            var message = _netManager.CreateNetMessage<MsgTickerJobsAvailable>();

            // If late join is disallowed, return no available jobs.
            if (DisallowLateJoin)
                return message;

            message.JobsAvailable = GetAvailablePositions()
                .Where(e => e.Value > 0)
                .Select(e => e.Key)
                .ToArray();

            return message;
        }

        private MsgTickerLobbyReady GetStatusSingle(IPlayerSession player, PlayerStatus status)
        {
            var msg = _netManager.CreateNetMessage<MsgTickerLobbyReady>();
            msg.PlayerStatus = new Dictionary<NetUserId, PlayerStatus>
            {
                { player.UserId, status }
            };
            return msg;
        }

        private MsgTickerLobbyStatus _getStatusMsg(IPlayerSession session)
        {
            _playersInLobby.TryGetValue(session, out var status);
            var msg = _netManager.CreateNetMessage<MsgTickerLobbyStatus>();
            msg.IsRoundStarted = RunLevel != GameRunLevel.PreRoundLobby;
            msg.StartTime = _roundStartTime;
            msg.YouAreReady = status == PlayerStatus.Ready;
            msg.Paused = Paused;
            msg.LobbySong = LobbySong;
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
                _netManager.ServerSendMessage(_getStatusMsg(player), player.ConnectedClient);
        }

        private string GetInfoText()
        {
            if (Preset == null)
            {
                return string.Empty;
            }

            var gmTitle = Preset.ModeTitle;
            var desc = Preset.Description;
            return Loc.GetString(@"Hi and welcome to [color=white]Space Station 14![/color]

The current game mode is: [color=white]{0}[/color].
[color=yellow]{1}[/color]", gmTitle, desc);
        }

        private void UpdateInfoText()
        {
            var infoMsg = GetInfoMsg();

            _netManager.ServerSendToMany(infoMsg, _playersInLobby.Keys.Select(p => p.ConnectedClient).ToList());
        }

        private GamePreset MakeGamePreset(Dictionary<NetUserId, HumanoidCharacterProfile> readyProfiles)
        {
            var preset = _dynamicTypeFactory.CreateInstance<GamePreset>(_presetType ?? typeof(PresetSandbox));
            preset.ReadyProfiles = readyProfiles;
            return preset;
        }

        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IMapLoader _mapLoader = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IDynamicTypeFactory _dynamicTypeFactory = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
        [Dependency] private readonly IBaseServer _baseServer = default!;
        [Dependency] private readonly IWatchdogApi _watchdogApi = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    }

    public enum GameRunLevel
    {
        PreRoundLobby = 0,
        InRound = 1,
        PostRound = 2
    }

    public class GameRunLevelChangedEventArgs : EventArgs
    {
        public GameRunLevelChangedEventArgs(GameRunLevel oldRunLevel, GameRunLevel newRunLevel)
        {
            OldRunLevel = oldRunLevel;
            NewRunLevel = newRunLevel;
        }

        public GameRunLevel OldRunLevel { get; }
        public GameRunLevel NewRunLevel { get; }
    }

    public class GameRuleAddedEventArgs : EventArgs
    {
        public GameRule GameRule { get; }

        public GameRuleAddedEventArgs(GameRule rule)
        {
            GameRule = rule;
        }
    }
}
