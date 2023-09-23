using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Shared.CCVar;
using Content.Shared.Station;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Station.Systems;

/// <summary>
/// System that manages stations.
/// A station is, by default, just a name, optional map prototype, and optional grids.
/// For jobs, look at StationJobSystem. For spawning, look at StationSpawningSystem.
/// </summary>
[PublicAPI]
public sealed class StationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private ISawmill _sawmill = default!;

    private bool _randomStationOffset;
    private bool _randomStationRotation;
    private float _maxRandomStationOffset;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _sawmill = _logManager.GetSawmill("station");

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRoundEnd);
        SubscribeLocalEvent<PreGameMapLoad>(OnPreGameMapLoad);
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
        SubscribeLocalEvent<StationDataComponent, ComponentStartup>(OnStationAdd);
        SubscribeLocalEvent<StationDataComponent, ComponentShutdown>(OnStationDeleted);
        SubscribeLocalEvent<StationMemberComponent, ComponentShutdown>(OnStationGridDeleted);
        SubscribeLocalEvent<StationMemberComponent, PostGridSplitEvent>(OnStationSplitEvent);

        _configurationManager.OnValueChanged(CCVars.StationOffset, x => _randomStationOffset = x, true);
        _configurationManager.OnValueChanged(CCVars.MaxStationOffset, x => _maxRandomStationOffset = x, true);
        _configurationManager.OnValueChanged(CCVars.StationRotation, x => _randomStationRotation = x, true);

        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void OnStationSplitEvent(EntityUid uid, StationMemberComponent component, ref PostGridSplitEvent args)
    {
        AddGridToStation(component.Station, args.Grid); // Add the new grid as a member.
    }

    private void OnStationGridDeleted(EntityUid uid, StationMemberComponent component, ComponentShutdown args)
    {
        if (!TryComp<StationDataComponent>(component.Station, out var stationData))
            return;

        stationData.Grids.Remove(uid);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Connected)
        {
            RaiseNetworkEvent(new StationsUpdatedEvent(GetNetEntitySet(GetStationsSet())), e.Session);
        }
    }

    #region Event handlers

    private void OnStationAdd(EntityUid uid, StationDataComponent component, ComponentStartup args)
    {
        RaiseNetworkEvent(new StationsUpdatedEvent(GetNetEntitySet(GetStationsSet())), Filter.Broadcast());

        var metaData = MetaData(uid);
        RaiseLocalEvent(new StationInitializedEvent(uid));
        _sawmill.Info($"Set up station {metaData.EntityName} ({uid}).");

    }

    private void OnStationDeleted(EntityUid uid, StationDataComponent component, ComponentShutdown args)
    {
        foreach (var grid in component.Grids)
        {
            RemComp<StationMemberComponent>(grid);
        }

        RaiseNetworkEvent(new StationsUpdatedEvent(GetNetEntitySet(GetStationsSet())), Filter.Broadcast());
    }

    private void OnPreGameMapLoad(PreGameMapLoad ev)
    {
        // this is only for maps loaded during round setup!
        if (_gameTicker.RunLevel == GameRunLevel.InRound)
            return;

        if (_randomStationOffset)
            ev.Options.Offset += _random.NextVector2(_maxRandomStationOffset);

        if (_randomStationRotation)
            ev.Options.Rotation = _random.NextAngle();
    }

    private void OnPostGameMapLoad(PostGameMapLoad ev)
    {
        var dict = new Dictionary<string, List<EntityUid>>();

        void AddGrid(string station, EntityUid grid)
        {
            if (dict.ContainsKey(station))
            {
                dict[station].Add(grid);
            }
            else
            {
                dict[station] = new List<EntityUid> {grid};
            }
        }

        // Iterate over all BecomesStation
        foreach (var grid in ev.Grids)
        {
            // We still setup the grid
            if (!TryComp<BecomesStationComponent>(grid, out var becomesStation))
                continue;

            AddGrid(becomesStation.Id, grid);
        }

        if (!dict.Any())
        {
            // Oh jeez, no stations got loaded.
            // We'll yell about it, but the thing this used to do with creating a dummy is kinda pointless now.
            _sawmill.Error($"There were no station grids for {ev.GameMap.ID}!");
        }

        foreach (var (id, gridIds) in dict)
        {
            StationConfig stationConfig;

            if (ev.GameMap.Stations.ContainsKey(id))
                stationConfig = ev.GameMap.Stations[id];
            else
            {
                _sawmill.Error($"The station {id} in map {ev.GameMap.ID} does not have an associated station config!");
                continue;
            }

            InitializeNewStation(stationConfig, gridIds, ev.StationName);
        }
    }

    private void OnRoundEnd(GameRunLevelChangedEvent eventArgs)
    {
        if (eventArgs.New != GameRunLevel.PreRoundLobby)
            return;

        var query = EntityQueryEnumerator<StationDataComponent>();
        while (query.MoveNext(out var station, out _))
        {
            QueueDel(station);
        }
    }

    #endregion Event handlers

    /// <summary>
    /// Gets the largest member grid from a station.
    /// </summary>
    public EntityUid? GetLargestGrid(StationDataComponent component)
    {
        EntityUid? largestGrid = null;
        Box2 largestBounds = new Box2();

        foreach (var gridUid in component.Grids)
        {
            if (!TryComp<MapGridComponent>(gridUid, out var grid) ||
                grid.LocalAABB.Size.LengthSquared() < largestBounds.Size.LengthSquared())
                continue;

            largestBounds = grid.LocalAABB;
            largestGrid = gridUid;
        }

        return largestGrid;
    }

    /// <summary>
    /// Tries to retrieve a filter for everything in the station the source is on.
    /// </summary>
    /// <param name="source">The entity to use to find the station.</param>
    /// <param name="range">The range around the station</param>
    /// <returns></returns>
    public Filter GetInOwningStation(EntityUid source, float range = 32f)
    {
        var station = GetOwningStation(source);

        if (TryComp<StationDataComponent>(station, out var data))
        {
            return GetInStation(data);
        }

        return Filter.Empty();
    }

    /// <summary>
    /// Retrieves a filter for everything in a particular station or near its member grids.
    /// </summary>
    public Filter GetInStation(StationDataComponent dataComponent, float range = 32f)
    {
        // Could also use circles if you wanted.
        var bounds = new ValueList<Box2>(dataComponent.Grids.Count);
        var filter = Filter.Empty();
        var mapIds = new ValueList<MapId>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach (var gridUid in dataComponent.Grids)
        {
            if (!_mapManager.TryGetGrid(gridUid, out var grid) ||
                !xformQuery.TryGetComponent(gridUid, out var xform))
                continue;

            var mapId = xform.MapID;
            var position = _transform.GetWorldPosition(xform, xformQuery);
            var bound = grid.LocalAABB.Enlarged(range).Translated(position);

            bounds.Add(bound);
            if (!mapIds.Contains(mapId))
            {
                mapIds.Add(xform.MapID);
            }
        }

        foreach (var session in Filter.GetAllPlayers(_player))
        {
            var entity = session.AttachedEntity;
            if (entity == null || !xformQuery.TryGetComponent(entity, out var xform))
                continue;

            var mapId = xform.MapID;

            if (!mapIds.Contains(mapId))
                continue;

            var position = _transform.GetWorldPosition(xform, xformQuery);

            foreach (var bound in bounds)
            {
                if (!bound.Contains(position))
                    continue;

                filter.AddPlayer(session);
                break;
            }
        }

        return filter;
    }

    /// <summary>
    /// Initializes a new station with the given information.
    /// </summary>
    /// <param name="stationConfig">The game map prototype used, if any.</param>
    /// <param name="gridIds">All grids that should be added to the station.</param>
    /// <param name="name">Optional override for the station name.</param>
    /// <remarks>This is for ease of use, manually spawning the entity works just fine.</remarks>
    /// <returns>The initialized station.</returns>
    public EntityUid InitializeNewStation(StationConfig stationConfig, IEnumerable<EntityUid>? gridIds, string? name = null)
    {
        // Use overrides for setup.
        var station = EntityManager.SpawnEntity(stationConfig.StationPrototype, MapCoordinates.Nullspace, stationConfig.StationComponentOverrides);

        if (name is not null)
            RenameStation(station, name, false);

        DebugTools.Assert(HasComp<StationDataComponent>(station), "Stations should have StationData in their prototype.");

        var data = Comp<StationDataComponent>(station);
        name ??= MetaData(station).EntityName;

        foreach (var grid in gridIds ?? Array.Empty<EntityUid>())
        {
            AddGridToStation(station, grid, null, data, name);
        }

        var ev = new StationPostInitEvent();
        RaiseLocalEvent(station, ref ev);

        return station;
    }

    /// <summary>
    /// Adds the given grid to a station.
    /// </summary>
    /// <param name="mapGrid">Grid to attach.</param>
    /// <param name="station">Station to attach the grid to.</param>
    /// <param name="gridComponent">Resolve pattern, grid component of mapGrid.</param>
    /// <param name="stationData">Resolve pattern, station data component of station.</param>
    /// <param name="name">The name to assign to the grid if any.</param>
    /// <exception cref="ArgumentException">Thrown when mapGrid or station are not a grid or station, respectively.</exception>
    public void AddGridToStation(EntityUid station, EntityUid mapGrid, MapGridComponent? gridComponent = null, StationDataComponent? stationData = null, string? name = null)
    {
        if (!Resolve(mapGrid, ref gridComponent))
            throw new ArgumentException("Tried to initialize a station on a non-grid entity!", nameof(mapGrid));
        if (!Resolve(station, ref stationData))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        if (!string.IsNullOrEmpty(name))
            _metaData.SetEntityName(mapGrid, name);

        var stationMember = AddComp<StationMemberComponent>(mapGrid);
        stationMember.Station = station;
        stationData.Grids.Add(mapGrid);

        RaiseLocalEvent(station, new StationGridAddedEvent(mapGrid, false), true);

        _sawmill.Info($"Adding grid {mapGrid} to station {Name(station)} ({station})");
    }

    /// <summary>
    /// Removes the given grid from a station.
    /// </summary>
    /// <param name="station">Station to remove the grid from.</param>
    /// <param name="mapGrid">Grid to remove</param>
    /// <param name="gridComponent">Resolve pattern, grid component of mapGrid.</param>
    /// <param name="stationData">Resolve pattern, station data component of station.</param>
    /// <exception cref="ArgumentException">Thrown when mapGrid or station are not a grid or station, respectively.</exception>
    public void RemoveGridFromStation(EntityUid station, EntityUid mapGrid, MapGridComponent? gridComponent = null, StationDataComponent? stationData = null)
    {
        if (!Resolve(mapGrid, ref gridComponent))
            throw new ArgumentException("Tried to initialize a station on a non-grid entity!", nameof(mapGrid));
        if (!Resolve(station, ref stationData))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        RemComp<StationMemberComponent>(mapGrid);
        stationData.Grids.Remove(mapGrid);

        RaiseLocalEvent(station, new StationGridRemovedEvent(mapGrid), true);
        _sawmill.Info($"Removing grid {mapGrid} from station {Name(station)} ({station})");
    }

    /// <summary>
    /// Renames the given station.
    /// </summary>
    /// <param name="station">Station to rename.</param>
    /// <param name="name">The new name to apply.</param>
    /// <param name="loud">Whether or not to announce the rename.</param>
    /// <param name="stationData">Resolve pattern, station data component of station.</param>
    /// <param name="metaData">Resolve pattern, metadata component of station.</param>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    public void RenameStation(EntityUid station, string name, bool loud = true, StationDataComponent? stationData = null, MetaDataComponent? metaData = null)
    {
        if (!Resolve(station, ref stationData, ref metaData))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        var oldName = metaData.EntityName;
        _metaData.SetEntityName(station, name, metaData);

        if (loud)
        {
            _chatSystem.DispatchStationAnnouncement(station, $"The station {oldName} has been renamed to {name}.");
        }

        RaiseLocalEvent(station, new StationRenamedEvent(oldName, name), true);
    }

    /// <summary>
    /// Deletes the given station.
    /// </summary>
    /// <param name="station">Station to delete.</param>
    /// <param name="stationData">Resolve pattern, station data component of station.</param>
    /// <exception cref="ArgumentException">Thrown when the given station is not a station.</exception>
    public void DeleteStation(EntityUid station, StationDataComponent? stationData = null)
    {
        if (!Resolve(station, ref stationData))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        QueueDel(station);
    }

    /// <summary>
    /// Gets the station that "owns" the given entity (essentially, the station the grid it's on is attached to)
    /// </summary>
    /// <param name="entity">Entity to find the owner of.</param>
    /// <param name="xform">Resolve pattern, transform of the entity.</param>
    /// <returns>The owning station, if any.</returns>
    /// <remarks>
    /// This does not remember what station an entity started on, it simply checks where it is currently located.
    /// </remarks>
    public EntityUid? GetOwningStation(EntityUid entity, TransformComponent? xform = null)
    {
        if (!Resolve(entity, ref xform))
            throw new ArgumentException("Tried to use an abstract entity!", nameof(entity));

        if (TryComp<StationDataComponent>(entity, out _))
        {
            // We are the station, just return ourselves.
            return entity;
        }

        if (TryComp<MapGridComponent>(entity, out _))
        {
            // We are the station, just check ourselves.
            return CompOrNull<StationMemberComponent>(entity)?.Station;
        }

        if (xform.GridUid == EntityUid.Invalid)
        {
            Logger.Debug("A");
            return null;
        }

        return CompOrNull<StationMemberComponent>(xform.GridUid)?.Station;
    }

    public List<EntityUid> GetStations()
    {
        return EntityQuery<StationDataComponent>().Select(x => x.Owner).ToList();
    }

    public HashSet<EntityUid> GetStationsSet()
    {
        return EntityQuery<StationDataComponent>().Select(x => x.Owner).ToHashSet();
    }

    /// <summary>
    /// Returns the first station that has a grid in a certain map.
    /// If the map has no stations, null is returned instead.
    /// </summary>
    /// <remarks>
    /// If there are multiple stations on a map it is probably arbitrary which one is returned.
    /// </remarks>
    public EntityUid? GetStationInMap(MapId map)
    {
        var query = EntityQueryEnumerator<StationDataComponent>();
        while (query.MoveNext(out var uid, out var data))
        {
            foreach (var gridUid in data.Grids)
            {
                if (Transform(gridUid).MapID == map)
                {
                    return uid;
                }
            }
        }

        return null;
    }
}

