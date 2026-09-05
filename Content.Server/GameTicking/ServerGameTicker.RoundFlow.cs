using System.Linq;
using System.Numerics;
using Content.Server.Announcements;
using Content.Server.Discord;
using Content.Server.GameTicking.Events;
using Content.Server.Roles;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Events;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Preferences;
using Content.Shared.Roles.Components;
using JetBrains.Annotations;
using Prometheus;
using Robust.Shared.Asynchronous;
using Robust.Shared.Audio;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking;

public sealed partial class ServerGameTicker
{
    [Dependency] private DiscordWebhook _discord = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private ITaskManager _taskManager = default!;

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

    private RoundEndMessageEvent.RoundEndPlayerInfo[]? _replayRoundPlayerInfo;

    private string? _replayRoundText;

    /// <summary>
    /// Returns true if the round's map is eligible to be updated.
    /// </summary>
    /// <returns></returns>
    public bool CanUpdateMap()
    {
        return RunLevel == GameRunLevel.PreRoundLobby &&
               _roundStartTime - RoundPreloadTime > Timing.CurTime;
    }

    /// <summary>
    ///     Loads all the maps for the given round.
    /// </summary>
    /// <remarks>
    ///     Must be called before the runlevel is set to InRound.
    /// </remarks>
    private void LoadMaps()
    {
        if (Map.MapExists(DefaultMap))
            return;

        AddGamePresetRules();

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
            ProtoMan.TryIndex(CurrentPreset.MapPool, out var pool) &&
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

        if (maps.Count == 0)
        {
            Map.CreateMap(out var mapId, runMapInit: false);
            DefaultMap = mapId;
            return;
        }

        for (var i = 0; i < maps.Count; i++)
        {
            LoadGameMap(maps[i], out var mapId);
            DebugTools.Assert(!Map.IsInitialized(mapId));

            if (i == 0)
                DefaultMap = mapId;
        }
    }

    public PreGameMapLoad RaisePreLoad(
        GameMapPrototype proto,
        DeserializationOptions? opts = null,
        Vector2? offset = null,
        Angle? rot = null)
    {
        offset ??= proto.MaxRandomOffset != 0f
            ? Random.NextVector2(proto.MaxRandomOffset)
            : Vector2.Zero;

        rot ??= proto.RandomRotation
            ? Random.NextAngle()
            : Angle.Zero;

        opts ??= DeserializationOptions.Default;
        var ev = new PreGameMapLoad(proto, opts.Value, offset.Value, rot.Value);
        RaiseLocalEvent(ev);
        return ev;
    }

    public override IReadOnlyList<EntityUid> LoadGameMap(
        GameMapPrototype proto,
        out MapId mapId,
        DeserializationOptions? options = null,
        string? stationName = null,
        Vector2? offset = null,
        Angle? rot = null)
    {
        var ev = RaisePreLoad(proto, options, offset, rot);

        if (ev.GameMap.IsGrid)
        {
            var mapUid = Map.CreateMap(out mapId, runMapInit: options?.InitializeMaps ?? false);
            if (!_loader.TryLoadGrid(mapId,
                    ev.GameMap.MapPath,
                    out var grid,
                    ev.Options,
                    ev.Offset,
                    ev.Rotation))
            {
                throw new Exception($"Failed to load game-map grid {ev.GameMap.ID}");
            }

            Meta.SetEntityName(mapUid, proto.MapName);
            var g = new List<EntityUid> { grid.Value.Owner };
            RaiseLocalEvent(new PostGameMapLoad(proto, mapId, g, stationName));
            return g;
        }

        if (!_loader.TryLoadMap(ev.GameMap.MapPath,
                out var map,
                out var grids,
                ev.Options,
                ev.Offset,
                ev.Rotation))
        {
            throw new Exception($"Failed to load game map {ev.GameMap.ID}");
        }

        mapId = map.Value.Comp.MapId;
        Meta.SetEntityName(map.Value.Owner, proto.MapName);
        var gridUids = grids.Select(x => x.Owner).ToList();
        RaiseLocalEvent(new PostGameMapLoad(proto, mapId, gridUids, stationName));
        return gridUids;
    }

