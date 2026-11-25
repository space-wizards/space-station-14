using System.Linq;
using Content.Shared.Chat;
using Content.Shared.Maps;
using Content.Shared.Station.Components;
using Content.Shared.Station.Events;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Station.Systems;

/// <summary>
/// System that manages stations.
/// A station is, by default, just a name, optional map prototype, and optional grids.
/// For jobs, look at StationJobSystem. For spawning, look at StationSpawningSystem.
/// </summary>
[PublicAPI]
public abstract partial class StationSystem : EntitySystem
{
    [Dependency] protected IRobustRandom Random = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] protected MetaDataSystem Meta = default!;
    [Dependency] protected SharedMapSystem Map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedChatSystem _chat = default!;

    [Dependency] private EntityQuery<StationMemberComponent> _stationMemberQuery;
    [Dependency] protected EntityQuery<MapGridComponent> GridQuery;

    private ValueList<MapId> _mapIds;
    private ValueList<(Box2Rotated Bounds, MapId MapId)> _gridBounds;

    protected ISawmill Sawmill = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        Sawmill = LogManager.GetSawmill("station");
    }

    /// <summary>
    /// Initializes a new station with the given information.
    /// </summary>
    /// <param name="stationConfig">The station configuration to use.</param>
    /// <param name="gridIds">All grids that should be added to the station.</param>
    /// <param name="name">Optional override for the station name.</param>
    /// <param name="gameMap">The game map that created this station, if any.</param>
    /// <remarks>This is for ease of use, manually spawning the entity works just fine.</remarks>
    /// <returns>The initialized station.</returns>
    public EntityUid InitializeNewStation(
        StationConfig stationConfig,
        IEnumerable<EntityUid>? gridIds,
        string? name = null,
        GameMapPrototype? gameMap = null)
    {
        // Use overrides for setup.
        var station = EntityManager.SpawnEntity(stationConfig.StationPrototype, MapCoordinates.Nullspace, stationConfig.StationComponentOverrides);

        if (name is not null)
            RenameStation(station, name, false);

        DebugTools.Assert(HasComp<StationDataComponent>(station), "Stations should have StationData in their prototype.");

        var data = Comp<StationDataComponent>(station);
        data.JobWeights = gameMap?.JobWeights;
        Dirty(station, data);
        name ??= MetaData(station).EntityName;

        foreach (var grid in gridIds ?? Array.Empty<EntityUid>())
        {
            AddGridToStation(station, grid, null, data, name);
        }

        var ev = new StationPostInitEvent((station, data));
        RaiseLocalEvent(station, ref ev, true);

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
            Meta.SetEntityName(mapGrid, name);

        var stationMember = EnsureComp<StationMemberComponent>(mapGrid);
        stationMember.Station = station;
        stationData.Grids.Add(mapGrid);
        Dirty(station, stationData);
        Dirty(mapGrid, stationMember);

        RaiseLocalEvent(station, new StationGridAddedEvent(mapGrid, station, false), true);

        Sawmill.Info($"Adding grid {mapGrid} to station {Name(station)} ({station})");
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
        Dirty(station, stationData);

        RaiseLocalEvent(station, new StationGridRemovedEvent(mapGrid, station), true);
        Sawmill.Info($"Removing grid {mapGrid} from station {Name(station)} ({station})");
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
        Meta.SetEntityName(station, name, metaData);

        if (loud)
        {
            _chat.DispatchStationAnnouncement(station, $"The station {oldName} has been renamed to {name}.");
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
    /// Get entities with <see cref="TComponent"/> on a random event-eligible station's largest grid.
    /// </summary>
    /// <param name="onlyAnchored">Whether to only get anchored entities.
    /// Good check for air vents, bad for containers like crates.</param>
    /// <returns>All matching entities.</returns>
    public HashSet<Entity<TComponent>> GetEntitiesWithComponentOnStation<TComponent>(bool onlyAnchored)
        where TComponent : IComponent
    {
        return GetEntitiesWithComponentOnStation<TComponent>(onlyAnchored, out _);
    }

    /// <param name="station">The station picked to search for the entities.</param>
    /// <inheritdoc cref="GetEntitiesWithComponentOnStation{TComponent}(bool)" />
    public HashSet<Entity<TComponent>> GetEntitiesWithComponentOnStation<TComponent>(bool onlyAnchored,
        out Entity<StationDataComponent>? station)
        where TComponent : IComponent
    {
        HashSet<Entity<TComponent>> entities = [];

        if (!TryGetRandomStation<StationEventEligibleComponent>(out var eligibleStation))
        {
            station = null;
            return entities;
        }

        station = (eligibleStation.Value.Owner, eligibleStation.Value.Comp1);

        var grid = GetLargestGrid(station.Value.Owner);
        if (grid is null)
            return entities;

        var locations = EntityQueryEnumerator<TComponent, TransformComponent>();
        while (locations.MoveNext(out var uid, out var component, out var transform))
        {
            if (onlyAnchored && !transform.Anchored)
                continue;

            if (transform.GridUid != grid)
                continue;

            entities.Add((uid, component));
        }

        return entities;
    }

    /// <summary>
    /// Gets the largest member grid from a station.
    /// </summary>
    /// <remarks>
    /// "Largest" is determined by the total area of the grid's bounding box, not by the tile count.
    /// If multiple grids share the largest area, any of them may be returned.
    /// </remarks>
    public EntityUid? GetLargestGrid(Entity<StationDataComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        EntityUid? largestGrid = null;
        var largestArea = 0.0f;

        foreach (var gridUid in ent.Comp.Grids)
        {
            if (!TryComp<MapGridComponent>(gridUid, out var grid))
                continue;

            var gridArea = Box2.Area(grid.LocalAABB);
            if (gridArea < largestArea)
                continue;

            largestArea = gridArea;
            largestGrid = gridUid;
        }

        return largestGrid;
    }

    /// <summary>
    /// Returns the total number of tiles contained in the station's grids.
    /// </summary>
    public int GetTileCount(Entity<StationDataComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return 0;

        var count = 0;
        foreach (var gridUid in ent.Comp.Grids)
        {
            if (!TryComp<MapGridComponent>(gridUid, out var grid))
                continue;

            count += Map.GetAllTiles(gridUid, grid).Count();
        }

        return count;
    }

    [PublicAPI]
    public EntityUid? GetOwningStation(EntityUid? entity, TransformComponent? xform = null)
    {
        if (entity == null)
            return null;

        return GetOwningStation(entity.Value, xform);
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

        if (TryComp<StationTrackerComponent>(entity, out var stationTracker))
        {
            // We have a specific station we are tracking and are tethered to.
            return stationTracker.Station;
        }

        if (HasComp<StationDataComponent>(entity))
        {
            // We are the station, just return ourselves.
            return entity;
        }

        if (HasComp<MapGridComponent>(entity))
        {
            // We are the station, just check ourselves.
            return CompOrNull<StationMemberComponent>(entity)?.Station;
        }

        if (xform.GridUid == EntityUid.Invalid)
        {
            Log.Debug("Unable to get owning station - GridUid invalid.");
            return null;
        }

        return CompOrNull<StationMemberComponent>(xform.GridUid)?.Station;
    }

    public List<Entity<StationDataComponent>> GetStations()
    {
        var stations = new List<Entity<StationDataComponent>>();
        var query = EntityQueryEnumerator<StationDataComponent>();
        while (query.MoveNext(out var uid, out var data))
        {
            stations.Add((uid, data));
        }

        return stations;
    }

    public HashSet<EntityUid> GetStationsSet()
    {
        var stations = new HashSet<EntityUid>();
        var query = EntityQueryEnumerator<StationDataComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            stations.Add(uid);
        }

        return stations;
    }

    public List<(string Name, NetEntity Entity)> GetStationNames()
    {
        var stations = GetStationsSet();
        var stats = new List<(string Name, NetEntity Station)>();

        foreach (var weh in stations)
        {
            stats.Add((MetaData(weh).EntityName, GetNetEntity(weh)));
        }

        return stats;
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
    /// Retrieves a filter for everything on the same map as a given station.
    /// </summary>
    public Filter GetOnMap(StationDataComponent dataComponent)
    {
        var filter = Filter.Empty();
        _mapIds.Clear();

        // First collect all valid map IDs where station grids exist
        foreach (var gridUid in dataComponent.Grids)
        {
            if (!TryComp(gridUid, out TransformComponent? xform))
                continue;

            var mapId = xform.MapID;
            if (!_mapIds.Contains(mapId))
                _mapIds.Add(mapId);
        }

        foreach (var session in Filter.GetAllPlayers(_player))
        {
            var entity = session.AttachedEntity;
            if (entity == null || !TryComp(entity, out TransformComponent? xform))
                continue;

            if (!_mapIds.Contains(xform.MapID))
                continue;

            filter.AddPlayer(session);
        }

        return filter;
    }

    /// <summary>
    /// Retrieves a filter for everything in a particular station or near its member grids.
    /// </summary>
    public Filter GetInStation(StationDataComponent dataComponent, float range = 32f)
    {
        var filter = Filter.Empty();
        _mapIds.Clear();

        // First collect all valid map IDs where station grids exist
        foreach (var gridUid in dataComponent.Grids)
        {
            if (!TryComp(gridUid, out TransformComponent? xform))
                continue;

            var mapId = xform.MapID;
            if (!_mapIds.Contains(mapId))
                _mapIds.Add(mapId);
        }

        // Cache the rotated bounds for each grid
        _gridBounds.Clear();

        foreach (var gridUid in dataComponent.Grids)
        {
            if (!GridQuery.TryComp(gridUid, out var grid) ||
                !TryComp(gridUid, out TransformComponent? gridXform))
            {
                continue;
            }

            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(gridXform);
            var localBounds = grid.LocalAABB.Enlarged(range);

            // Create a rotated box using the grid's transform
            var rotatedBounds = new Box2Rotated(
                localBounds,
                worldRot,
                worldPos);

            _gridBounds.Add((rotatedBounds, gridXform.MapID));
        }

        foreach (var session in Filter.GetAllPlayers(_player))
        {
            var entity = session.AttachedEntity;
            if (entity == null || !TryComp(entity, out TransformComponent? xform))
                continue;

            var mapId = xform.MapID;

            if (!_mapIds.Contains(mapId))
                continue;

            // Check if the player is directly on any station grid
            var gridUid = xform.GridUid;
            if (gridUid != null && dataComponent.Grids.Contains(gridUid.Value))
            {
                filter.AddPlayer(session);
                continue;
            }

            // If not directly on a grid, check against cached rotated bounds
            var position = _transform.GetWorldPosition(xform);

            foreach (var (bounds, boundsMapId) in _gridBounds)
            {
                // Skip bounds on different maps
                if (boundsMapId != mapId)
                    continue;

                if (!bounds.Contains(position))
                    continue;

                filter.AddPlayer(session);
                break;
            }
        }

        return filter;
    }

    /// <summary>
    /// Returns true if an entity's parent is the station, and false if the entity is not on the station
    /// </summary>
    /// <param name="entity">The entity to check if is on a grid</param>
    /// <param name="onlyCountLargestGrid">If true, only the largest grid of each station will count, so if the entity is on
    /// the ATS or a escape pod, it will return false</param>
    public bool IsOnStation(EntityUid entity, bool onlyCountLargestGrid = true)
    {
        var entityParent = Transform(entity).ParentUid;

        var query = EntityQueryEnumerator<StationDataComponent>();
        while (query.MoveNext(out var uid, out var stationDataComp))
        {
            if (onlyCountLargestGrid && GetLargestGrid(uid) == entityParent)
                return true;

            if (!onlyCountLargestGrid && stationDataComp.Grids.Contains(entityParent))
                return true;
        }

        return false;
    }
}
