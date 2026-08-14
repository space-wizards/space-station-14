using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.SpaceArena.Components;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Chasm;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Systems;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Content.Shared.SpaceArena;
using Content.Shared.SpaceArena.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Collections;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SpaceArena;

public sealed partial class SpaceArenaMatchSystem : EntitySystem
{
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private StationSpawningSystem _stationSpawning = default!;
    [Dependency] private StationSystem _station = default!;

    private static readonly ProtoId<StartingGearPrototype> HubStartingGear = "SpaceArenaHubChameleonGear";
    private static readonly TimeSpan RespawnRetryDelay = TimeSpan.FromSeconds(5);

    private readonly Dictionary<NetUserId, EntityUid> _playerMatches = [];
    private readonly Dictionary<NetUserId, EntityUid> _spectatorMatches = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceArenaMatchRuntimeComponent, ComponentShutdown>(OnMatchShutdown);
        SubscribeLocalEvent<SpaceArenaMatchMemberComponent, ComponentShutdown>(OnMatchMemberShutdown);
        SubscribeLocalEvent<SpaceArenaMatchMemberComponent, GhostAttemptEvent>(OnGhostAttempt);
        SubscribeLocalEvent<SpaceArenaMatchMemberComponent, StartedFallingIntoChasmEvent>(OnStartedFallingIntoChasm);
        SubscribeLocalEvent<SpaceArenaMatchMemberComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SpaceArenaSpectatorComponent, ComponentShutdown>(OnSpectatorShutdown);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeNetworkEvent<SpaceArenaLeaveMatchRequest>(OnLeaveMatchRequest);
        SubscribeNetworkEvent<SpaceArenaLeaveSpectatingRequest>(OnLeaveSpectatingRequest);
        _players.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public bool TryCreateMatch(
        EntProtoId mode,
        ProtoId<GameMapPrototype> arenaId,
        out EntityUid match)
    {
        match = EntityUid.Invalid;

        if (!ProtoMan.TryIndex(mode, out EntityPrototype? modePrototype) ||
            !modePrototype.HasComp<SpaceArenaMatchComponent>(Factory) ||
            !ProtoMan.TryIndex(arenaId, out var arena) ||
            arena.SpaceArena is not { } arenaData ||
            !arenaData.Modes.Contains(mode))
        {
            return false;
        }

        var uid = Spawn(mode, MapCoordinates.Nullspace);
        var component = Comp<SpaceArenaMatchComponent>(uid);
        EnsureComp<SpaceArenaMatchRuntimeComponent>(uid);

        var arenaMinPlayers = (int) Math.Min(arena.MinPlayers, int.MaxValue);
        var arenaMaxPlayers = (int) Math.Min(arena.MaxPlayers, int.MaxValue);
        component.MinPlayers = Math.Max(component.MinPlayers, arenaMinPlayers);
        component.MaxPlayers = Math.Min(component.MaxPlayers, arenaMaxPlayers);
        if (arenaData.CountdownDuration is { } countdownDuration)
            component.CountdownDuration = countdownDuration;

        if (component.MinPlayers <= 0 || component.MaxPlayers < component.MinPlayers)
        {
            QueueDel(uid);
            return false;
        }

        component.Arena = arenaId;
        component.State = SpaceArenaMatchState.Waiting;
        component.StateEndsAt = null;
        component.PlayerCount = 0;

        match = uid;
        return true;
    }

    public bool TryJoinMatch(EntityUid match, ICommonSession player)
    {
        if (!TryGetMatchContext(match, out var component, out var runtime) ||
            component.State != SpaceArenaMatchState.Waiting ||
            runtime.Players.Count >= component.MaxPlayers ||
            _playerMatches.ContainsKey(player.UserId) ||
            _spectatorMatches.ContainsKey(player.UserId) ||
            player.AttachedEntity is not { Valid: true } lobbyEntity ||
            TerminatingOrDeleted(lobbyEntity) ||
            EntityManager.IsQueuedForDeletion(lobbyEntity) ||
            !_mind.TryGetMind(player.UserId, out var mindUid, out _))
        {
            return false;
        }

        runtime.Players.Add(player.UserId, new SpaceArenaMatchPlayerData
        {
            Mind = mindUid.Value,
            LobbyEntity = lobbyEntity,
            LobbyStation = _station.GetOwningStation(lobbyEntity),
        });
        _playerMatches.Add(player.UserId, match);

        component.PlayerCount = runtime.Players.Count;
        SetPlayerState(mindUid.Value, SpaceArenaPlayerState.MatchLobby);

        var ev = new SpaceArenaMatchPlayerJoinedEvent(player.UserId, mindUid.Value);
        RaiseLocalEvent(match, ref ev);
        return true;
    }

    public bool TryLeaveMatch(ICommonSession player)
    {
        if (!_playerMatches.TryGetValue(player.UserId, out var match) ||
            !TryComp(match, out SpaceArenaMatchComponent? component) ||
            component.State != SpaceArenaMatchState.Waiting && !component.AllowVoluntaryLeave)
        {
            return false;
        }

        return RemovePlayer(match, player.UserId, returnToLobby: true);
    }