    /// <summary>
    /// Variant of <see cref="LoadGameMap"/> that attempts to assign the provided <see cref="MapId"/> to the
    /// loaded map.
    /// </summary>
    public IReadOnlyList<EntityUid> LoadGameMapWithId(
        GameMapPrototype proto,
        MapId mapId,
        DeserializationOptions? opts = null,
        string? stationName = null,
        Vector2? offset = null,
        Angle? rot = null)
    {
        var ev = RaisePreLoad(proto, opts, offset, rot);

        if (ev.GameMap.IsGrid)
        {
            var mapUid = Map.CreateMap(mapId);
            if (!_loader.TryLoadGrid(mapId,
                    ev.GameMap.MapPath,
                    out var grid,
                    ev.Options,
                    ev.Offset,
                    ev.Rotation))
            {
                throw new Exception($"Failed to load game-map grid {ev.GameMap.ID}");
            }

            Meta.SetEntityName(mapUid, proto.MapName);
            var g = new List<EntityUid> { grid.Value.Owner };
            RaiseLocalEvent(new PostGameMapLoad(proto, mapId, g, stationName));
            return g;
        }

        if (!_loader.TryLoadMapWithId(
                mapId,
                ev.GameMap.MapPath,
                out var map,
                out var grids,
                ev.Options,
                ev.Offset,
                ev.Rotation))
        {
            throw new Exception($"Failed to load map");
        }

        Meta.SetEntityName(map.Value.Owner, proto.MapName);
        var gridUids = grids.Select(x => x.Owner).ToList();
        RaiseLocalEvent(new PostGameMapLoad(proto, mapId, gridUids, stationName));
        return gridUids;
    }

    /// <summary>
    /// Variant of <see cref="LoadGameMap"/> that loads and then merges a game map onto an existing map.
    /// </summary>
    public IReadOnlyList<EntityUid> MergeGameMap(
        GameMapPrototype proto,
        MapId targetMap,
        DeserializationOptions? opts = null,
        string? stationName = null,
        Vector2? offset = null,
        Angle? rot = null)
    {
        // TODO MAP LOADING use a new event?
        // This is quite different from the other methods, which will actually create a **new** map.
        var ev = RaisePreLoad(proto, opts, offset, rot);

        if (ev.GameMap.IsGrid)
        {
            if (!_loader.TryLoadGrid(targetMap,
                    ev.GameMap.MapPath,
                    out var grid,
                    ev.Options,
                    ev.Offset,
                    ev.Rotation))
            {
                throw new Exception($"Failed to load game-map grid {ev.GameMap.ID}");
            }

            var g = new List<EntityUid> { grid.Value.Owner };
            // TODO MAP LOADING use a new event?
            RaiseLocalEvent(new PostGameMapLoad(proto, targetMap, g, stationName));
            return g;
        }

        if (!_loader.TryMergeMap(targetMap,
                ev.GameMap.MapPath,
                out var grids,
                ev.Options,
                ev.Offset,
                ev.Rotation))
        {
            throw new Exception($"Failed to load map");
        }

        var gridUids = grids.Select(x => x.Owner).ToList();

        // TODO MAP LOADING use a new event?
        RaiseLocalEvent(new PostGameMapLoad(proto, targetMap, gridUids, stationName));
        return gridUids;
    }

