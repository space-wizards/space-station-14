using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared.DeadSpace.StationAi;
using Content.Shared.DeadSpace.StationAI.UI;
using Content.Server.Silicons.StationAi;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Item;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Popups;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.StationAI.Systems;

public sealed class AiEyeSystem : EntitySystem
{
    private const int MinSearchLength = 2;
    private const int MinItemSearchLength = 3;
    private const int MaxSearchLength = 64;
    private const int MaxSearchResults = 40;
    private const int MaxSearchCandidates = 150;
    private const int MaxItemNameMatches = 80;
    private static readonly TimeSpan SearchCooldown = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan TargetJumpCooldown = TimeSpan.FromSeconds(0.75);
    private static readonly TimeSpan SensorCacheRefreshRate = TimeSpan.FromSeconds(1);

    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationAiSystem _stationAi = default!;
    [Dependency] private readonly StationAiVisionSystem _vision = default!;

    private EntityQuery<BroadphaseComponent> _broadphaseQuery;
    private EntityQuery<MapGridComponent> _gridQuery;
    private readonly Dictionary<EntityUid, TimeSpan> _nextSearchTime = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextTargetJumpTime = new();
    private readonly HashSet<EntityUid> _sensorTrackedUsers = new();
    private TimeSpan _nextSensorCacheRefresh;

    public override void Initialize()
    {
        base.Initialize();

        _broadphaseQuery = GetEntityQuery<BroadphaseComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<AiEyeComponent, EyeMoveToCam>(OnMoveToCam);
        SubscribeLocalEvent<AiEyeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AiEyeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AiEyeComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<AiEyeComponent, AiCameraSearchRequestMessage>(OnSearchRequest);
        SubscribeLocalEvent<AiEyeComponent, AiCameraJumpToTargetMessage>(OnJumpToTarget);
        SubscribeNetworkEvent<StationAiTrackEntityNetworkEvent>(OnTrackEntityRequest);
    }

    private void OnStartup(EntityUid uid, AiEyeComponent component, ComponentStartup args)
    {
        RefreshCameras((uid, component));
    }

    private void OnShutdown(Entity<AiEyeComponent> ent, ref ComponentShutdown args)
    {
        _nextSearchTime.Remove(ent.Owner);
        _nextTargetJumpTime.Remove(ent.Owner);
    }

    private void OnUiOpened(Entity<AiEyeComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!Equals(args.UiKey, AICameraListUiKey.Key))
            return;