    public bool TrySpectateMatch(EntityUid match, ICommonSession player)
    {
        if (!TryGetMatchContext(match, out var component, out var runtime) ||
            !CanSpectate(component.State) ||
            runtime.Map == MapId.Nullspace ||
            _playerMatches.ContainsKey(player.UserId) ||
            _spectatorMatches.ContainsKey(player.UserId) ||
            player.AttachedEntity is not { Valid: true } lobbyEntity ||
            TerminatingOrDeleted(lobbyEntity) ||
            EntityManager.IsQueuedForDeletion(lobbyEntity) ||
            !_mind.TryGetMind(player.UserId, out var mindUid, out var mind) ||
            !TryGetSpectatorCoordinates(runtime, out var coordinates))
        {
            return false;
        }

        var ghost = _ghost.SpawnGhost((mindUid.Value, mind), coordinates, canReturn: false);
        if (ghost is not { } spectatorEntity)
            return false;

        var spectator = EnsureComp<SpaceArenaSpectatorComponent>(spectatorEntity);
        spectator.Match = match;
        spectator.Player = player.UserId;

        runtime.Spectators.Add(player.UserId, new SpaceArenaMatchSpectatorData
        {
            Mind = mindUid.Value,
            LobbyStation = _station.GetOwningStation(lobbyEntity),
            SpectatorEntity = spectatorEntity,
        });
        _spectatorMatches.Add(player.UserId, match);
        SetPlayerState(mindUid.Value, SpaceArenaPlayerState.Spectator);

        if (lobbyEntity != spectatorEntity && Exists(lobbyEntity))
            QueueDel(lobbyEntity);

        return true;
    }

    public bool TryLeaveSpectating(ICommonSession player)
    {
        return _spectatorMatches.TryGetValue(player.UserId, out var match) &&
               RemoveSpectator(match, player.UserId, returnToLobby: true);
    }

    public bool TryStartMatch(EntityUid match)
    {
        if (!TryGetMatchContext(match, out var component, out var runtime) ||
            component.State != SpaceArenaMatchState.Waiting)
        {
            return false;
        }

        RemoveUnavailablePlayers(match, component, runtime);
        if (runtime.Players.Count < component.MinPlayers || !TryLoadArena(match, component, runtime))
            return false;

        SetMatchState(match, component, runtime, SpaceArenaMatchState.Preparing, component.PreparationDuration);
        SpawnWaitingPlayers(match, component, runtime);

        if (runtime.Players.Count >= component.MinPlayers)
            return true;

        ReturnAllPlayers(runtime, SpaceArenaPlayerState.MatchLobby, removeMembership: false);
        DeleteArena(runtime);
        if (runtime.Players.Count == 0)
        {
            QueueDel(match);
            return false;
        }

        SetMatchState(match, component, runtime, SpaceArenaMatchState.Waiting, null);
        return false;
    }

    public bool FinishMatch(EntityUid match)
    {
        if (!TryGetMatchContext(match, out var component, out var runtime) ||
            component.State is SpaceArenaMatchState.Ending or
                SpaceArenaMatchState.Finished or
                SpaceArenaMatchState.Cleanup or
                SpaceArenaMatchState.Waiting)
        {
            return false;
        }

        runtime.Respawns.Clear();
        runtime.DisconnectForfeits.Clear();
        runtime.NextRespawn = null;
        runtime.NextDisconnectForfeit = null;
        SetMatchState(match, component, runtime, SpaceArenaMatchState.Ending, component.EndingDuration);
        ReturnAllSpectators(runtime);
        return true;
    }

    public bool ContainsPlayer(EntityUid match, NetUserId player)
    {
        return TryGetMatchContext(match, out _, out var runtime) && runtime.Players.ContainsKey(player);
    }

    public bool IsMatchActive(EntityUid match)
    {
        return TryGetMatchContext(match, out var component, out _) &&
               component.State == SpaceArenaMatchState.Active;
    }

    public bool TryGetPlayerMatch(NetUserId player, out EntityUid match)
    {
        return _playerMatches.TryGetValue(player, out match);
    }

    public bool TryGetSpectatedMatch(NetUserId player, out EntityUid match)
    {
        return _spectatorMatches.TryGetValue(player, out match);
    }

    public bool TryGetFirstPlayer(EntityUid match, out NetUserId player)
    {
        player = default;
        if (!TryGetMatchContext(match, out _, out var runtime))
            return false;

        foreach (var playerId in runtime.Players.Keys)
        {
            player = playerId;
            return true;
        }

        return false;
    }

