using System.Linq;
using Content.Shared.Station.Components;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Shared.Station;

public abstract partial class SharedStationSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<StationMemberComponent> _stationMemberQuery;
    private EntityQuery<MapGridComponent> _gridQuery;

    private ValueList<MapId> _mapIds;
    private ValueList<(Box2Rotated Bounds, MapId MapId)> _gridBounds;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeTracker();

        _xformQuery = GetEntityQuery<TransformComponent>();
        _stationMemberQuery = GetEntityQuery<StationMemberComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
    }

    /// <summary>
    /// Gets the largest member grid from a station.
    /// </summary>
    public EntityUid? GetLargestGrid(Entity<StationDataComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        EntityUid? largestGrid = null;
        Box2 largestBounds = new Box2();

        foreach (var gridUid in ent.Comp.Grids)
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

            count += _map.GetAllTiles(gridUid, grid).Count();
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

    public List<EntityUid> GetStations()
    {
        var stations = new List<EntityUid>();
        var query = EntityQueryEnumerator<StationDataComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            stations.Add(uid);
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
    /// Retrieves a filter for everything in a particular station or near its member grids.
    /// </summary>
    public Filter GetInStation(StationDataComponent dataComponent, float range = 32f)
    {
        var filter = Filter.Empty();
        _mapIds.Clear();

        // First collect all valid map IDs where station grids exist
        foreach (var gridUid in dataComponent.Grids)
        {
            if (!_xformQuery.TryGetComponent(gridUid, out var xform))
                continue;

            var mapId = xform.MapID;
            if (!_mapIds.Contains(mapId))
                _mapIds.Add(mapId);
        }

        // Cache the rotated bounds for each grid
        _gridBounds.Clear();

        foreach (var gridUid in dataComponent.Grids)
        {
            if (!_gridQuery.TryComp(gridUid, out var grid) ||
                !_xformQuery.TryGetComponent(gridUid, out var gridXform))
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
            if (entity == null || !_xformQuery.TryGetComponent(entity, out var xform))
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
}
