using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Database;
using Content.Server.GameTicking.Events;
using Content.Server.Ghost;
using Content.Server.Maps;
using Content.Server.Mind;
using Content.Server.Players;
using Content.Server.Station;
using Content.Shared.CCVar;
using Content.Shared.Coordinates;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Station;
using Prometheus;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameTicking
{
    public partial class GameTicker
    {
        private static readonly Counter RoundNumberMetric = Metrics.CreateCounter(
            "ss14_round_number",
            "Round number.");

        private static readonly Gauge RoundLengthMetric = Metrics.CreateGauge(
            "ss14_round_length",
            "Round length in seconds.");

        [Dependency] private readonly IServerDbManager _db = default!;

        [ViewVariables]
        private TimeSpan _roundStartTimeSpan;

        [ViewVariables]
        private bool _startingRound;

        [ViewVariables]
        private GameRunLevel _runLevel;

        [ViewVariables]
        public GameRunLevel RunLevel
        {
            get => _runLevel;
            private set
            {
                if (_runLevel == value) return;

                var old = _runLevel;
                _runLevel = value;

                RaiseLocalEvent(new GameRunLevelChangedEvent(old, value));
            }
        }

        [ViewVariables]
        public int RoundId { get; private set; }

        private void PreRoundSetup()
        {
            DefaultMap = _mapManager.CreateMap();
            _pauseManager.AddUninitializedMap(DefaultMap);
            _startingRound = false;
            var startTime = _gameTiming.RealTime;
            var maps = new List<GameMapPrototype>() { _gameMapManager.GetSelectedMapChecked(true) };

            // Let game rules dictate what maps we should load.
            RaiseLocalEvent(new LoadingMapsEvent(maps));

            foreach (var map in maps)
            {
                var toLoad = DefaultMap;
                if (maps[0] != map)
                {
                    // Create other maps for the others since we need to.
                    toLoad = _mapManager.CreateMap();
                    _pauseManager.AddUninitializedMap(toLoad);
                }

                _mapLoader.LoadMap(toLoad, map.MapPath);

                var grids = _mapManager.GetAllMapGrids(toLoad).ToList();
                var dict = new Dictionary<string, StationId>();

                StationId SetupInitialStation(IMapGrid grid, GameMapPrototype map)
                {
                    var stationId = _stationSystem.InitialSetupStationGrid(grid.GridEntityId, map);
                    SetupGridStation(grid);

                    // ass!
                    _spawnPoint = grid.ToCoordinates();
                    return stationId;
                }

                // Iterate over all BecomesStation
                for (var i = 0; i < grids.Count; i++)
                {
                    var grid = grids[i];

                    // We still setup the grid
                    if (!TryComp<BecomesStationComponent>(grid.GridEntityId, out var becomesStation))
                        continue;

                    var stationId = SetupInitialStation(grid, map);

                    dict.Add(becomesStation.Id, stationId);
                }

                if (!dict.Any())
                {
                    // Oh jeez, no stations got loaded.
                    // We'll just take the first grid and setup that, then.

                    var grid = grids[0];
                    var stationId = SetupInitialStation(grid, map);

                    dict.Add("Station", stationId);
                }

                // Iterate over all PartOfStation
                for (var i = 0; i < grids.Count; i++)
                {
                    var grid = grids[i];
                    if (!TryComp<PartOfStationComponent>(grid.GridEntityId, out var partOfStation))
                        continue;
                    SetupGridStation(grid);

                    if (dict.TryGetValue(partOfStation.Id, out var stationId))
                    {
                        _stationSystem.AddGridToStation(grid.GridEntityId, stationId);
                    }
                    else
                    {
                        Logger.Error($"Grid {grid.Index} ({grid.GridEntityId}) specified that it was part of station {partOfStation.Id} which does not exist");
                    }
                }
            }

            var timeSpan = _gameTiming.RealTime - startTime;
            Logger.InfoS("ticker", $"Loaded maps in {timeSpan.TotalMilliseconds:N2}ms.");
        }

        private void SetupGridStation(IMapGrid grid)
        {
            var stationXform = EntityManager.GetComponent<TransformComponent>(grid.GridEntityId);

            if (StationOffset)
            {
                // Apply a random offset to the station grid entity.
                var x = _robustRandom.NextFloat(-MaxStationOffset, MaxStationOffset);
                var y = _robustRandom.NextFloat(-MaxStationOffset, MaxStationOffset);
                stationXform.LocalPosition = new Vector2(x, y);
            }

            if (StationRotation)
            {
                stationXform.LocalRotation = _robustRandom.NextFloat(MathF.Tau);
            }
        }

        public async void StartRound(bool force = false)
        {
#if EXCEPTION_TOLERANCE
            try
            {
#endif
                // If this game ticker is a dummy or the round is already being started, do nothing!
                if (DummyTicker || _startingRound)
                    return;

                _startingRound = true;

                DebugTools.Assert(RunLevel == GameRunLevel.PreRoundLobby);
                Logger.InfoS("ticker", "Starting round!");

                SendServerMessage(Loc.GetString("game-ticker-start-round"));

                AddGamePresetRules();

                List<IPlayerSession> readyPlayers;
                if (LobbyEnabled)
                {
                    readyPlayers = _playersInLobby.Where(p => p.Value == LobbyPlayerStatus.Ready).Select(p => p.Key)
                        .ToList();
                }
                else
                {
                    readyPlayers = _playersInLobby.Keys.ToList();
                }

                RoundLengthMetric.Set(0);

                var playerIds = _playersInLobby.Keys.Select(player => player.UserId.UserId).ToArray();
                RoundId = await _db.AddNewRound(playerIds);

                var startingEvent = new RoundStartingEvent();
                RaiseLocalEvent(startingEvent);

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

                var origReadyPlayers = readyPlayers.ToArray();

                var startAttempt = new RoundStartAttemptEvent(origReadyPlayers, force);
                RaiseLocalEvent(startAttempt);

                var presetTitle = _preset != null ? Loc.GetString(_preset.ModeTitle) : string.Empty;

                void FailedPresetRestart()
                {
                    SendServerMessage(Loc.GetString("game-ticker-start-round-cannot-start-game-mode-restart",
                        ("failedGameMode", presetTitle)));
                    RestartRound();
                    DelayStart(TimeSpan.FromSeconds(PresetFailedCooldownIncrease));
                }

                if (startAttempt.Cancelled)
                {
                    if (_configurationManager.GetCVar(CCVars.GameLobbyFallbackEnabled))
                    {
                        var oldPreset = _preset;
                        ClearGameRules();
                        SetGamePreset(_configurationManager.GetCVar(CCVars.GameLobbyFallbackPreset));
                        AddGamePresetRules();

                        startAttempt.Uncancel();
                        RaiseLocalEvent(startAttempt);

                        _chatManager.DispatchServerAnnouncement(
                            Loc.GetString("game-ticker-start-round-cannot-start-game-mode-fallback",
                                ("failedGameMode", presetTitle),
                                ("fallbackMode", Loc.GetString(_preset!.ModeTitle))));

                        if (startAttempt.Cancelled)
                        {
                            FailedPresetRestart();
                        }

                        RefreshLateJoinAllowed();
                    }
                    else
                    {
                        FailedPresetRestart();
                        return;
                    }
                }

                // MapInitialize *before* spawning players, our codebase is too shit to do it afterwards...
                _pauseManager.DoMapInitialize(DefaultMap);

                // Allow game rules to spawn players by themselves if needed. (For example, nuke ops or wizard)
                RaiseLocalEvent(new RulePlayerSpawningEvent(readyPlayers, profiles, force));

                var assignedJobs = AssignJobs(readyPlayers, profiles);

                // For players without jobs, give them the overflow job if they have that set...
                foreach (var player in origReadyPlayers)
                {
                    if (assignedJobs.ContainsKey(player))
                    {
                        continue;
                    }

                    var profile = profiles[player.UserId];
                    if (profile.PreferenceUnavailable == PreferenceUnavailableMode.SpawnAsOverflow)
                    {
                        // Pick a random station
                        var stations = _stationSystem.StationInfo.Keys.ToList();
                        _robustRandom.Shuffle(stations);

                        if (stations.Count == 0)
                        {
                            assignedJobs.Add(player, (FallbackOverflowJob, StationId.Invalid));
                            continue;
                        }

                        foreach (var station in stations)
                        {
                            // Pick a random overflow job from that station
                            var overflows = _stationSystem.StationInfo[station].MapPrototype.OverflowJobs.Clone();
                            _robustRandom.Shuffle(overflows);

                            // Stations with no overflow slots should simply get skipped over.
                            if (overflows.Count == 0)
                                continue;

                            // If the overflow exists, put them in as it.
                            assignedJobs.Add(player, (overflows[0], stations[0]));
                        }
                    }
                }

                // Spawn everybody in!
                foreach (var (player, (job, station)) in assignedJobs)
                {
                    SpawnPlayer(player, profiles[player.UserId], station, job, false);
                }

                RefreshLateJoinAllowed();

                // Allow rules to add roles to players who have been spawned in. (For example, on-station traitors)
                RaiseLocalEvent(new RulePlayerJobsAssignedEvent(assignedJobs.Keys.ToArray(), profiles, force));

                _roundStartDateTime = DateTime.UtcNow;
                RunLevel = GameRunLevel.InRound;

                _startingRound = false;

                _roundStartTimeSpan = _gameTiming.RealTime;
                SendStatusToAll();
                ReqWindowAttentionAll();
                UpdateLateJoinStatus();
                UpdateJobsAvailable();

#if EXCEPTION_TOLERANCE
            }
            catch(Exception e)
            {

                Logger.WarningS("ticker", $"Exception caught while trying to start the round! Restarting...");
                _runtimeLog.LogException(e, nameof(GameTicker));
                RestartRound();
            }
#endif
        }

        private void RefreshLateJoinAllowed()
        {
            var refresh = new RefreshLateJoinAllowedEvent();
            RaiseLocalEvent(refresh);
            DisallowLateJoin = refresh.DisallowLateJoin;
        }

        public void EndRound(string text = "")
        {
            // If this game ticker is a dummy, do nothing!
            if (DummyTicker)
                return;

            DebugTools.Assert(RunLevel == GameRunLevel.InRound);
            Logger.InfoS("ticker", "Ending round!");

            RunLevel = GameRunLevel.PostRound;

            //Tell every client the round has ended.
            var gamemodeTitle = _preset != null ? Loc.GetString(_preset.ModeTitle) : string.Empty;

            // Let things add text here.
            var textEv = new RoundEndTextAppendEvent();
            RaiseLocalEvent(textEv);

            var roundEndText = $"{text}\n{textEv.Text}";

            //Get the timespan of the round.
            var roundDuration = RoundDuration();

            //Generate a list of basic player info to display in the end round summary.
            var listOfPlayerInfo = new List<RoundEndMessageEvent.RoundEndPlayerInfo>();
            // Grab the great big book of all the Minds, we'll need them for this.
            var allMinds = Get<MindTrackerSystem>().AllMinds;
            foreach (var mind in allMinds)
            {
                if (mind != null)
                {
                    // Some basics assuming things fail
                    var userId = mind.OriginalOwnerUserId;
                    var playerOOCName = userId.ToString();
                    var connected = false;
                    var observer = mind.AllRoles.Any(role => role is ObserverRole);
                    // Continuing
                    if (_playerManager.TryGetSessionById(userId, out var ply))
                    {
                        connected = true;
                    }
                    PlayerData? contentPlayerData = null;
                    if (_playerManager.TryGetPlayerData(userId, out var playerData))
                    {
                        contentPlayerData = playerData.ContentData();
                    }
                    // Finish
                    var antag = mind.AllRoles.Any(role => role.Antagonist);

                    var playerIcName = string.Empty;

                    if (mind.CharacterName != null)
                        playerIcName = mind.CharacterName;
                    else if (mind.CurrentEntity != null)
                        playerIcName = EntityManager.GetComponent<MetaDataComponent>(mind.CurrentEntity.Value).EntityName;

                    var playerEndRoundInfo = new RoundEndMessageEvent.RoundEndPlayerInfo()
                    {
                        // Note that contentPlayerData?.Name sticks around after the player is disconnected.
                        // This is as opposed to ply?.Name which doesn't.
                        PlayerOOCName = contentPlayerData?.Name ?? "(IMPOSSIBLE: REGISTERED MIND WITH NO OWNER)",
                        // Character name takes precedence over current entity name
                        PlayerICName = playerIcName,
                        Role = antag
                            ? mind.AllRoles.First(role => role.Antagonist).Name
                            : mind.AllRoles.FirstOrDefault()?.Name ?? Loc.GetString("game-ticker-unknown-role"),
                        Antag = antag,
                        Observer = observer,
                        Connected = connected
                    };
                    listOfPlayerInfo.Add(playerEndRoundInfo);
                }
            }
            // This ordering mechanism isn't great (no ordering of minds) but functions
            var listOfPlayerInfoFinal = listOfPlayerInfo.OrderBy(pi => pi.PlayerOOCName).ToArray();
            _playersInGame.Clear();
            RaiseNetworkEvent(new RoundEndMessageEvent(gamemodeTitle, roundEndText, roundDuration, listOfPlayerInfoFinal.Length, listOfPlayerInfoFinal));
        }

        public void RestartRound()
        {
            // If this game ticker is a dummy, do nothing!
            if (DummyTicker)
                return;

            if (_updateOnRoundEnd)
            {
                _baseServer.Shutdown(Loc.GetString("game-ticker-shutdown-server-update"));
                return;
            }

            Logger.InfoS("ticker", "Restarting round!");

            SendServerMessage(Loc.GetString("game-ticker-restart-round"));

            RoundNumberMetric.Inc();

            RunLevel = GameRunLevel.PreRoundLobby;
            LobbySong = _robustRandom.Pick(_lobbyMusicCollection.PickFiles).ToString();
            ResettingCleanup();
            PreRoundSetup();

            if (!LobbyEnabled)
            {
                StartRound();
            }
            else
            {
                if (_playerManager.PlayerCount == 0)
                    _roundStartCountdownHasNotStartedYetDueToNoPlayers = true;
                else
                    _roundStartTime = _gameTiming.CurTime + LobbyDuration;

                SendStatusToAll();

                ReqWindowAttentionAll();
            }
        }

        /// <summary>
        ///     Cleanup that has to run to clear up anything from the previous round.
        ///     Stuff like wiping the previous map clean.
        /// </summary>
        private void ResettingCleanup()
        {
            // Move everybody currently in the server to lobby.
            foreach (var player in _playerManager.ServerSessions)
            {
                PlayerJoinLobby(player);
            }

            // Delete the minds of everybody.
            // TODO: Maybe move this into a separate manager?
            foreach (var unCastData in _playerManager.GetAllPlayerData())
            {
                unCastData.ContentData()?.WipeMind();
            }

            // Delete all entities.
            foreach (var entity in EntityManager.GetEntities().ToList())
            {
                // TODO: Maybe something less naive here?
                // FIXME: Actually, definitely.
                EntityManager.DeleteEntity(entity);
            }

            _startingRound = false;

            _mapManager.Restart();

            // Clear up any game rules.
            ClearGameRules();

            _gameRules.Clear();

            // Round restart cleanup event, so entity systems can reset.
            var ev = new RoundRestartCleanupEvent();
            RaiseLocalEvent(ev);

            // So clients' entity systems can clean up too...
            RaiseNetworkEvent(ev, Filter.Broadcast());

            _spawnedPositions.Clear();
            _manifest.Clear();
            DisallowLateJoin = false;
        }

        public bool DelayStart(TimeSpan time)
        {
            if (_runLevel != GameRunLevel.PreRoundLobby)
            {
                return false;
            }

            _roundStartTime += time;

            RaiseNetworkEvent(new TickerLobbyCountdownEvent(_roundStartTime, Paused));

            _chatManager.DispatchServerAnnouncement(Loc.GetString("game-ticker-delay-start", ("seconds",time.TotalSeconds)));

            return true;
        }

        private void UpdateRoundFlow(float frameTime)
        {
            if (RunLevel == GameRunLevel.InRound)
            {
                RoundLengthMetric.Inc(frameTime);
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

        public TimeSpan RoundDuration()
        {
            return _gameTiming.RealTime.Subtract(_roundStartTimeSpan);
        }
    }

    public enum GameRunLevel
    {
        PreRoundLobby = 0,
        InRound = 1,
        PostRound = 2
    }

    public class GameRunLevelChangedEvent
    {
        public GameRunLevel Old { get; }
        public GameRunLevel New { get; }

        public GameRunLevelChangedEvent(GameRunLevel old, GameRunLevel @new)
        {
            Old = old;
            New = @new;
        }
    }

    /// <summary>
    ///     Event raised before maps are loaded in pre-round setup.
    ///     Contains a list of game map prototypes to load; modify it if you want to load different maps,
    ///     for example as part of a game rule.
    /// </summary>
    public class LoadingMapsEvent : EntityEventArgs
    {
        public List<GameMapPrototype> Maps;

        public LoadingMapsEvent(List<GameMapPrototype> maps)
        {
            Maps = maps;
        }
    }

    /// <summary>
    ///     Event raised to refresh the late join status.
    ///     If you want to disallow late joins, listen to this and call Disallow.
    /// </summary>
    public class RefreshLateJoinAllowedEvent
    {
        public bool DisallowLateJoin { get; private set; } = false;

        public void Disallow()
        {
            DisallowLateJoin = true;
        }
    }

    /// <summary>
    ///     Attempt event raised on round start.
    ///     This can be listened to by GameRule systems to cancel round start if some condition is not met, like player count.
    /// </summary>
    public class RoundStartAttemptEvent : CancellableEntityEventArgs
    {
        public IPlayerSession[] Players { get; }
        public bool Forced { get; }

        public RoundStartAttemptEvent(IPlayerSession[] players, bool forced)
        {
            Players = players;
            Forced = forced;
        }
    }

    /// <summary>
    ///     Event raised before readied up players are spawned and given jobs by the GameTicker.
    ///     You can use this to spawn people off-station, like in the case of nuke ops or wizard.
    ///     Remove the players you spawned from the PlayerPool and call <see cref="GameTicker.PlayerJoinGame"/> on them.
    /// </summary>
    public class RulePlayerSpawningEvent
    {
        /// <summary>
        ///     Pool of players to be spawned.
        ///     If you want to handle a specific player being spawned, remove it from this list and do what you need.
        /// </summary>
        /// <remarks>If you spawn a player by yourself from this event, don't forget to call <see cref="GameTicker.PlayerJoinGame"/> on them.</remarks>
        public List<IPlayerSession> PlayerPool { get; }
        public IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> Profiles { get; }
        public bool Forced { get; }

        public RulePlayerSpawningEvent(List<IPlayerSession> playerPool, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles, bool forced)
        {
            PlayerPool = playerPool;
            Profiles = profiles;
            Forced = forced;
        }
    }

    /// <summary>
    ///     Event raised after players were assigned jobs by the GameTicker.
    ///     You can give on-station people special roles by listening to this event.
    /// </summary>
    public class RulePlayerJobsAssignedEvent
    {
        public IPlayerSession[] Players { get; }
        public IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> Profiles { get; }
        public bool Forced { get; }

        public RulePlayerJobsAssignedEvent(IPlayerSession[] players, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles, bool forced)
        {
            Players = players;
            Profiles = profiles;
            Forced = forced;
        }
    }

    /// <summary>
    ///     Event raised to allow subscribers to add text to the round end summary screen.
    /// </summary>
    public class RoundEndTextAppendEvent
    {
        private bool _doNewLine;

        /// <summary>
        ///     Text to display in the round end summary screen.
        /// </summary>
        public string Text { get; private set; } = string.Empty;

        /// <summary>
        ///     Invoke this method to add text to the round end summary screen.
        /// </summary>
        /// <param name="text"></param>
        public void AddLine(string text)
        {
            if (_doNewLine)
                Text += "\n";

            Text += text;
            _doNewLine = true;
        }
    }
}
