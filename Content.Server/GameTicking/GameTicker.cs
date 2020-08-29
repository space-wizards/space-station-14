using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Markers;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Observer;
using Content.Server.GameObjects.Components.PDA;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Accessible;
using Content.Server.GameObjects.EntitySystems.Atmos;
using Content.Server.GameObjects.EntitySystems.StationEvents;
using Content.Server.GameTicking.GamePresets;
using Content.Server.Interfaces;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Mobs;
using Content.Server.Mobs.Roles;
using Content.Server.Players;
using Content.Shared;
using Content.Shared.Chat;
using Content.Shared.GameObjects.Components.PDA;
using Content.Shared.Network.NetMessages;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Prometheus;
using Robust.Server.Interfaces;
using Robust.Server.Interfaces.Maps;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
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
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;
using Timer = Robust.Shared.Timers.Timer;

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

        private const string PlayerPrototypeName = "HumanMob_Content";
        private const string ObserverPrototypeName = "MobObserver";
        private const string MapFile = "Maps/saltern.yml";
        private static TimeSpan _roundStartTimeSpan;

        [ViewVariables] private readonly List<GameRule> _gameRules = new List<GameRule>();
        [ViewVariables] private readonly List<ManifestEntry> _manifest = new List<ManifestEntry>();

        [ViewVariables]
        private readonly Dictionary<IPlayerSession, PlayerStatus> _playersInLobby = new Dictionary<IPlayerSession, PlayerStatus>();

        [ViewVariables] private bool _initialized;

        [ViewVariables] private Type _presetType;

        [ViewVariables] private DateTime _pauseTime;
        [ViewVariables] private bool _roundStartCountdownHasNotStartedYetDueToNoPlayers;
        private DateTime _roundStartTimeUtc;
        [ViewVariables] private GameRunLevel _runLevel;
        [ViewVariables(VVAccess.ReadWrite)] private GridCoordinates _spawnPoint;

        [ViewVariables] private bool DisallowLateJoin { get; set; } = false;

        [ViewVariables] private bool LobbyEnabled => _configurationManager.GetCVar<bool>("game.lobbyenabled");

        [ViewVariables] private bool _updateOnRoundEnd;
        private CancellationTokenSource _updateShutdownCts;


        [ViewVariables] public bool Paused { get; private set; }

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

        public event Action<GameRunLevelChangedEventArgs> OnRunLevelChanged;
        public event Action<GameRuleAddedEventArgs> OnRuleAdded;

        private TimeSpan LobbyDuration =>
            TimeSpan.FromSeconds(_configurationManager.GetCVar<int>("game.lobbyduration"));

        public override void Initialize()
        {
            base.Initialize();

            DebugTools.Assert(!_initialized);

            _configurationManager.RegisterCVar("game.lobbyenabled", false, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("game.lobbyduration", 20, CVar.ARCHIVE);
            _configurationManager.RegisterCVar("game.defaultpreset", "Suspicion", CVar.ARCHIVE);
            _configurationManager.RegisterCVar("game.fallbackpreset", "Sandbox", CVar.ARCHIVE);

            _configurationManager.RegisterCVar("game.enablewin", true, CVar.CHEAT);

            PresetSuspicion.RegisterCVars(_configurationManager);

            _netManager.RegisterNetMessage<MsgTickerJoinLobby>(nameof(MsgTickerJoinLobby));
            _netManager.RegisterNetMessage<MsgTickerJoinGame>(nameof(MsgTickerJoinGame));
            _netManager.RegisterNetMessage<MsgTickerLobbyStatus>(nameof(MsgTickerLobbyStatus));
            _netManager.RegisterNetMessage<MsgTickerLobbyInfo>(nameof(MsgTickerLobbyInfo));
            _netManager.RegisterNetMessage<MsgTickerLobbyCountdown>(nameof(MsgTickerLobbyCountdown));
            _netManager.RegisterNetMessage<MsgTickerLobbyReady>(nameof(MsgTickerLobbyReady));
            _netManager.RegisterNetMessage<MsgRoundEndMessage>(nameof(MsgRoundEndMessage));
            _netManager.RegisterNetMessage<MsgRequestWindowAttention>(nameof(MsgRequestWindowAttention));
            _netManager.RegisterNetMessage<MsgTickerLateJoinStatus>(nameof(MsgTickerLateJoinStatus));

            SetStartPreset(_configurationManager.GetCVar<string>("game.defaultpreset"));

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
                _roundStartTimeUtc > DateTime.UtcNow ||
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
            _resettingCleanup();
            _preRoundSetup();

            if (!LobbyEnabled)
            {
                StartRound();
            }
            else
            {
                if (PlayerManager.PlayerCount == 0)
                    _roundStartCountdownHasNotStartedYetDueToNoPlayers = true;
                else
                    _roundStartTimeUtc = DateTime.UtcNow + LobbyDuration;

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

        public async void StartRound(bool force = false)
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
            var profiles = (await _prefsManager.GetSelectedProfilesForPlayersAsync(
                readyPlayers
                    .Select(p => p.Name).ToList()))
                    .ToDictionary(p => p.Key, p => (HumanoidCharacterProfile) p.Value);

            foreach (var readyPlayer in readyPlayers)
            {
                if (!profiles.ContainsKey(readyPlayer.Name))
                {
                    profiles.Add(readyPlayer.Name, HumanoidCharacterProfile.Default());
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

                var profile = profiles[player.Name];
                if (profile.PreferenceUnavailable == PreferenceUnavailableMode.SpawnAsOverflow)
                {
                    assignedJobs.Add(player, OverflowJob);
                }
            }

            // Spawn everybody in!
            foreach (var (player, job) in assignedJobs)
            {
                SpawnPlayer(player, profiles[player.Name], job, false);
            }

            // Time to start the preset.
            var preset = MakeGamePreset(profiles);

            DisallowLateJoin |= preset.DisallowLateJoin;

            if (!preset.Start(assignedJobs.Keys.ToList(), force))
            {
                SetStartPreset(_configurationManager.GetCVar<string>("game.fallbackpreset"));
                var newPreset = MakeGamePreset(profiles);
                _chatManager.DispatchServerAnnouncement(
                    $"Failed to start {preset.ModeTitle} mode! Defaulting to {newPreset.ModeTitle}...");
                if (!newPreset.Start(readyPlayers, force))
                {
                    throw new ApplicationException("Fallback preset failed to start!");
                }

                DisallowLateJoin = false;
                DisallowLateJoin |= newPreset.DisallowLateJoin;
            }

            _roundStartTimeSpan = IoCManager.Resolve<IGameTiming>().RealTime;
            _sendStatusToAll();
            ReqWindowAttentionAll();
            UpdateLateJoinStatus();
        }

        private void UpdateLateJoinStatus()
        {
            var msg = new MsgTickerLateJoinStatus(null) {Disallowed = DisallowLateJoin};
            _netManager.ServerSendToAll(msg);
        }

        private void SendServerMessage(string message)
        {
            var msg = _netManager.CreateNetMessage<MsgChatMessage>();
            msg.Channel = ChatChannel.Server;
            msg.Message = message;
            IoCManager.Resolve<IServerNetManager>().ServerSendToAll(msg);
        }

        private async Task<HumanoidCharacterProfile> GetPlayerProfileAsync(IPlayerSession p) =>
            (HumanoidCharacterProfile) (await _prefsManager.GetPreferencesAsync(p.SessionId.Username))
            .SelectedCharacter;

        public void EndRound(string roundEndText = "")
        {
            DebugTools.Assert(RunLevel == GameRunLevel.InRound);
            Logger.InfoS("ticker", "Ending round!");

            RunLevel = GameRunLevel.PostRound;

            //Tell every client the round has ended.
            var roundEndMessage = _netManager.CreateNetMessage<MsgRoundEndMessage>();
            roundEndMessage.GamemodeTitle = MakeGamePreset(null).ModeTitle;
            roundEndMessage.RoundEndText = roundEndText;

            //Get the timespan of the round.
            roundEndMessage.RoundDuration = IoCManager.Resolve<IGameTiming>().RealTime.Subtract(_roundStartTimeSpan);

            //Generate a list of basic player info to display in the end round summary.
            var listOfPlayerInfo = new List<RoundEndPlayerInfo>();
            foreach (var ply in PlayerManager.GetAllPlayers().OrderBy(p => p.Name))
            {
                var mind = ply.ContentData().Mind;
                if (mind != null)
                {
                    _playersInLobby.TryGetValue(ply, out var status);
                    var antag = mind.AllRoles.Any(role => role.Antagonist);
                    var playerEndRoundInfo = new RoundEndPlayerInfo()
                    {
                        PlayerOOCName = ply.Name,
                        PlayerICName = mind.CurrentEntity.Name,
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
            targetPlayer.ContentData().WipeMind();

            if (LobbyEnabled)
                _playerJoinLobby(targetPlayer);
            else
                SpawnPlayerAsync(targetPlayer);
        }

        public void MakeObserve(IPlayerSession player)
        {
            if (!_playersInLobby.ContainsKey(player)) return;

            _spawnObserver(player);
            _playersInLobby[player] = PlayerStatus.Observer;
            _netManager.ServerSendToAll(GetStatusSingle(player, PlayerStatus.Observer));
        }

        public void MakeJoinGame(IPlayerSession player, string jobId = null)
        {
            if (!_playersInLobby.ContainsKey(player)) return;

            SpawnPlayerAsync(player, jobId);
        }

        public void ToggleReady(IPlayerSession player, bool ready)
        {
            if (!_playersInLobby.ContainsKey(player)) return;

            var status = ready ? PlayerStatus.Ready : PlayerStatus.NotReady;
            _playersInLobby[player] = ready ? PlayerStatus.Ready : PlayerStatus.NotReady;
            _netManager.ServerSendMessage(_getStatusMsg(player), player.ConnectedClient);
            _netManager.ServerSendToAll(GetStatusSingle(player, status));
        }

        public T AddGameRule<T>() where T : GameRule, new()
        {
            var instance = _dynamicTypeFactory.CreateInstance<T>();

            _gameRules.Add(instance);
            instance.Added();

            OnRuleAdded?.Invoke(new GameRuleAddedEventArgs(instance));

            return instance;
        }

        public bool HasGameRule(Type t)
        {
            if (t == null || !typeof(GameRule).IsAssignableFrom(t))
                return false;

            foreach (var rule in _gameRules)
            {
                if (rule.GetType().IsAssignableFrom(t))
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

        public bool TryGetPreset(string name, out Type type)
        {
            type = name.ToLower() switch
            {
                "sandbox" => typeof(PresetSandbox),
                "deathmatch" => typeof(PresetDeathMatch),
                "suspicion" => typeof(PresetSuspicion),
                _ => default
            };

            return type != default;
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
                throw new NotSupportedException();
            }

            SetStartPreset(type, force);
        }

        public bool DelayStart(TimeSpan time)
        {
            if (_runLevel != GameRunLevel.PreRoundLobby)
            {
                return false;
            }

            _roundStartTimeUtc += time;

            var lobbyCountdownMessage = _netManager.CreateNetMessage<MsgTickerLobbyCountdown>();
            lobbyCountdownMessage.StartTime = _roundStartTimeUtc;
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
                _pauseTime = DateTime.UtcNow;
            }
            else if (_pauseTime != default)
            {
                _roundStartTimeUtc += DateTime.UtcNow - _pauseTime;
            }

            var lobbyCountdownMessage = _netManager.CreateNetMessage<MsgTickerLobbyCountdown>();
            lobbyCountdownMessage.StartTime = _roundStartTimeUtc;
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

        private IEntity _spawnPlayerMob(Job job, bool lateJoin = true)
        {
            GridCoordinates coordinates = lateJoin ? GetLateJoinSpawnPoint() : GetJobSpawnPoint(job.Prototype.ID);
            var entity = _entityManager.SpawnEntity(PlayerPrototypeName, coordinates);
            var startingGear = _prototypeManager.Index<StartingGearPrototype>(job.StartingGear);
            EquipStartingGear(entity, startingGear);

            return entity;
        }

        public void EquipStartingGear(IEntity entity, StartingGearPrototype startingGear)
        {
            if (entity.TryGetComponent(out InventoryComponent inventory))
            {
                var gear = startingGear.Equipment;

                foreach (var (slot, equipmentStr) in gear)
                {
                    var equipmentEntity = _entityManager.SpawnEntity(equipmentStr, entity.Transform.GridPosition);
                    inventory.Equip(slot, equipmentEntity.GetComponent<ItemComponent>());
                }
            }

            if (entity.TryGetComponent(out HandsComponent handsComponent))
            {
                var inhand = startingGear.Inhand;
                foreach (var (hand, prototype) in inhand)
                {
                    var inhandEntity = _entityManager.SpawnEntity(prototype, entity.Transform.GridPosition);
                    handsComponent.PutInHand(inhandEntity.GetComponent<ItemComponent>(), hand);
                }
            }
        }

        private void ApplyCharacterProfile(IEntity entity, ICharacterProfile profile)
        {
            if (profile is null)
                return;
            entity.GetComponent<HumanoidAppearanceComponent>().UpdateFromProfile(profile);
            entity.Name = profile.Name;
        }

        private IEntity _spawnObserverMob()
        {
            var coordinates = GetObserverSpawnPoint();
            return _entityManager.SpawnEntity(ObserverPrototypeName, coordinates);
        }

        public GridCoordinates GetLateJoinSpawnPoint()
        {
            var location = _spawnPoint;

            var possiblePoints = new List<GridCoordinates>();
            foreach (var entity in _entityManager.GetEntities(new TypeEntityQuery(typeof(SpawnPointComponent))))
            {
                var point = entity.GetComponent<SpawnPointComponent>();
                if (point.SpawnType == SpawnPointType.LateJoin) possiblePoints.Add(entity.Transform.GridPosition);
            }

            if (possiblePoints.Count != 0) location = _robustRandom.Pick(possiblePoints);

            return location;
        }

        public GridCoordinates GetJobSpawnPoint(string jobId)
        {
            var location = _spawnPoint;

            var possiblePoints = new List<GridCoordinates>();
            foreach (var entity in _entityManager.GetEntities(new TypeEntityQuery(typeof(SpawnPointComponent))))
            {
                var point = entity.GetComponent<SpawnPointComponent>();
                if (point.SpawnType == SpawnPointType.Job && point.Job.ID == jobId)
                    possiblePoints.Add(entity.Transform.GridPosition);
            }

            if (possiblePoints.Count != 0) location = _robustRandom.Pick(possiblePoints);

            return location;
        }

        public GridCoordinates GetObserverSpawnPoint()
        {
            var location = _spawnPoint;

            var possiblePoints = new List<GridCoordinates>();
            foreach (var entity in _entityManager.GetEntities(new TypeEntityQuery(typeof(SpawnPointComponent))))
            {
                var point = entity.GetComponent<SpawnPointComponent>();
                if (point.SpawnType == SpawnPointType.Observer)
                    possiblePoints.Add(entity.Transform.GridPosition);
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
            // Delete all entities.
            foreach (var entity in _entityManager.GetEntities().ToList())
                // TODO: Maybe something less naive here?
                // FIXME: Actually, definitely.
                entity.Delete();

            _mapManager.Restart();

            // Delete the minds of everybody.
            // TODO: Maybe move this into a separate manager?
            foreach (var unCastData in PlayerManager.GetAllPlayerData()) unCastData.ContentData().WipeMind();

            // Clear up any game rules.
            foreach (var rule in _gameRules) rule.Removed();

            _gameRules.Clear();

            // Move everybody currently in the server to lobby.
            foreach (var player in PlayerManager.GetAllPlayers())
            {
                if (_playersInLobby.ContainsKey(player)) continue;

                _playerJoinLobby(player);
            }

            EntitySystem.Get<GasTileOverlaySystem>().ResettingCleanup();
            EntitySystem.Get<PathfindingSystem>().ResettingCleanup();
            EntitySystem.Get<AiReachableSystem>().ResettingCleanup();
            EntitySystem.Get<WireHackingSystem>().ResetLayouts();
            EntitySystem.Get<StationEventSystem>().ResettingCleanup();

            _spawnedPositions.Clear();
            _manifest.Clear();
            DisallowLateJoin = false;
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

        protected override void PlayerStatusChanged(object sender, SessionStatusEventArgs args)
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
                    _chatManager.DispatchServerAnnouncement($"Player {args.Session.SessionId} joined server!");

                    if (LobbyEnabled && _roundStartCountdownHasNotStartedYetDueToNoPlayers)
                    {
                        _roundStartCountdownHasNotStartedYetDueToNoPlayers = false;
                        _roundStartTimeUtc = DateTime.UtcNow + LobbyDuration;
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

                        SpawnPlayerAsync(session);
                    }
                    else
                    {
                        if (data.Mind.CurrentEntity == null)
                        {
                            SpawnPlayerAsync(session);
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

                    _chatManager.DispatchServerAnnouncement($"Player {args.Session.SessionId} left server!");
                    ServerEmptyUpdateRestartCheck();
                    break;
                }
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

        private async void SpawnPlayerAsync(IPlayerSession session, string jobId = null, bool lateJoin = true)
        {
            var character = (HumanoidCharacterProfile) (await _prefsManager
                    .GetPreferencesAsync(session.SessionId.Username))
                .SelectedCharacter;

            SpawnPlayer(session, character, jobId, lateJoin);
        }

        private void SpawnPlayer(IPlayerSession session,
            HumanoidCharacterProfile character,
            string jobId = null,
            bool lateJoin = true)
        {
            if (lateJoin && DisallowLateJoin)
            {
                MakeObserve(session);
                return;
            }

            _playerJoinGame(session);

            var data = session.ContentData();
            data.WipeMind();
            data.Mind = new Mind(session.SessionId)
            {
                CharacterName = character.Name
            };

            if (jobId == null)
            {
                // Pick best job best on prefs.
                jobId = PickBestAvailableJob(character);
            }

            var jobPrototype = _prototypeManager.Index<JobPrototype>(jobId);
            var job = new Job(data.Mind, jobPrototype);
            data.Mind.AddRole(job);

            var mob = _spawnPlayerMob(job, lateJoin);
            data.Mind.TransferTo(mob);
            ApplyCharacterProfile(mob, character);

            AddManifestEntry(character.Name, jobId);
            AddSpawnedPosition(jobId);
            EquipIdCard(mob, character.Name, jobPrototype);
            jobPrototype.Special?.AfterEquip(mob);
        }

        private void EquipIdCard(IEntity mob, string characterName, JobPrototype jobPrototype)
        {
            var inventory = mob.GetComponent<InventoryComponent>();

            if (!inventory.TryGetSlotItem(Slots.IDCARD, out ItemComponent pdaItem))
            {
                return;
            }

            var pda = pdaItem.Owner;

            var pdaComponent = pda.GetComponent<PDAComponent>();
            if (pdaComponent.IdSlotEmpty)
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
            var mindComponent = mob.GetComponent<MindComponent>();
            if (mindComponent.HasMind) //Redundancy checks.
            {
                if (mindComponent.Mind.AllRoles.Any(role => role.Antagonist)) //Give antags a new uplinkaccount.
                {
                    var uplinkAccount =
                        new UplinkAccount(mob.Uid,
                            20); //TODO: make me into a variable based on server pop or something.
                    pdaComponent.InitUplinkAccount(uplinkAccount);
                }
            }
        }

        private void AddManifestEntry(string characterName, string jobId)
        {
            _manifest.Add(new ManifestEntry(characterName, jobId));
        }

        private async void _spawnObserver(IPlayerSession session)
        {
            _playerJoinGame(session);

            var name = (await _prefsManager
                    .GetPreferencesAsync(session.SessionId.Username))
                .SelectedCharacter.Name;

            var data = session.ContentData();
            data.WipeMind();
            data.Mind = new Mind(session.SessionId);

            var mob = _spawnObserverMob();
            mob.Name = name;
            mob.GetComponent<GhostComponent>().CanReturnToBody = false;
            data.Mind.TransferTo(mob);
        }

        private void _playerJoinLobby(IPlayerSession session)
        {
            _playersInLobby.Add(session, PlayerStatus.NotReady);

            _prefsManager.OnClientConnected(session);
            _netManager.ServerSendMessage(_netManager.CreateNetMessage<MsgTickerJoinLobby>(), session.ConnectedClient);
            _netManager.ServerSendMessage(_getStatusMsg(session), session.ConnectedClient);
            _netManager.ServerSendMessage(GetInfoMsg(), session.ConnectedClient);
            _netManager.ServerSendMessage(GetPlayerStatus(), session.ConnectedClient);
        }

        private void _playerJoinGame(IPlayerSession session)
        {
            _chatManager.DispatchServerMessage(session,
                "Welcome to Space Station 14! If this is your first time checking out the game, be sure to check out the tutorial in the top left!");
            if (_playersInLobby.ContainsKey(session)) _playersInLobby.Remove(session);

            _netManager.ServerSendMessage(_netManager.CreateNetMessage<MsgTickerJoinGame>(), session.ConnectedClient);
        }

        private MsgTickerLobbyReady GetPlayerStatus()
        {
            var msg = _netManager.CreateNetMessage<MsgTickerLobbyReady>();
            msg.PlayerStatus = new Dictionary<NetSessionId, PlayerStatus>();
            foreach (var player in _playersInLobby.Keys)
            {
                _playersInLobby.TryGetValue(player, out var status);
                msg.PlayerStatus.Add(player.SessionId, status);
            }
            return msg;
        }

        private MsgTickerLobbyReady GetStatusSingle(IPlayerSession player, PlayerStatus status)
        {
            var msg = _netManager.CreateNetMessage<MsgTickerLobbyReady>();
            msg.PlayerStatus = new Dictionary<NetSessionId, PlayerStatus>
            {
                { player.SessionId, status }
            };
            return msg;
        }

        private MsgTickerLobbyStatus _getStatusMsg(IPlayerSession session)
        {
            _playersInLobby.TryGetValue(session, out var status);
            var msg = _netManager.CreateNetMessage<MsgTickerLobbyStatus>();
            msg.IsRoundStarted = RunLevel != GameRunLevel.PreRoundLobby;
            msg.StartTime = _roundStartTimeUtc;
            msg.YouAreReady = status == PlayerStatus.Ready;
            msg.Paused = Paused;
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
            var gmTitle = MakeGamePreset(null).ModeTitle;
            var desc = MakeGamePreset(null).Description;
            return _localization.GetString(@"Hi and welcome to [color=white]Space Station 14![/color]

The current game mode is: [color=white]{0}[/color].
[color=yellow]{1}[/color]", gmTitle, desc);
        }

        private void UpdateInfoText()
        {
            var infoMsg = GetInfoMsg();

            _netManager.ServerSendToMany(infoMsg, _playersInLobby.Keys.Select(p => p.ConnectedClient).ToList());
        }

        private GamePreset MakeGamePreset(Dictionary<string, HumanoidCharacterProfile> readyProfiles)
        {
            var preset = _dynamicTypeFactory.CreateInstance<GamePreset>(_presetType ?? typeof(PresetSandbox));
            preset.readyProfiles = readyProfiles;
            return preset;
        }

        [Dependency] private IEntityManager _entityManager = default!;
        [Dependency] private IMapManager _mapManager = default!;
        [Dependency] private IMapLoader _mapLoader = default!;
        [Dependency] private IGameTiming _gameTiming = default!;
        [Dependency] private IConfigurationManager _configurationManager = default!;
        [Dependency] private IChatManager _chatManager = default!;
        [Dependency] private IServerNetManager _netManager = default!;
        [Dependency] private IDynamicTypeFactory _dynamicTypeFactory = default!;
        [Dependency] private IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ILocalizationManager _localization = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
        [Dependency] private readonly IBaseServer _baseServer = default!;
        [Dependency] private readonly IWatchdogApi _watchdogApi = default!;
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