    public List<EntityUid> GetMatches()
    {
        List<EntityUid> matches = [];
        var query = EntityQueryEnumerator<SpaceArenaMatchComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid))
                continue;

            matches.Add(uid);
        }

        return matches;
    }

    private bool TryGetMatchContext(
        EntityUid match,
        [NotNullWhen(true)] out SpaceArenaMatchComponent? component,
        [NotNullWhen(true)] out SpaceArenaMatchRuntimeComponent? runtime)
    {
        component = null;
        runtime = null;
        return !TerminatingOrDeleted(match) &&
               !EntityManager.IsQueuedForDeletion(match) &&
               TryComp(match, out component) &&
               TryComp(match, out runtime);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<SpaceArenaMatchComponent, SpaceArenaMatchRuntimeComponent>();
        while (query.MoveNext(out var uid, out var component, out var runtime))
        {
            if (component.State is (SpaceArenaMatchState.Preparing or
                SpaceArenaMatchState.Countdown or
                SpaceArenaMatchState.Active) &&
                (runtime.Map == MapId.Nullspace || !_map.MapExists(runtime.Map)))
            {
                FinishMatch(uid);
                continue;
            }

            if (component.State == SpaceArenaMatchState.Active)
            {
                ProcessDisconnectForfeits(uid, component, runtime, curTime);
                if (component.State == SpaceArenaMatchState.Active)
                    ProcessRespawns(uid, component, runtime, curTime);
            }

            if (component.StateEndsAt is not { } stateEndsAt || curTime < stateEndsAt)
                continue;

            switch (component.State)
            {
                case SpaceArenaMatchState.Preparing:
                    SetMatchState(uid, component, runtime, SpaceArenaMatchState.Countdown, component.CountdownDuration);
                    break;
                case SpaceArenaMatchState.Countdown:
                    SetMatchState(uid, component, runtime, SpaceArenaMatchState.Active, component.TimeLimit);
                    break;
                case SpaceArenaMatchState.Active:
                    FinishMatch(uid);
                    break;
                case SpaceArenaMatchState.Ending:
                    SetMatchState(uid, component, runtime, SpaceArenaMatchState.Finished, component.ResultsDuration);
                    break;
                case SpaceArenaMatchState.Finished:
                    SetMatchState(uid, component, runtime, SpaceArenaMatchState.Cleanup, null);
                    CleanupMatch(uid, component, runtime);
                    break;
            }
        }
    }

    private bool TryLoadArena(
        EntityUid match,
        SpaceArenaMatchComponent component,
        SpaceArenaMatchRuntimeComponent runtime)
    {
        if (component.Arena is not { } arenaId || !ProtoMan.TryIndex(arenaId, out var arena))
            return false;

        try
        {
            var options = DeserializationOptions.Default with { InitializeMaps = true };
            _gameTicker.LoadGameMap(arena, out runtime.Map, options, $"{arena.MapName} {GetNetEntity(match)}");
        }
        catch (Exception exception)
        {
            Log.Error($"Failed to load SpaceArena map {arenaId} for {ToPrettyString(match)}: {exception}");
            DeleteArena(runtime);
            return false;
        }

        runtime.Station = _station.GetStationInMap(runtime.Map);
        CacheSpawnPoints(runtime);
        if (TryAssignSpawnGroups(component, runtime, arena.SpaceArena!))
            return true;

        Log.Error(
            $"SpaceArena map {arenaId} does not have enough configured spawn points for {ToPrettyString(match)}.");
        DeleteArena(runtime);
        return false;
    }

    private void SpawnWaitingPlayers(
        EntityUid match,
        SpaceArenaMatchComponent component,
        SpaceArenaMatchRuntimeComponent runtime)
    {
        var failed = new ValueList<NetUserId>(runtime.Players.Count);
        foreach (var (player, data) in runtime.Players)
        {
            if (!TrySpawnMatchPlayer(match, component, runtime, player, data))
                failed.Add(player);
        }

        foreach (var player in failed)
            RemovePlayer(match, player, returnToLobby: false, finishIfBelowMinimum: false);
    }

    private bool TrySpawnMatchPlayer(
        EntityUid match,
        SpaceArenaMatchComponent component,
        SpaceArenaMatchRuntimeComponent runtime,
        NetUserId playerId,
        SpaceArenaMatchPlayerData data)
    {
        if (!_players.TryGetSessionById(playerId, out var session) ||
            !_mind.TryGetMind(playerId, out var mindUid, out _) ||
            !TryGetSpawnCoordinates(runtime, data.SpawnGroup, out var coordinates) ||
            !coordinates.IsValid(EntityManager))
        {
            return false;
        }

        var profile = _gameTicker.GetPlayerProfile(session);
        var mob = _stationSpawning.SpawnPlayerMob(coordinates, null, profile, runtime.Station);
        if (component.StartingGear is { } gear)
            _stationSpawning.EquipStartingGear(mob, gear);

        var member = EnsureComp<SpaceArenaMatchMemberComponent>(mob);
        member.Match = match;
        member.Player = playerId;
        if (component.AllowVoluntaryLeave)
            EnsureComp<SpaceArenaVoluntaryLeaveComponent>(mob);

        var lobbyBody = data.LobbyEntity;
        _mind.TransferTo(mindUid.Value, mob);
        data.MatchEntity = mob;
        data.LobbyEntity = null;
        if (lobbyBody is { } oldLobbyBody && oldLobbyBody != mob && Exists(oldLobbyBody))
            QueueDel(oldLobbyBody);

        SetPlayerState(data.Mind, PlayerStateForMatch(component.State));

        var ev = new SpaceArenaMatchPlayerSpawnedEvent(playerId, data.Mind, mob, data.SpawnGroup);
        RaiseLocalEvent(match, ref ev);
        return true;
    }

    private void CacheSpawnPoints(SpaceArenaMatchRuntimeComponent runtime)
    {
        runtime.SpawnPoints.Clear();
        runtime.NextSpawnPoints.Clear();

        var points = EntityQueryEnumerator<SpaceArenaSpawnPointComponent, TransformComponent>();
        while (points.MoveNext(out _, out var point, out var xform))
        {
            if (xform.MapID != runtime.Map)
                continue;

            runtime.SpawnPoints.GetOrNew(point.Group).Add(xform.Coordinates);
        }

        if (runtime.SpawnPoints.Count != 0)
        {
            CacheBarriers(runtime);
            return;
        }

        var legacyPoints = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (legacyPoints.MoveNext(out _, out var point, out var xform))
        {
            if (xform.MapID != runtime.Map || point.SpawnType != SpawnPointType.LateJoin)
                continue;

            runtime.SpawnPoints.GetOrNew(SpaceArenaSpawnGroups.Player).Add(xform.Coordinates);
        }

        CacheBarriers(runtime);
    }

    private void CacheBarriers(SpaceArenaMatchRuntimeComponent runtime)
    {
        runtime.Barriers.Clear();
        var barriers = EntityQueryEnumerator<SpaceArenaBarrierComponent, TransformComponent>();
        while (barriers.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID == runtime.Map)
                runtime.Barriers.Add(uid);
        }
    }

    private bool TryAssignSpawnGroups(
        SpaceArenaMatchComponent component,
        SpaceArenaMatchRuntimeComponent runtime,
        SpaceArenaMapData arena)
    {
        if (arena.SpawnGroups.Count == 0)
            return false;

        var assigned = new Dictionary<string, int>(arena.SpawnGroups.Count);
        var nextGroup = 0;
        foreach (var data in runtime.Players.Values)
        {
            var found = false;
            for (var attempt = 0; attempt < arena.SpawnGroups.Count; attempt++)
            {
                var group = arena.SpawnGroups[nextGroup];
                nextGroup = (nextGroup + 1) % arena.SpawnGroups.Count;

                if (!runtime.SpawnPoints.TryGetValue(group, out var points) ||
                    points.Count == 0 ||
                    assigned.GetValueOrDefault(group) >= points.Count)
                {
                    continue;
                }

                data.SpawnGroup = group;
                assigned[group] = assigned.GetValueOrDefault(group) + 1;
                found = true;
                break;
            }

            if (!found)
                return false;
        }

        return runtime.Players.Count >= component.MinPlayers;
    }

    private static bool TryGetSpawnCoordinates(
        SpaceArenaMatchRuntimeComponent runtime,
        string group,
        out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;
        if (!runtime.SpawnPoints.TryGetValue(group, out var points) || points.Count == 0)
            return false;

        var index = runtime.NextSpawnPoints.GetValueOrDefault(group) % points.Count;
        runtime.NextSpawnPoints[group] = (index + 1) % points.Count;
        coordinates = points[index];
        return true;
    }

    private static bool CanSpectate(SpaceArenaMatchState state)
    {
        return state is SpaceArenaMatchState.Preparing or
            SpaceArenaMatchState.Countdown or
            SpaceArenaMatchState.Active;
    }

    private bool TryGetSpectatorCoordinates(
        SpaceArenaMatchRuntimeComponent runtime,
        out EntityCoordinates coordinates)
    {
        foreach (var data in runtime.Players.Values)
        {
            if (data.MatchEntity is not { } body ||
                TerminatingOrDeleted(body) ||
                EntityManager.IsQueuedForDeletion(body))
            {
                continue;
            }

            var xform = Transform(body);
            if (xform.MapID != runtime.Map)
                continue;

            coordinates = xform.Coordinates;
            return true;
        }

        foreach (var points in runtime.SpawnPoints.Values)
        {
            foreach (var point in points)
            {
                if (!point.IsValid(EntityManager))
                    continue;

                coordinates = point;
                return true;
            }
        }

        coordinates = EntityCoordinates.Invalid;
        return false;
    }

    private void RemoveUnavailablePlayers(
        EntityUid match,
        SpaceArenaMatchComponent component,
        SpaceArenaMatchRuntimeComponent runtime)
    {
        var unavailable = new ValueList<NetUserId>(runtime.Players.Count);
        foreach (var (player, data) in runtime.Players)
        {
            if (!_players.TryGetSessionById(player, out var session) ||
                session.AttachedEntity != data.LobbyEntity ||
                data.LobbyEntity is not { } lobbyEntity ||
                TerminatingOrDeleted(lobbyEntity) ||
                EntityManager.IsQueuedForDeletion(lobbyEntity) ||
                !_mind.TryGetMind(player, out _, out _))
            {
                unavailable.Add(player);
            }
        }

        foreach (var player in unavailable)
            RemovePlayer(match, player, returnToLobby: false);

        component.PlayerCount = runtime.Players.Count;
    }

    private bool RemovePlayer(
        EntityUid match,
        NetUserId player,
        bool returnToLobby,
        bool finishIfBelowMinimum = true)
    {
        if (!TryGetMatchContext(match, out var component, out var runtime) ||
            !runtime.Players.Remove(player, out var data))
        {
            return false;
        }

        RemoveDeadline(runtime.Respawns, ref runtime.NextRespawn, player);
        RemoveDeadline(runtime.DisconnectForfeits, ref runtime.NextDisconnectForfeit, player);
        _playerMatches.Remove(player);

        if (returnToLobby)
            ReturnPlayer(runtime, player, data, SpaceArenaPlayerState.Lobby);
        else
            SetPlayerState(data.Mind, SpaceArenaPlayerState.Lobby);

        component.PlayerCount = runtime.Players.Count;

        var ev = new SpaceArenaMatchPlayerLeftEvent(player, data.Mind);
        RaiseLocalEvent(match, ref ev);

        if (finishIfBelowMinimum &&
            runtime.Players.Count < component.MinPlayers &&
            component.State is (SpaceArenaMatchState.Preparing or
                SpaceArenaMatchState.Countdown or
                SpaceArenaMatchState.Active))
        {
            FinishMatch(match);
        }

        return true;
    }

    private bool RemoveSpectator(EntityUid match, NetUserId player, bool returnToLobby)
    {
        if (!TryComp(match, out SpaceArenaMatchRuntimeComponent? runtime) ||
            !runtime.Spectators.Remove(player, out var data))
        {
            return false;
        }

        _spectatorMatches.Remove(player);

        if (returnToLobby)
            ReturnSpectator(runtime, player, data);
        else
            SetPlayerState(data.Mind, SpaceArenaPlayerState.Lobby);

        if (!returnToLobby && data.SpectatorEntity is { } ghost && Exists(ghost))
            QueueDel(ghost);

        return true;
    }

    private void OnMobStateChanged(
        Entity<SpaceArenaMatchMemberComponent> entity,
        ref MobStateChangedEvent args)
    {
        if (args.NewMobState is not (MobState.Critical or MobState.Dead) ||
            !TryGetMatchContext(entity.Comp.Match, out var component, out var runtime) ||
            component.State != SpaceArenaMatchState.Active ||
            !runtime.Players.TryGetValue(entity.Comp.Player, out var playerData))
        {
            return;
        }

        var ev = new SpaceArenaMatchPlayerMobStateChangedEvent(
            entity.Comp.Player,
            entity.Owner,
            args.NewMobState);
        RaiseLocalEvent(entity.Comp.Match, ref ev);

        if (args.NewMobState != MobState.Dead || component.State != SpaceArenaMatchState.Active)
            return;

        HandlePlayerDeath(component, runtime, entity.Comp.Player, playerData);
    }

    private void OnGhostAttempt(Entity<SpaceArenaMatchMemberComponent> entity, ref GhostAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnMatchMemberShutdown(
        Entity<SpaceArenaMatchMemberComponent> entity,
        ref ComponentShutdown args)
    {
        if (!TryGetMatchContext(entity.Comp.Match, out var component, out var runtime) ||
            component.State != SpaceArenaMatchState.Active ||
            !runtime.Players.TryGetValue(entity.Comp.Player, out var playerData) ||
            playerData.MatchEntity != entity.Owner ||
            TryComp(playerData.Mind, out SpaceArenaPlayerComponent? playerState) &&
            playerState.State is (SpaceArenaPlayerState.Eliminated or SpaceArenaPlayerState.Spectator))
        {
            return;
        }

        var ev = new SpaceArenaMatchPlayerMobStateChangedEvent(
            entity.Comp.Player,
            entity.Owner,
            MobState.Dead);
        RaiseLocalEvent(entity.Comp.Match, ref ev);

        if (component.State == SpaceArenaMatchState.Active)
            HandlePlayerDeath(component, runtime, entity.Comp.Player, playerData);
    }

    private void HandlePlayerDeath(
        SpaceArenaMatchComponent component,
        SpaceArenaMatchRuntimeComponent runtime,
        NetUserId player,
        SpaceArenaMatchPlayerData playerData)
    {
        SetPlayerState(playerData.Mind, SpaceArenaPlayerState.Eliminated);
        if (component.RespawnDelay is not { } delay)
        {
            SetPlayerState(playerData.Mind, SpaceArenaPlayerState.Spectator);
            return;
        }

        var respawnAt = _timing.CurTime + delay;
        runtime.Respawns[player] = respawnAt;
        if (runtime.NextRespawn == null || respawnAt < runtime.NextRespawn)
            runtime.NextRespawn = respawnAt;
    }

    private void OnStartedFallingIntoChasm(
        Entity<SpaceArenaMatchMemberComponent> entity,
        ref StartedFallingIntoChasmEvent args)
    {
        if (!TryComp(entity.Comp.Match, out SpaceArenaMatchComponent? component) ||
            component.State != SpaceArenaMatchState.Active)
            return;

        _mobState.ChangeMobState(entity.Owner, MobState.Dead);
    }

    private void OnLeaveSpectatingRequest(
        SpaceArenaLeaveSpectatingRequest args,
        EntitySessionEventArgs sessionArgs)
    {
        TryLeaveSpectating(sessionArgs.SenderSession);
    }

    private void OnLeaveMatchRequest(
        SpaceArenaLeaveMatchRequest args,
        EntitySessionEventArgs sessionArgs)
    {
        TryLeaveMatch(sessionArgs.SenderSession);
    }

    private void OnSpectatorShutdown(
        Entity<SpaceArenaSpectatorComponent> entity,
        ref ComponentShutdown args)
    {
        if (!TryComp(entity.Comp.Match, out SpaceArenaMatchRuntimeComponent? runtime) ||
            !runtime.Spectators.TryGetValue(entity.Comp.Player, out var data) ||
            data.SpectatorEntity != entity.Owner)
        {
            return;
        }

        data.SpectatorEntity = null;
        RemoveSpectator(entity.Comp.Match, entity.Comp.Player, returnToLobby: true);
    }

    private void ProcessRespawns(
        EntityUid match,
        SpaceArenaMatchComponent component,
        SpaceArenaMatchRuntimeComponent runtime,
        TimeSpan curTime)
    {
        if (runtime.NextRespawn is not { } nextRespawn || curTime < nextRespawn)
            return;

        var due = new ValueList<NetUserId>(runtime.Respawns.Count);
        runtime.NextRespawn = null;
        foreach (var (player, respawnAt) in runtime.Respawns)
        {
            if (curTime >= respawnAt)
                due.Add(player);
            else if (runtime.NextRespawn == null || respawnAt < runtime.NextRespawn)
                runtime.NextRespawn = respawnAt;
        }

        foreach (var player in due)
        {
            if (!runtime.Players.TryGetValue(player, out var data))
            {
                runtime.Respawns.Remove(player);
                continue;
            }

            var oldBody = data.MatchEntity;
            if (!TrySpawnMatchPlayer(match, component, runtime, player, data))
            {
                var retryAt = curTime + RespawnRetryDelay;
                runtime.Respawns[player] = retryAt;
                if (runtime.NextRespawn == null || retryAt < runtime.NextRespawn)
                    runtime.NextRespawn = retryAt;
                continue;
            }

            runtime.Respawns.Remove(player);
            if (oldBody is { } body && body != data.MatchEntity && Exists(body))
                QueueDel(body);
        }
    }

    private void ProcessDisconnectForfeits(
        EntityUid match,
        SpaceArenaMatchComponent component,
        SpaceArenaMatchRuntimeComponent runtime,
        TimeSpan curTime)
    {
        if (runtime.NextDisconnectForfeit is not { } nextForfeit || curTime < nextForfeit)
            return;

        var due = new ValueList<NetUserId>(runtime.DisconnectForfeits.Count);
        runtime.NextDisconnectForfeit = null;
        foreach (var (player, forfeitAt) in runtime.DisconnectForfeits)
        {
            if (curTime >= forfeitAt)
                due.Add(player);
            else if (runtime.NextDisconnectForfeit == null || forfeitAt < runtime.NextDisconnectForfeit)
                runtime.NextDisconnectForfeit = forfeitAt;
        }

        foreach (var player in due)
        {
            runtime.DisconnectForfeits.Remove(player);
            if (_players.TryGetSessionById(player, out var session) &&
                session.Status != SessionStatus.Disconnected)
            {
                continue;
            }

            if (!runtime.Players.TryGetValue(player, out var data) ||
                data.MatchEntity is not { } body ||
                TerminatingOrDeleted(body) ||
                !_mobState.IsAlive(body))
            {
                continue;
            }

            _mobState.ChangeMobState(body, MobState.Dead);
            if (component.State != SpaceArenaMatchState.Active)
                return;
        }
    }

    private void SetMatchState(
        EntityUid match,
        SpaceArenaMatchComponent component,
        SpaceArenaMatchRuntimeComponent runtime,
        SpaceArenaMatchState state,
        TimeSpan? duration)
    {
        var oldState = component.State;
        component.State = state;
        component.StateEndsAt = duration == null ? null : _timing.CurTime + duration.Value;

        if (state == SpaceArenaMatchState.Active)
        {
            foreach (var barrier in runtime.Barriers)
            {
                if (Exists(barrier))
                    QueueDel(barrier);
            }

            runtime.Barriers.Clear();
            SchedulePreActiveRespawns(match, component, runtime);
        }

        var playerState = PlayerStateForMatch(state);
        foreach (var (player, data) in runtime.Players)
        {
            if (state == SpaceArenaMatchState.Active && runtime.Respawns.ContainsKey(player))
                continue;

            SetPlayerState(data.Mind, playerState);
        }

        var ev = new SpaceArenaMatchStateChangedEvent(oldState, state);
        RaiseLocalEvent(match, ref ev);

        if (state == SpaceArenaMatchState.Active && component.State == SpaceArenaMatchState.Active)
            StartDisconnectedPlayerForfeits(match, runtime);
    }

    private void SchedulePreActiveRespawns(
        EntityUid match,
        SpaceArenaMatchComponent component,
        SpaceArenaMatchRuntimeComponent runtime)
    {
        if (component.RespawnDelay == null)
            return;

        foreach (var (player, data) in runtime.Players)
        {
            if (runtime.Respawns.ContainsKey(player) ||
                data.MatchEntity is { } body && !TerminatingOrDeleted(body) && _mobState.IsAlive(body))
            {
                continue;
            }

            HandlePlayerDeath(component, runtime, player, data);
        }
    }

    private void StartDisconnectedPlayerForfeits(
        EntityUid match,
        SpaceArenaMatchRuntimeComponent runtime)
    {
        foreach (var player in runtime.Players.Keys)
        {
            if (!_players.TryGetSessionById(player, out var session) ||
                session.Status == SessionStatus.Disconnected)
            {
                StartDisconnectForfeit(match, player);
            }
        }
    }

    private static SpaceArenaPlayerState PlayerStateForMatch(SpaceArenaMatchState state)
    {
        return state switch
        {
            SpaceArenaMatchState.Waiting => SpaceArenaPlayerState.MatchLobby,
            SpaceArenaMatchState.Preparing => SpaceArenaPlayerState.Preparing,
            SpaceArenaMatchState.Countdown => SpaceArenaPlayerState.Countdown,
            SpaceArenaMatchState.Active => SpaceArenaPlayerState.Active,
            SpaceArenaMatchState.Ending or SpaceArenaMatchState.Finished => SpaceArenaPlayerState.Results,
            _ => SpaceArenaPlayerState.Lobby,
        };
    }

    private void SetPlayerState(EntityUid mind, SpaceArenaPlayerState state)
    {
        if (!Exists(mind))
            return;

        if (state == SpaceArenaPlayerState.Lobby)
        {
            RemCompDeferred<SpaceArenaPlayerComponent>(mind);
            return;
        }

        var component = EnsureComp<SpaceArenaPlayerComponent>(mind);
        component.State = state;
    }

    private void CleanupMatch(
        EntityUid match,
        SpaceArenaMatchComponent component,
        SpaceArenaMatchRuntimeComponent runtime)
    {
        if (runtime.CleanedUp)
            return;

        ReturnAllSpectators(runtime);
        ReturnAllPlayers(runtime, SpaceArenaPlayerState.Lobby, removeMembership: true);
        component.PlayerCount = 0;
        DeleteArena(runtime);
        runtime.CleanedUp = true;
        QueueDel(match);
    }

    private void ReturnAllPlayers(
        SpaceArenaMatchRuntimeComponent runtime,
        SpaceArenaPlayerState state,
        bool removeMembership)
    {
        foreach (var (player, data) in runtime.Players)
        {
            ReturnPlayer(runtime, player, data, state);
            if (removeMembership)
                _playerMatches.Remove(player);
        }

        runtime.Respawns.Clear();
        runtime.DisconnectForfeits.Clear();
        runtime.NextRespawn = null;
        runtime.NextDisconnectForfeit = null;

        if (removeMembership)
            runtime.Players.Clear();
    }

    private void ReturnAllSpectators(SpaceArenaMatchRuntimeComponent runtime)
    {
        foreach (var (player, data) in runtime.Spectators)
        {
            ReturnSpectator(runtime, player, data);
            _spectatorMatches.Remove(player);
        }

        runtime.Spectators.Clear();
    }

    private bool TrySpawnHubBody(NetUserId player, EntityUid? lobbyStation, out EntityUid body)
    {
        body = EntityUid.Invalid;
        if (!TryGetHubSpawnStation(lobbyStation, out var station) ||
            !_players.TryGetSessionById(player, out var session) ||
            _stationSpawning.SpawnPlayerCharacterOnStation(
                station,
                null,
                _gameTicker.GetPlayerProfile(session)) is not { } spawned)
        {
            return false;
        }

        _stationSpawning.EquipStartingGear(spawned, HubStartingGear);
        body = spawned;
        return true;
    }

    private bool TryGetHubSpawnStation(EntityUid? preferredStation, out EntityUid station)
    {
        if (preferredStation is { } preferred &&
            !TerminatingOrDeleted(preferred) &&
            !EntityManager.IsQueuedForDeletion(preferred) &&
            HasComp<StationSpawningComponent>(preferred))
        {
            station = preferred;
            return true;
        }

        foreach (var candidate in _station.GetStations())
        {
            if (!TerminatingOrDeleted(candidate) &&
                !EntityManager.IsQueuedForDeletion(candidate) &&
                HasComp<StationSpawningComponent>(candidate))
            {
                station = candidate;
                return true;
            }
        }

        station = EntityUid.Invalid;
        return false;
    }

    private void ReturnPlayer(
        SpaceArenaMatchRuntimeComponent runtime,
        NetUserId player,
        SpaceArenaMatchPlayerData data,
        SpaceArenaPlayerState state)
    {
        var oldBody = data.MatchEntity;
        EntityUid? target = null;

        if (data.LobbyEntity is { } lobbyBody &&
            !TerminatingOrDeleted(lobbyBody) &&
            !EntityManager.IsQueuedForDeletion(lobbyBody) &&
            (!_mind.TryGetMind(lobbyBody, out var occupant, out _) || occupant == data.Mind))
        {
            target = lobbyBody;
        }
        else if (TrySpawnHubBody(player, data.LobbyStation, out var hubBody))
            target = hubBody;

        CloseBodyUis(oldBody);
        if (target != null && Exists(data.Mind))
        {
            _mind.TransferTo(data.Mind, target);
        }
        else if (Exists(data.Mind) && TryComp(data.Mind, out MindComponent? mindComponent))
        {
            var fallback = FindFallbackCoordinates(runtime.Map);
            if (fallback != null)
                _ghost.SpawnGhost((data.Mind, mindComponent), fallback);
            else
                _mind.TransferTo(data.Mind, null, createGhost: false, mind: mindComponent);
        }

        if (oldBody is { } matchBody && matchBody != target && Exists(matchBody))
            QueueDel(matchBody);

        data.LobbyEntity = state == SpaceArenaPlayerState.MatchLobby ? target : null;
        data.MatchEntity = null;
        SetPlayerState(data.Mind, state);
    }

    private void CloseBodyUis(EntityUid? body)
    {
        if (body is { } uid && Exists(uid))
            _ui.CloseUserUis(uid);
    }

    private void ReturnSpectator(
        SpaceArenaMatchRuntimeComponent runtime,
        NetUserId player,
        SpaceArenaMatchSpectatorData data)
    {
        var oldGhost = data.SpectatorEntity;
        EntityUid? target = null;

        if (TrySpawnHubBody(player, data.LobbyStation, out var hubBody))
            target = hubBody;

        CloseBodyUis(oldGhost);
        if (target != null && Exists(data.Mind))
        {
            _mind.TransferTo(data.Mind, target);
        }
        else if (Exists(data.Mind) && TryComp(data.Mind, out MindComponent? mindComponent))
        {
            var fallback = FindFallbackCoordinates(runtime.Map);
            if (fallback != null)
                _ghost.SpawnGhost((data.Mind, mindComponent), fallback, canReturn: false);
            else
                _mind.TransferTo(data.Mind, null, createGhost: false, mind: mindComponent);
        }

        if (oldGhost is { } ghost && ghost != target && Exists(ghost))
            QueueDel(ghost);

        data.SpectatorEntity = null;
        SetPlayerState(data.Mind, SpaceArenaPlayerState.Lobby);
    }

    private EntityCoordinates? FindFallbackCoordinates(MapId excludedMap)
    {
        foreach (var station in _station.GetStations())
        {
            if (TerminatingOrDeleted(station) ||
                EntityManager.IsQueuedForDeletion(station) ||
                _station.GetLargestGrid((station, null)) is not { } grid ||
                TerminatingOrDeleted(grid) ||
                EntityManager.IsQueuedForDeletion(grid) ||
                Transform(grid).MapID == excludedMap)
            {
                continue;
            }

            return new EntityCoordinates(grid, Vector2.Zero);
        }

        return null;
    }

    private void DeleteArena(SpaceArenaMatchRuntimeComponent runtime)
    {
        if (runtime.Map != MapId.Nullspace && _map.MapExists(runtime.Map))
            _map.QueueDeleteMap(runtime.Map);

        runtime.Map = MapId.Nullspace;
        runtime.Station = null;
        runtime.SpawnPoints.Clear();
        runtime.NextSpawnPoints.Clear();
        runtime.Barriers.Clear();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (_playerMatches.TryGetValue(args.Session.UserId, out var match))
        {
            if (args.NewStatus == SessionStatus.Disconnected)
            {
                if (TryComp(match, out SpaceArenaMatchComponent? component) && component.AllowVoluntaryLeave)
                    RemovePlayer(match, args.Session.UserId, returnToLobby: false);
                else
                    StartDisconnectForfeit(match, args.Session.UserId);
            }
            else
                CancelDisconnectForfeit(match, args.Session.UserId);

            return;
        }
    }

    private void StartDisconnectForfeit(EntityUid match, NetUserId player)
    {
        if (!TryGetMatchContext(match, out var component, out var runtime) ||
            component.State != SpaceArenaMatchState.Active ||
            component.DisconnectGracePeriod is not { } gracePeriod ||
            !runtime.Players.TryGetValue(player, out var data) ||
            data.MatchEntity is not { } body ||
            TerminatingOrDeleted(body) ||
            !_mobState.IsAlive(body))
        {
            return;
        }

        var forfeitAt = _timing.CurTime + gracePeriod;
        runtime.DisconnectForfeits[player] = forfeitAt;
        if (runtime.NextDisconnectForfeit == null || forfeitAt < runtime.NextDisconnectForfeit)
            runtime.NextDisconnectForfeit = forfeitAt;
    }

    private void CancelDisconnectForfeit(EntityUid match, NetUserId player)
    {
        if (!TryComp(match, out SpaceArenaMatchRuntimeComponent? runtime))
            return;

        RemoveDeadline(runtime.DisconnectForfeits, ref runtime.NextDisconnectForfeit, player);
    }

    private static void RemoveDeadline(
        Dictionary<NetUserId, TimeSpan> deadlines,
        ref TimeSpan? nextDeadline,
        NetUserId player)
    {
        if (!deadlines.Remove(player, out var removedAt) || nextDeadline != removedAt)
            return;

        nextDeadline = null;
        foreach (var deadline in deadlines.Values)
        {
            if (nextDeadline == null || deadline < nextDeadline)
                nextDeadline = deadline;
        }
    }

    private void OnMatchShutdown(
        Entity<SpaceArenaMatchRuntimeComponent> match,
        ref ComponentShutdown args)
    {
        if (match.Comp.CleanedUp)
            return;

        ReturnAllSpectators(match.Comp);
        ReturnAllPlayers(match.Comp, SpaceArenaPlayerState.Lobby, removeMembership: true);
        DeleteArena(match.Comp);
        match.Comp.CleanedUp = true;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        var query = EntityQueryEnumerator<SpaceArenaMatchComponent, SpaceArenaMatchRuntimeComponent>();
        while (query.MoveNext(out var uid, out var component, out var runtime))
            CleanupMatch(uid, component, runtime);
    }
}