/// <summary>
/// Broadcast event fired when a station is first set up.
/// This is the ideal point to add components to it.
/// </summary>
[PublicAPI]
public sealed class StationInitializedEvent : EntityEventArgs
{
    /// <summary>
    /// Station this event is for.
    /// </summary>
    public EntityUid Station;

    public StationInitializedEvent(EntityUid station)
    {
        Station = station;
    }
}

/// <summary>
/// Directed event fired on a station when a grid becomes a member of the station.
/// </summary>
[PublicAPI]
public sealed class StationGridAddedEvent : EntityEventArgs
{
    /// <summary>
    /// ID of the grid added to the station.
    /// </summary>
    public EntityUid GridId;

    /// <summary>
    /// Indicates that the event was fired during station setup,
    /// so that it can be ignored if StationInitializedEvent was already handled.
    /// </summary>
    public bool IsSetup;

    public StationGridAddedEvent(EntityUid gridId, bool isSetup)
    {
        GridId = gridId;
        IsSetup = isSetup;
    }
}

/// <summary>
/// Directed event fired on a station when a grid is no longer a member of the station.
/// </summary>
[PublicAPI]
public sealed class StationGridRemovedEvent : EntityEventArgs
{
    /// <summary>
    /// ID of the grid removed from the station.
    /// </summary>
    public EntityUid GridId;

    public StationGridRemovedEvent(EntityUid gridId)
    {
        GridId = gridId;
    }
}

/// <summary>
/// Directed event fired on a station when it is renamed.
/// </summary>
[PublicAPI]
public sealed class StationRenamedEvent : EntityEventArgs
{
    /// <summary>
    /// Prior name of the station.
    /// </summary>
    public string OldName;

    /// <summary>
    /// New name of the station.
    /// </summary>
    public string NewName;

    public StationRenamedEvent(string oldName, string newName)
    {
        OldName = oldName;
        NewName = newName;
    }
}