        RefreshCameras(ent);
    }

    private void RefreshCameras(Entity<AiEyeComponent> ent)
    {
        ent.Comp.Cameras.Clear();

        var eyeGrid = GetAiEyeGrid(ent.Owner);
        if (eyeGrid == null)
        {
            Dirty(ent);
            return;
        }

        var cameras = EntityQueryEnumerator<SurveillanceCameraComponent, TransformComponent>();

        while (cameras.MoveNext(out var camUid, out var camera, out var transformComponent))
        {
            if (transformComponent.GridUid != eyeGrid)
                continue;

            if (!IsCameraAvailableForVision(camUid, camera)) // DS14: unpowered cameras still provide video.
                continue;

            ent.Comp.Cameras.Add((GetNetEntity(camUid), GetNetCoordinates(transformComponent.Coordinates)));
        }

        Dirty(ent);
    }

    private void OnMoveToCam(Entity<AiEyeComponent> ent, ref EyeMoveToCam args)
    {
        if (args.Actor != ent.Owner)
            return;

        if (!TryGetEntity(args.Uid, out var camera))
            return;

        if (!TryComp<SurveillanceCameraComponent>(camera, out var cameraComp))
            return;

        if (!IsCameraAvailableForVision(camera.Value, cameraComp)) // DS14: allow jumps to unpowered cameras.
        {
            PopupToAi(ent.Owner, Loc.GetString("station-ai-camera-not-working"));
            return;
        }

        if (!TryGetCameraCoordinates(ent.Owner, camera.Value, out var coordinates))
        {
            PopupToAi(ent.Owner, Loc.GetString("station-ai-camera-search-not-visible"));
            return;
        }

        TryMoveEye(ent.Owner, coordinates);
    }

    // DS14-start
    private bool IsCameraAvailableForVision(EntityUid camera, SurveillanceCameraComponent cameraComp)
    {
        if (!TryComp<StationAiVisionComponent>(camera, out var vision))
            return cameraComp.Active;

        if (!vision.Enabled)
            return false;

        return !vision.NeedsAnchoring || Transform(camera).Anchored;
    }
    // DS14-end

    private void OnSearchRequest(Entity<AiEyeComponent> ent, ref AiCameraSearchRequestMessage args)
    {
        if (args.Actor != ent.Owner)
            return;

        var query = args.Query.Trim();
        var state = new AiCameraSearchResultsState();

        if (query.Length < MinSearchLength)
        {
            state.Message = Loc.GetString("station-ai-camera-search-too-short", ("count", MinSearchLength));
            _ui.SetUiState(ent.Owner, AICameraListUiKey.Key, state);
            return;
        }

        if (query.Length > MaxSearchLength)
        {
            state.Message = Loc.GetString("station-ai-camera-search-too-long", ("count", MaxSearchLength));
            _ui.SetUiState(ent.Owner, AICameraListUiKey.Key, state);
            return;
        }

        if (args.Type == AiCameraSearchType.Items && query.Length < MinItemSearchLength)
        {
            state.Message = Loc.GetString("station-ai-camera-search-too-short", ("count", MinItemSearchLength));
            _ui.SetUiState(ent.Owner, AICameraListUiKey.Key, state);
            return;
        }

        if (TryGetCooldown(_nextSearchTime, ent.Owner, out var remaining))
        {
            state.Message = Loc.GetString("station-ai-camera-search-cooldown", ("seconds", GetRemainingSeconds(remaining)));
            _ui.SetUiState(ent.Owner, AICameraListUiKey.Key, state);
            return;
        }

        _nextSearchTime[ent.Owner] = _timing.CurTime + SearchCooldown;
        var eyeGrid = GetAiEyeGrid(ent.Owner);
        var checkedCandidates = 0;
        HashSet<EntityUid>? sensorTrackedTargets = null;

        if (args.Type is AiCameraSearchType.All or AiCameraSearchType.Characters)
        {
            sensorTrackedTargets = GetSensorTrackedTargets(ent.Owner);
            AddCharacterResults(ent.Owner, eyeGrid, query, state.Results, ref checkedCandidates, sensorTrackedTargets);
        }

        if (state.Results.Count < MaxSearchResults &&
            checkedCandidates < MaxSearchCandidates &&
            query.Length >= MinItemSearchLength &&
            args.Type is AiCameraSearchType.All or AiCameraSearchType.Items)
        {
            AddItemResults(ent.Owner, eyeGrid, query, state.Results, ref checkedCandidates);
        }

        state.Message = state.Results.Count == 0
            ? Loc.GetString("station-ai-camera-search-empty")
            : Loc.GetString("station-ai-camera-search-count", ("count", state.Results.Count));

        _ui.SetUiState(ent.Owner, AICameraListUiKey.Key, state);
    }

    private void OnJumpToTarget(Entity<AiEyeComponent> ent, ref AiCameraJumpToTargetMessage args)
    {
        if (args.Actor != ent.Owner)
            return;

        if (!TryGetEntity(args.Target, out var target))
        {
            PopupToAi(ent.Owner, Loc.GetString("station-ai-camera-search-invalid"));
            return;
        }

        TryJumpToTarget(ent.Owner, target.Value);
    }

    private void OnTrackEntityRequest(StationAiTrackEntityNetworkEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ai ||
            !HasComp<StationAiHeldComponent>(ai) ||
            !HasComp<AiEyeComponent>(ai))
        {
            return;
        }

        if (!TryGetEntity(msg.Target, out var target))
        {
            PopupToAi(ai, Loc.GetString("station-ai-camera-search-invalid"));
            return;
        }

        TryJumpToTarget(ai, target.Value);
    }

    private bool TryJumpToTarget(EntityUid ai, EntityUid target, bool showPopup = true)
    {
        if (!TryUseTargetJumpCooldown(ai, showPopup))
            return false;

        if (HasTrackingSuitSensors(target) && TryGetSensorTrackedCoordinates(ai, target, out var sensorCoordinates))
            return TryMoveEye(ai, sensorCoordinates);

        return TryJumpToVisibleTarget(ai, target, showPopup);
    }

    public bool TryJumpToVisibleTarget(EntityUid ai, EntityUid target, bool showPopup = true)
    {
        if (!TryGetVisibleTargetCoordinates(ai, target, out var coordinates, out var failMessage))
        {
            if (showPopup)
                PopupToAi(ai, failMessage);

            return false;
        }

        return TryMoveEye(ai, coordinates);
    }

    private bool TryGetCameraCoordinates(EntityUid ai, EntityUid camera, out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;

        if (!_stationAi.TryGetCore(ai, out var core) || core.Comp?.RemoteEntity == null)
            return false;

        var eyeGrid = Transform(core.Comp.RemoteEntity.Value).GridUid;
        var cameraXform = Transform(camera);

        if (eyeGrid == null || cameraXform.GridUid == null || eyeGrid.Value != cameraXform.GridUid.Value)
            return false;

        coordinates = cameraXform.Coordinates;
        return true;
    }

    private bool TryMoveEye(EntityUid ai, EntityCoordinates coordinates)
    {
        if (!_stationAi.TryGetCore(ai, out var core) || core.Comp?.RemoteEntity == null)
            return false;

        var eye = core.Comp.RemoteEntity.Value;
        _transform.SetCoordinates(eye, coordinates);
        _transform.AttachToGridOrMap(eye);
        return true;
    }

    private bool TryGetSensorTrackedCoordinates(
        EntityUid ai,
        EntityUid target,
        out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;

        if (!_stationAi.TryGetCore(ai, out var core) || core.Comp?.RemoteEntity == null)
            return false;

        var eyeGrid = Transform(core.Comp.RemoteEntity.Value).GridUid;
        var targetXform = Transform(target);
        var targetGrid = targetXform.GridUid;

        if (eyeGrid == null || targetGrid == null || eyeGrid.Value != targetGrid.Value)
            return false;

        if (!_gridQuery.TryComp(targetGrid.Value, out var mapGrid))
            return false;

        var worldPosition = _transform.GetWorldPosition(targetXform);
        var localPosition = _map.WorldToLocal(targetGrid.Value, mapGrid, worldPosition);
        coordinates = new EntityCoordinates(targetGrid.Value, localPosition);
        return true;
    }

    private bool TryGetVisibleTargetCoordinates(
        EntityUid ai,
        EntityUid target,
        out EntityCoordinates coordinates,
        out string failMessage)
    {
        if (!TryGetPotentialCameraTarget(ai, target, out coordinates, out var grid, out var targetTile, out failMessage))
            return false;

        if (!IsTargetAccessible(grid, targetTile))
            return false;

        return true;
    }

    private bool TryGetPotentialCameraTarget(
        EntityUid ai,
        EntityUid target,
        out EntityCoordinates coordinates,
        out Entity<BroadphaseComponent, MapGridComponent> grid,
        out Vector2i targetTile,
        out string failMessage)
    {
        coordinates = EntityCoordinates.Invalid;
        grid = default;
        targetTile = default;
        failMessage = Loc.GetString("station-ai-camera-search-not-visible");

        if (TerminatingOrDeleted(target))
        {
            failMessage = Loc.GetString("station-ai-camera-search-invalid");
            return false;
        }

        if (!_stationAi.TryGetCore(ai, out var core) || core.Comp?.RemoteEntity == null)
        {
            failMessage = Loc.GetString("station-ai-camera-search-no-eye");
            return false;
        }

        if (_container.TryGetContainingContainer((target, null, null), out _))
            return false;

        var eye = core.Comp.RemoteEntity.Value;
        var eyeGrid = Transform(eye).GridUid;
        var targetXform = Transform(target);
        var targetGrid = targetXform.GridUid;

        if (eyeGrid == null || targetGrid == null || eyeGrid.Value != targetGrid.Value)
            return false;

        if (!_broadphaseQuery.TryComp(targetGrid.Value, out var broadphase) ||
            !_gridQuery.TryComp(targetGrid.Value, out var mapGrid))
            return false;

        grid = (targetGrid.Value, broadphase, mapGrid);
        var worldPosition = _transform.GetWorldPosition(targetXform);
        var localPosition = _map.WorldToLocal(targetGrid.Value, mapGrid, worldPosition);
        coordinates = new EntityCoordinates(targetGrid.Value, localPosition);
        targetTile = _map.LocalToTile(targetGrid.Value, mapGrid, coordinates);
        return true;
    }

    private bool IsTargetAccessible(Entity<BroadphaseComponent, MapGridComponent> grid, Vector2i targetTile)
    {
        lock (_vision)
        {
            return _vision.IsAccessible(grid, targetTile);
        }
    }

    private void AddCharacterResults(
        EntityUid ai,
        EntityUid? eyeGrid,
        string query,
        List<AiCameraSearchResult> results,
        ref int checkedCandidates,
        HashSet<EntityUid> sensorTrackedTargets)
    {
        if (eyeGrid == null)
            return;

        var actors = EntityQueryEnumerator<ActorComponent, MetaDataComponent>();
        while (actors.MoveNext(out var uid, out _, out _))
        {
            if (uid == ai || HasComp<GhostComponent>(uid))
                continue;

            if (Transform(uid).GridUid != eyeGrid)
                continue;

            var name = Identity.Name(uid, EntityManager, ai);
            if (!MatchesSearch(name, query))
                continue;

            if (!TryAddSearchResult(ai, uid, name, AiCameraSearchResultType.Character, results, ref checkedCandidates, sensorTrackedTargets))
                return;
        }
    }

    private void AddItemResults(
        EntityUid ai,
        EntityUid? eyeGrid,
        string query,
        List<AiCameraSearchResult> results,
        ref int checkedCandidates)
    {
        if (eyeGrid == null)
            return;

        var matchedNames = 0;
        var items = EntityQueryEnumerator<ItemComponent, MetaDataComponent, TransformComponent>();

        while (items.MoveNext(out var uid, out _, out var meta, out var xform))
        {
            if (HasComp<ActorComponent>(uid))
                continue;

            if (xform.GridUid != eyeGrid)
                continue;

            var name = meta.EntityName;
            if (!MatchesSearch(name, query))
                continue;

            matchedNames++;
            if (matchedNames > MaxItemNameMatches)
                return;

            if (!TryAddSearchResult(ai, uid, name, AiCameraSearchResultType.Item, results, ref checkedCandidates))
                return;
        }
    }

    private bool TryAddSearchResult(
        EntityUid ai,
        EntityUid target,
        string name,
        AiCameraSearchResultType type,
        List<AiCameraSearchResult> results,
        ref int checkedCandidates,
        HashSet<EntityUid>? sensorTrackedTargets = null)
    {
        if (results.Count >= MaxSearchResults)
            return false;

        if (sensorTrackedTargets != null && sensorTrackedTargets.Contains(target))
        {
            results.Add(new AiCameraSearchResult(GetNetEntity(target), name, type));
            return results.Count < MaxSearchResults;
        }

        if (!TryGetPotentialCameraTarget(ai, target, out _, out var grid, out var targetTile, out _))
            return true;

        if (checkedCandidates >= MaxSearchCandidates)
            return false;

        checkedCandidates++;

        if (!IsTargetAccessible(grid, targetTile))
            return true;

        results.Add(new AiCameraSearchResult(GetNetEntity(target), name, type));
        return results.Count < MaxSearchResults;
    }

    private static bool MatchesSearch(string name, string query)
    {
        return name.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    private HashSet<EntityUid> GetSensorTrackedTargets(EntityUid ai)
    {
        RefreshSensorTrackedUsers();
        var result = new HashSet<EntityUid>();

        if (!_stationAi.TryGetCore(ai, out var core) || core.Comp?.RemoteEntity == null)
            return result;

        var eyeGrid = Transform(core.Comp.RemoteEntity.Value).GridUid;
        if (eyeGrid == null)
            return result;

        foreach (var user in _sensorTrackedUsers)
        {
            if (TerminatingOrDeleted(user))
                continue;

            if (Transform(user).GridUid == eyeGrid)
                result.Add(user);
        }

        return result;
    }

    private bool HasTrackingSuitSensors(EntityUid target)
    {
        RefreshSensorTrackedUsers();
        return _sensorTrackedUsers.Contains(target);
    }

    private void RefreshSensorTrackedUsers()
    {
        var now = _timing.CurTime;
        if (now < _nextSensorCacheRefresh)
            return;

        _nextSensorCacheRefresh = now + SensorCacheRefreshRate;
        _sensorTrackedUsers.Clear();

        var sensors = EntityQueryEnumerator<SuitSensorComponent>();
        while (sensors.MoveNext(out _, out var sensor))
        {
            if (!IsTrackingSensorMode(sensor.Mode) || sensor.User == null)
                continue;

            var user = sensor.User.Value;
            if (!TerminatingOrDeleted(user))
                _sensorTrackedUsers.Add(user);
        }
    }

    private static bool IsTrackingSensorMode(SuitSensorMode mode)
    {
        return mode == SuitSensorMode.SensorCords;
    }

    private bool TryUseTargetJumpCooldown(EntityUid ai, bool showPopup = true)
    {
        if (TryGetCooldown(_nextTargetJumpTime, ai, out var remaining))
        {
            if (showPopup)
            {
                PopupToAi(ai,
                    Loc.GetString("station-ai-camera-jump-cooldown", ("seconds", GetRemainingSeconds(remaining))));
            }

            return false;
        }

        _nextTargetJumpTime[ai] = _timing.CurTime + TargetJumpCooldown;
        return true;
    }

    private bool TryGetCooldown(Dictionary<EntityUid, TimeSpan> cooldowns, EntityUid ai, out TimeSpan remaining)
    {
        remaining = TimeSpan.Zero;

        if (!cooldowns.TryGetValue(ai, out var nextAllowed))
            return false;

        var now = _timing.CurTime;
        if (now >= nextAllowed)
            return false;

        remaining = nextAllowed - now;
        return true;
    }

    private static int GetRemainingSeconds(TimeSpan remaining)
    {
        return Math.Max(1, (int) Math.Ceiling(remaining.TotalSeconds));
    }

    private EntityUid? GetAiEyeGrid(EntityUid ai)
    {
        if (_stationAi.TryGetCore(ai, out var core) && core.Comp?.RemoteEntity != null)
            return Transform(core.Comp.RemoteEntity.Value).GridUid;

        return Transform(ai).GridUid;
    }

    private void PopupToAi(EntityUid ai, string message)
    {
        if (TryComp(ai, out ActorComponent? actor))
        {
            _chat.DispatchServerMessage(actor.PlayerSession, message);
            return;
        }

        var popupTarget = ai;

        if (_stationAi.TryGetCore(ai, out var core) && core.Comp?.RemoteEntity != null)
            popupTarget = core.Comp.RemoteEntity.Value;

        _popup.PopupEntity(message, popupTarget, ai, PopupType.MediumCaution);
    }
}
