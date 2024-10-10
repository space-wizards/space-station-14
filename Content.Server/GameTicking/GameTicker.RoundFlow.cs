using System.Linq;
using Content.Server.Announcements;
using Content.Server.Discord;
using Content.Server.GameTicking.Events;
using Content.Server.Ghost;
using Content.Server.Maps;
using Content.Server.Roles;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Prometheus;
using Robust.Server.Maps;
using Robust.Shared.Asynchronous;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        [Dependency] private readonly DiscordWebhook _discord = default!;
        [Dependency] private readonly RoleSystem _role = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;

        private static readonly Counter RoundNumberMetric = Metrics.CreateCounter(
            "ss14_round_number",
            "Round number.");

        private static readonly Gauge RoundLengthMetric = Metrics.CreateGauge(
            "ss14_round_length",
            "Round length in seconds.");

#if EXCEPTION_TOLERANCE
        [ViewVariables]
        private int _roundStartFailCount = 0;
#endif

        [ViewVariables]
        private bool _startingRound;

        [ViewVariables]
        private GameRunLevel _runLevel;

        private RoundEndMessageEvent.RoundEndPlayerInfo[]? _replayRoundPlayerInfo;

        private string? _replayRoundText;

        [ViewVariables]
        public GameRunLevel RunLevel
        {
            get => _runLevel;
            private set
            {
                // Game admins can run `restartroundnow` while still in-lobby, which'd break things with this check.
                // if (_runLevel == value) return;

                var old = _runLevel;
                _runLevel = value;

                RaiseLocalEvent(new GameRunLevelChangedEvent(old, value));
            }
        }

        /// <summary>
        /// Returns true if the round's map is eligible to be updated.
        /// </summary>
        /// <returns></returns>
        public bool CanUpdateMap()
        {
            return RunLevel == GameRunLevel.PreRoundLobby &&
                   _roundStartTime - RoundPreloadTime > _gameTiming.CurTime;
        }

        /// <summary>
        ///     Loads all the maps for the given round.
        /// </summary>
        /// <remarks>
        ///     Must be called before the runlevel is set to InRound.
        /// </remarks>
        private void LoadMaps()
        {
            if (_mapManager.MapExists(DefaultMap))
                return;

            AddGamePresetRules();

            DefaultMap = _mapManager.CreateMap();
            _mapManager.AddUninitializedMap(DefaultMap);

            var maps = new List<GameMapPrototype>();

            // the map might have been force-set by something
            // (i.e. votemap or forcemap)
            var mainStationMap = _gameMapManager.GetSelectedMap();
            if (mainStationMap == null)
            {
                // otherwise set the map using the config rules
                _gameMapManager.SelectMapByConfigRules();
                mainStationMap = _gameMapManager.GetSelectedMap();
            }

            // Small chance the above could return no map.
            // ideally SelectMapByConfigRules will always find a valid map
            if (mainStationMap != null)
            {
                maps.Add(mainStationMap);
            }
            else
            {
                throw new Exception("invalid config; couldn't select a valid station map!");
            }

            if (CurrentPreset?.MapPool != null &&
                _prototypeManager.TryIndex<GameMapPoolPrototype>(CurrentPreset.MapPool, out var pool) &&
                !pool.Maps.Contains(mainStationMap.ID))
            {
                var msg = Loc.GetString("game-ticker-start-round-invalid-map",
                    ("map", mainStationMap.MapName),
                    ("mode", Loc.GetString(CurrentPreset.ModeTitle)));
                Log.Debug(msg);
                SendServerMessage(msg);
            }

            // Let game rules dictate what maps we should load.
            RaiseLocalEvent(new LoadingMapsEvent(maps));

            foreach (var map in maps)
            {
                var toLoad = DefaultMap;
                if (maps[0] != map)
                {
                    // Create other maps for the others since we need to.
                    toLoad = _mapManager.CreateMap();
                    _mapManager.AddUninitializedMap(toLoad);
                }

                LoadGameMap(map, toLoad, null);
            }
        }


        /// <summary>
        ///     Loads a new map, allowing systems interested in it to handle loading events.
        ///     In the base game, this is required to be used if you want to load a station.
        /// </summary>
        /// <param name="map">Game map prototype to load in.</param>
        /// <param name="targetMapId">Map to load into.</param>
        /// <param name="loadOptions">Map loading options, includes offset.</param>
        /// <param name="stationName">Name to assign to the loaded station.</param>
        /// <returns>All loaded entities and grids.</returns>
        public IReadOnlyList<EntityUid> LoadGameMap(GameMapPrototype map, MapId targetMapId, MapLoadOptions? loadOptions, string? stationName = null)
        {
            // Okay I specifically didn't set LoadMap here because this is typically called onto a new map.
            // whereas the command can also be used on an existing map.
            var loadOpts = loadOptions ?? new MapLoadOptions();

            if (map.MaxRandomOffset != 0f)
                loadOpts.Offset = _robustRandom.NextVector2(map.MaxRandomOffset);

            if (map.RandomRotation)
                loadOpts.Rotation = _robustRandom.NextAngle();

            var ev = new PreGameMapLoad(targetMapId, map, loadOpts);
            RaiseLocalEvent(ev);

            var gridIds = _map.LoadMap(targetMapId, ev.GameMap.MapPath.ToString(), ev.Options);

            _metaData.SetEntityName(_mapManager.GetMapEntityId(targetMapId), map.MapName);

            var gridUids = gridIds.ToList();
            RaiseLocalEvent(new PostGameMapLoad(map, targetMapId, gridUids, stationName));

            return gridUids;
        }

        public int ReadyPlayerCount()
        {
            var total = 0;
            foreach (var (userId, status) in _playerGameStatuses)
            {
                if (LobbyEnabled && status == PlayerGameStatus.NotReadyToPlay)
                    continue;

                if (!_playerManager.TryGetSessionById(userId, out _))
                    continue;

                if (_banManager.GetRoleBans(userId) == null)
                    continue;

                total++;
            }

            return total;
        }

        public void StartRound(bool force = false)
        {
#if EXCEPTION_TOLERANCE
            try
            {
#endif
            // If this game ticker is a dummy or the round is already being started, do nothing!
            if (DummyTicker || _startingRound)
                return;

            _startingRound = true;

            if (RoundId == 0)
                IncrementRoundNumber();

            ReplayStartRound();

            DebugTools.Assert(RunLevel == GameRunLevel.PreRoundLobby);
            _sawmill.Info("Starting round!");

            SendServerMessage(Loc.GetString("game-ticker-start-round"));

            var readyPlayers = new List<ICommonSession>();
            var readyPlayerProfiles = new Dictionary<NetUserId, HumanoidCharacterProfile>();
            var autoDeAdmin = _cfg.GetCVar(CCVars.AdminDeadminOnJoin);
            foreach (var (userId, status) in _playerGameStatuses)
            {
                if (LobbyEnabled && status != PlayerGameStatus.ReadyToPlay) continue;
                if (!_playerManager.TryGetSessionById(userId, out var session)) continue;

                if (autoDeAdmin && _adminManager.IsAdmin(session))
                {
                    _adminManager.DeAdmin(session);
                }
#if DEBUG
                DebugTools.Assert(_userDb.IsLoadComplete(session), $"Player was readied up but didn't have user DB data loaded yet??");
#endif
                if (_banManager.GetRoleBans(userId) == null)
                {
                    Logger.ErrorS("RoleBans", $"Role bans for player {session} {userId} have not been loaded yet.");
                    continue;
                }
                readyPlayers.Add(session);
                HumanoidCharacterProfile profile;
                if (_prefsManager.TryGetCachedPreferences(userId, out var preferences))
                {
                    profile = (HumanoidCharacterProfile) preferences.SelectedCharacter;
                }
                else
                {
                    profile = HumanoidCharacterProfile.Random();
                }
                readyPlayerProfiles.Add(userId, profile);
            }

            DebugTools.AssertEqual(readyPlayers.Count, ReadyPlayerCount());

            // Just in case it hasn't been loaded previously we'll try loading it.
            LoadMaps();

            // map has been selected so update the lobby info text
            // applies to players who didn't ready up
            UpdateInfoText();

            StartGamePresetRules();

            RoundLengthMetric.Set(0);

            var startingEvent = new RoundStartingEvent(RoundId);
            RaiseLocalEvent(startingEvent);

            var origReadyPlayers = readyPlayers.ToArray();

            if (!StartPreset(origReadyPlayers, force))
            {
                _startingRound = false;
                return;
            }

            // MapInitialize *before* spawning players, our codebase is too shit to do it afterwards...
            _mapManager.DoMapInitialize(DefaultMap);

            SpawnPlayers(readyPlayers, readyPlayerProfiles, force);

            _roundStartDateTime = DateTime.UtcNow;
            RunLevel = GameRunLevel.InRound;

            RoundStartTimeSpan = _gameTiming.CurTime;
            SendStatusToAll();
            ReqWindowAttentionAll();
            UpdateLateJoinStatus();
            AnnounceRound();
            UpdateInfoText();
            SendRoundStartedDiscordMessage();

#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _roundStartFailCount++;

                if (RoundStartFailShutdownCount > 0 && _roundStartFailCount >= RoundStartFailShutdownCount)
                {
                    _sawmill.Fatal($"Failed to start a round {_roundStartFailCount} time(s) in a row... Shutting down!");
                    _runtimeLog.LogException(e, nameof(GameTicker));
                    _baseServer.Shutdown("Restarting server");
                    return;
                }

                _sawmill.Error($"Exception caught while trying to start the round! Restarting round...");
                _runtimeLog.LogException(e, nameof(GameTicker));
                _startingRound = false;
                RestartRound();
                return;
            }

            // Round started successfully! Reset counter...
            _roundStartFailCount = 0;
#endif
            _startingRound = false;
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
            _sawmill.Info("Ending round!");

            RunLevel = GameRunLevel.PostRound;

            ShowRoundEndScoreboard(text);
            SendRoundEndDiscordMessage();
        }

        public void ShowRoundEndScoreboard(string text = "")
        {
            // Log end of round
            _adminLogger.Add(LogType.EmergencyShuttle, LogImpact.High, $"Round ended, showing summary");

            //Tell every client the round has ended.
            var gamemodeTitle = CurrentPreset != null ? Loc.GetString(CurrentPreset.ModeTitle) : string.Empty;

            // Let things add text here.
            var textEv = new RoundEndTextAppendEvent();
            RaiseLocalEvent(textEv);

            var roundEndText = $"{text}\n{textEv.Text}";

            //Get the timespan of the round.
            var roundDuration = RoundDuration();

            //Generate a list of basic player info to display in the end round summary.
            var listOfPlayerInfo = new List<RoundEndMessageEvent.RoundEndPlayerInfo>();
            // Grab the great big book of all the Minds, we'll need them for this.
            var allMinds = EntityQueryEnumerator<MindComponent>();
            var pvsOverride = _cfg.GetCVar(CCVars.RoundEndPVSOverrides);
            while (allMinds.MoveNext(out var mindId, out var mind))
            {
                // TODO don't list redundant observer roles?
                // I.e., if a player was an observer ghost, then a hamster ghost role, maybe just list hamster and not
                // the observer role?
                var userId = mind.UserId ?? mind.OriginalOwnerUserId;

                var connected = false;
                var observer = _role.MindHasRole<ObserverRoleComponent>(mindId);
                // Continuing
                if (userId != null && _playerManager.ValidSessionId(userId.Value))
                {
                    connected = true;
                }
                ContentPlayerData? contentPlayerData = null;
                if (userId != null && _playerManager.TryGetPlayerData(userId.Value, out var playerData))
                {
                    contentPlayerData = playerData.ContentData();
                }
                // Finish

                var antag = _roles.MindIsAntagonist(mindId);

                var playerIcName = "Unknown";

                if (mind.CharacterName != null)
                    playerIcName = mind.CharacterName;
                else if (mind.CurrentEntity != null && TryName(mind.CurrentEntity.Value, out var icName))
                    playerIcName = icName;

                if (TryGetEntity(mind.OriginalOwnedEntity, out var entity) && pvsOverride)
                {
                    _pvsOverride.AddGlobalOverride(GetNetEntity(entity.Value), recursive: true);
                }

                var roles = _roles.MindGetAllRoleInfo(mindId);

                var playerEndRoundInfo = new RoundEndMessageEvent.RoundEndPlayerInfo()
                {
                    // Note that contentPlayerData?.Name sticks around after the player is disconnected.
                    // This is as opposed to ply?.Name which doesn't.
                    PlayerOOCName = contentPlayerData?.Name ?? "(IMPOSSIBLE: REGISTERED MIND WITH NO OWNER)",
                    // Character name takes precedence over current entity name
                    PlayerICName = playerIcName,
                    PlayerGuid = userId,
                    PlayerNetEntity = GetNetEntity(entity),
                    Role = antag
                        ? roles.First(role => role.Antagonist).Name
                        : roles.FirstOrDefault().Name ?? Loc.GetString("game-ticker-unknown-role"),
                    Antag = antag,
                    JobPrototypes = roles.Where(role => !role.Antagonist).Select(role => role.Prototype).ToArray(),
                    AntagPrototypes = roles.Where(role => role.Antagonist).Select(role => role.Prototype).ToArray(),
                    Observer = observer,
                    Connected = connected
                };
                listOfPlayerInfo.Add(playerEndRoundInfo);
            }

            // This ordering mechanism isn't great (no ordering of minds) but functions
            var listOfPlayerInfoFinal = listOfPlayerInfo.OrderBy(pi => pi.PlayerOOCName).ToArray();
            var sound = RoundEndSoundCollection == null ? null : _audio.GetSound(new SoundCollectionSpecifier(RoundEndSoundCollection));

            var roundEndMessageEvent = new RoundEndMessageEvent(
                gamemodeTitle,
                roundEndText,
                roundDuration,
                RoundId,
                listOfPlayerInfoFinal.Length,
                listOfPlayerInfoFinal,
                sound
            );
            RaiseNetworkEvent(roundEndMessageEvent);
            RaiseLocalEvent(roundEndMessageEvent);

            _replayRoundPlayerInfo = listOfPlayerInfoFinal;
            _replayRoundText = roundEndText;
        }

        private async void SendRoundEndDiscordMessage()
        {
            try
            {
                if (_webhookIdentifier == null)
                    return;

                var duration = RoundDuration();
                var content = Loc.GetString("discord-round-notifications-end",
                    ("id", RoundId),
                    ("hours", Math.Truncate(duration.TotalHours)),
                    ("minutes", duration.Minutes),
                    ("seconds", duration.Seconds));
                var payload = new WebhookPayload { Content = content };

                await _discord.CreateMessage(_webhookIdentifier.Value, payload);

                if (DiscordRoundEndRole == null)
                    return;

                content = Loc.GetString("discord-round-notifications-end-ping", ("roleId", DiscordRoundEndRole));
                payload = new WebhookPayload { Content = content };
                payload.AllowedMentions.AllowRoleMentions();

                await _discord.CreateMessage(_webhookIdentifier.Value, payload);
            }
            catch (Exception e)
            {
                Log.Error($"Error while sending discord round end message:\n{e}");
            }
        }

        public void RestartRound()
        {
            // If this game ticker is a dummy, do nothing!
            if (DummyTicker)
                return;

            ReplayEndRound();

            // Handle restart for server update
            if (_serverUpdates.RoundEnded())
                return;

            _sawmill.Info("Restarting round!");

            SendServerMessage(Loc.GetString("game-ticker-restart-round"));

            RoundNumberMetric.Inc();

            PlayersJoinedRoundNormally = 0;

            RunLevel = GameRunLevel.PreRoundLobby;
            RandomizeLobbyBackground();
            ResettingCleanup();
            IncrementRoundNumber();
            SendRoundStartingDiscordMessage();

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
                UpdateInfoText();

                ReqWindowAttentionAll();
            }
        }

        private async void SendRoundStartingDiscordMessage()
        {
            try
            {
                if (_webhookIdentifier == null)
                    return;

                var content = Loc.GetString("discord-round-notifications-new");

                var payload = new WebhookPayload { Content = content };

                await _discord.CreateMessage(_webhookIdentifier.Value, payload);
            }
            catch (Exception e)
            {
                Log.Error($"Error while sending discord round starting message:\n{e}");
            }
        }

        /// <summary>
        ///     Cleanup that has to run to clear up anything from the previous round.
        ///     Stuff like wiping the previous map clean.
        /// </summary>
        private void ResettingCleanup()
        {
            // Move everybody currently in the server to lobby.
            foreach (var player in _playerManager.Sessions)
            {
                PlayerJoinLobby(player);
            }

            // Round restart cleanup event, so entity systems can reset.
            var ev = new RoundRestartCleanupEvent();
            RaiseLocalEvent(ev);

            // So clients' entity systems can clean up too...
            RaiseNetworkEvent(ev);

            EntityManager.FlushEntities();

            _mapManager.Restart();

            _banManager.Restart();

            _gameMapManager.ClearSelectedMap();

            // Clear up any game rules.
            ClearGameRules();
            CurrentPreset = null;

            _allPreviousGameRules.Clear();

            DisallowLateJoin = false;
            _playerGameStatuses.Clear();
            foreach (var session in _playerManager.Sessions)
            {
                _playerGameStatuses[session.UserId] = LobbyEnabled ? PlayerGameStatus.NotReadyToPlay : PlayerGameStatus.ReadyToPlay;
            }
        }

        public bool DelayStart(TimeSpan time)
        {
            if (_runLevel != GameRunLevel.PreRoundLobby)
            {
                return false;
            }

            _roundStartTime += time;

            RaiseNetworkEvent(new TickerLobbyCountdownEvent(_roundStartTime, Paused));

            _chatManager.DispatchServerAnnouncement(Loc.GetString("game-ticker-delay-start", ("seconds", time.TotalSeconds)));

            return true;
        }

        private void UpdateRoundFlow(float frameTime)
        {
            if (RunLevel == GameRunLevel.InRound)
            {
                RoundLengthMetric.Inc(frameTime);
            }

            if (_roundStartTime == TimeSpan.Zero ||
                RunLevel != GameRunLevel.PreRoundLobby ||
                Paused ||
                _roundStartTime - RoundPreloadTime > _gameTiming.CurTime ||
                _roundStartCountdownHasNotStartedYetDueToNoPlayers)
            {
                return;
            }

            if (_roundStartTime < _gameTiming.CurTime)
            {
                StartRound();
            }
            // Preload maps so we can start faster
            else if (_roundStartTime - RoundPreloadTime < _gameTiming.CurTime)
            {
                LoadMaps();
            }
        }

        private void AnnounceRound()
        {
            if (CurrentPreset == null) return;

            var options = _prototypeManager.EnumeratePrototypes<RoundAnnouncementPrototype>().ToList();

            if (options.Count == 0)
                return;

            var proto = _robustRandom.Pick(options);

            if (proto.Message != null)
                _chatSystem.DispatchGlobalAnnouncement(Loc.GetString(proto.Message), playSound: true);

            if (proto.Sound != null)
                _audio.PlayGlobal(proto.Sound, Filter.Broadcast(), true);
        }

        private async void SendRoundStartedDiscordMessage()
        {
            try
            {
                if (_webhookIdentifier == null)
                    return;

                var mapName = _gameMapManager.GetSelectedMap()?.MapName ?? Loc.GetString("discord-round-notifications-unknown-map");
                var content = Loc.GetString("discord-round-notifications-started", ("id", RoundId), ("map", mapName));

                var payload = new WebhookPayload { Content = content };

                await _discord.CreateMessage(_webhookIdentifier.Value, payload);
            }
            catch (Exception e)
            {
                Log.Error($"Error while sending discord round start message:\n{e}");
            }
        }
    }

    public enum GameRunLevel
    {
        PreRoundLobby = 0,
        InRound = 1,
        PostRound = 2
    }

    public sealed class GameRunLevelChangedEvent
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
    [PublicAPI]
    public sealed class LoadingMapsEvent : EntityEventArgs
    {
        public List<GameMapPrototype> Maps;

        public LoadingMapsEvent(List<GameMapPrototype> maps)
        {
            Maps = maps;
        }
    }

    /// <summary>
    ///     Event raised before the game loads a given map.
    ///     This event is mutable, and load options should be tweaked if necessary.
    /// </summary>
    /// <remarks>
    ///     You likely want to subscribe to this after StationSystem.
    /// </remarks>
    [PublicAPI]
    public sealed class PreGameMapLoad : EntityEventArgs
    {
        public readonly MapId Map;
        public GameMapPrototype GameMap;
        public MapLoadOptions Options;

        public PreGameMapLoad(MapId map, GameMapPrototype gameMap, MapLoadOptions options)
        {
            Map = map;
            GameMap = gameMap;
            Options = options;
        }
    }


    /// <summary>
    ///     Event raised after the game loads a given map.
    /// </summary>
    /// <remarks>
    ///     You likely want to subscribe to this after StationSystem.
    /// </remarks>
    [PublicAPI]
    public sealed class PostGameMapLoad : EntityEventArgs
    {
        public readonly GameMapPrototype GameMap;
        public readonly MapId Map;
        public readonly IReadOnlyList<EntityUid> Grids;
        public readonly string? StationName;

        public PostGameMapLoad(GameMapPrototype gameMap, MapId map, IReadOnlyList<EntityUid> grids, string? stationName)
        {
            GameMap = gameMap;
            Map = map;
            Grids = grids;
            StationName = stationName;
        }
    }

    /// <summary>
    ///     Event raised to refresh the late join status.
    ///     If you want to disallow late joins, listen to this and call Disallow.
    /// </summary>
    public sealed class RefreshLateJoinAllowedEvent
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
    public sealed class RoundStartAttemptEvent : CancellableEntityEventArgs
    {
        public ICommonSession[] Players { get; }
        public bool Forced { get; }

        public RoundStartAttemptEvent(ICommonSession[] players, bool forced)
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
    public sealed class RulePlayerSpawningEvent
    {
        /// <summary>
        ///     Pool of players to be spawned.
        ///     If you want to handle a specific player being spawned, remove it from this list and do what you need.
        /// </summary>
        /// <remarks>If you spawn a player by yourself from this event, don't forget to call <see cref="GameTicker.PlayerJoinGame"/> on them.</remarks>
        public List<ICommonSession> PlayerPool { get; }
        public IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> Profiles { get; }
        public bool Forced { get; }

        public RulePlayerSpawningEvent(List<ICommonSession> playerPool, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles, bool forced)
        {
            PlayerPool = playerPool;
            Profiles = profiles;
            Forced = forced;
        }
    }

    /// <summary>
    ///     Event raised after players were assigned jobs by the GameTicker and have been spawned in.
    ///     You can give on-station people special roles by listening to this event.
    /// </summary>
    public sealed class RulePlayerJobsAssignedEvent
    {
        public ICommonSession[] Players { get; }
        public IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> Profiles { get; }
        public bool Forced { get; }

        public RulePlayerJobsAssignedEvent(ICommonSession[] players, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles, bool forced)
        {
            Players = players;
            Profiles = profiles;
            Forced = forced;
        }
    }

    /// <summary>
    ///     Event raised to allow subscribers to add text to the round end summary screen.
    /// </summary>
    public sealed class RoundEndTextAppendEvent
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