    public override int ReadyPlayerCount()
    {
        var total = 0;
        foreach (var (userId, status) in _playerGameStatuses)
        {
            if (LobbyEnabled && status == PlayerGameStatus.NotReadyToPlay)
                continue;

            if (!_playerManager.TryGetSessionById(userId, out _))
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
        Log.Info("Starting round!");

        SendServerMessage(Loc.GetString("game-ticker-start-round"));

        var readyPlayers = new List<ICommonSession>();
        var readyPlayerProfiles = new Dictionary<NetUserId, HumanoidCharacterProfile>();
        var autoDeAdmin = Cfg.GetCVar(CCVars.AdminDeadminOnJoin);
        foreach (var (userId, status) in _playerGameStatuses)
        {
            if (LobbyEnabled && status != PlayerGameStatus.ReadyToPlay)
                continue;

            if (!_playerManager.TryGetSessionById(userId, out var session))
                continue;

            if (autoDeAdmin && _adminManager.IsAdmin(session))
            {
                _adminManager.DeAdmin(session);
            }
#if DEBUG
            DebugTools.Assert(_userDb.IsLoadComplete(session), $"Player was readied up but didn't have user DB data loaded yet??");
#endif

            readyPlayers.Add(session);
            HumanoidCharacterProfile profile;
            if (_prefsManager.TryGetCachedPreferences(userId, out var preferences))
            {
                profile = preferences.SelectedCharacter;
            }
            else
            {
                var speciesToBlacklist =
                    new HashSet<string>(Cfg.GetCVar(CCVars.ICNewAccountSpeciesBlacklist).Split(","));
                profile = HumanoidCharacterProfile.Random(speciesToBlacklist);
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
        Map.InitializeMap(DefaultMap);

        SpawnPlayers(readyPlayers, readyPlayerProfiles, force);

        _roundStartDateTime = DateTime.UtcNow;
        RunLevel = GameRunLevel.InRound;

        RoundStartTimeSpan = Timing.CurTime;
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
                    Log.Fatal($"Failed to start a round {_roundStartFailCount} time(s) in a row... Shutting down!");
                    _runtimeLog.LogException(e, nameof(GameTicker));
                    _baseServer.Shutdown("Restarting server");
                    return;
                }

                Log.Error($"Exception caught while trying to start the round! Restarting round...");
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

    public override void EndRound(string text = "")
    {
        // If this game ticker is a dummy, do nothing!
        if (DummyTicker)
            return;

        DebugTools.Assert(RunLevel == GameRunLevel.InRound);
        Log.Info("Ending round!");

        RunLevel = GameRunLevel.PostRound;

        try
        {
            ShowRoundEndScoreboard(text);
        }
        catch (Exception e)
        {
            Log.Error($"Error while showing round end scoreboard: {e}");
        }

        try
        {
            SendRoundEndDiscordMessage();
        }
        catch (Exception e)
        {
            Log.Error($"Error while sending round end Discord message: {e}");
        }
    }

    public void ShowRoundEndScoreboard(string text = "")
    {
        // Log end of round
        Admin.Add(LogType.EmergencyShuttle, LogImpact.High, $"Round ended, showing summary");

        //Tell every client the round has ended.
        var gamemodeTitle = CurrentPreset != null ? Loc.GetString(CurrentPreset.ModeTitle) : string.Empty;

        // Let things add text here.
        var textEv = new RoundEndTextAppendEvent();
        RaiseLocalEvent(ref textEv);

        var roundEndText = $"{text}\n{textEv.Text}";

        //Get the timespan of the round.
        var roundDuration = RoundDuration();

        //Generate a list of basic player info to display in the end round summary.
        var listOfPlayerInfo = new List<RoundEndMessageEvent.RoundEndPlayerInfo>();
        // Grab the great big book of all the Minds, we'll need them for this.
        var allMinds = EntityQueryEnumerator<MindComponent>();
        var pvsOverride = Cfg.GetCVar(CCVars.RoundEndPVSOverrides);
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

            var antag = Role.MindIsAntagonist(mindId);

            var playerIcName = "Unknown";

            if (mind.CharacterName != null)
                playerIcName = mind.CharacterName;
            else if (mind.CurrentEntity != null && TryName(mind.CurrentEntity.Value, out var icName))
                playerIcName = icName;

            if (TryGetEntity(mind.OriginalOwnedEntity, out var entity) && pvsOverride)
            {
                _pvsOverride.AddGlobalOverride(entity.Value);
            }

            var roles = Role.MindGetAllRoleInfo(mindId);

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
        var sound = RoundEndSoundCollection == null ? null : Audio.ResolveSound(new SoundCollectionSpecifier(RoundEndSoundCollection));

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

    public override void RestartRound()
    {
        // If this game ticker is a dummy, do nothing!
        if (DummyTicker)
            return;

        ReplayEndRound();

        // Handle restart for server update
        if (_serverUpdates.RoundEnded())
            return;

        // Check if the GamePreset needs to be reset
        TryResetPreset();

        Log.Info("Restarting round!");

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
                _roundStartTime = Timing.CurTime + LobbyDuration;

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

        _banManager.Restart();

        _gameMapManager.ClearSelectedMap();

        // Clear up any game rules.
        ClearGameRules();
        CurrentPreset = null;

        AllRoundGameRules.Clear();

        DisallowLateJoin = false;
        _playerGameStatuses.Clear();
        foreach (var session in _playerManager.Sessions)
        {
            _playerGameStatuses[session.UserId] = LobbyEnabled ? PlayerGameStatus.NotReadyToPlay : PlayerGameStatus.ReadyToPlay;
        }
    }

    public bool DelayStart(TimeSpan time)
    {
        if (RunLevel != GameRunLevel.PreRoundLobby)
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
            _roundStartTime - RoundPreloadTime > Timing.CurTime ||
            _roundStartCountdownHasNotStartedYetDueToNoPlayers)
        {
            return;
        }

        if (_roundStartTime < Timing.CurTime)
        {
            StartRound();
        }
        // Preload maps so we can start faster
        else if (_roundStartTime - RoundPreloadTime < Timing.CurTime)
        {
            LoadMaps();
        }
    }

    private void AnnounceRound()
    {
        if (CurrentPreset == null)
            return;

        var options = ProtoMan.EnumeratePrototypes<RoundAnnouncementPrototype>().ToList();

        if (options.Count == 0)
            return;

        var proto = Random.Pick(options);

        if (proto.Message != null)
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString(proto.Message), playSound: true);

        if (proto.Sound != null)
            Audio.PlayGlobal(proto.Sound, Filter.Broadcast(), true);
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
public sealed class PreGameMapLoad(GameMapPrototype gameMap, DeserializationOptions options, Vector2 offset, Angle rotation) : EntityEventArgs
{
    public readonly GameMapPrototype GameMap = gameMap;
    public DeserializationOptions Options = options;
    public Vector2 Offset = offset;
    public Angle Rotation = rotation;
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
    public bool DisallowLateJoin { get; private set; }

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
