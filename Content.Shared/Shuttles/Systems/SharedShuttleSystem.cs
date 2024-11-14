using Content.Shared.Containers.ItemSlots;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.UI.MapObjects;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Shuttles.Systems;

public abstract partial class SharedShuttleSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] protected readonly SharedMapSystem Maps = default!;
    [Dependency] protected readonly SharedTransformSystem XformSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public const float FTLRange = 256f;
    public const float FTLBufferRange = 8f;

    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private List<Entity<MapGridComponent>> _grids = new();

    public override void Initialize()
    {
        base.Initialize();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    /// <summary>
    /// Returns whether an entity can FTL to the specified map.
    /// </summary>
    public bool CanFTLTo(EntityUid shuttleUid, MapId targetMap, EntityUid consoleUid)
    {
        var mapUid = _mapManager.GetMapEntityId(targetMap);
        var shuttleMap = _xformQuery.GetComponent(shuttleUid).MapID;

        if (shuttleMap == targetMap)
            return true;

        if (!TryComp<FTLDestinationComponent>(mapUid, out var destination) || !destination.Enabled)
            return false;

        if (destination.RequireCoordinateDisk)
        {
            if (!TryComp<ItemSlotsComponent>(consoleUid, out var slot))
            {
                return false;
            }

            if (!_itemSlots.TryGetSlot(consoleUid, SharedShuttleConsoleComponent.DiskSlotName, out var itemSlot, component: slot) || !itemSlot.HasItem)
            {
                return false;
            }

            if (itemSlot.Item is { Valid: true } disk)
            {
                ShuttleDestinationCoordinatesComponent? diskCoordinates = null;
                if (!Resolve(disk, ref diskCoordinates))
                {
                    return false;
                }

                var diskCoords = diskCoordinates.Destination;

                if (diskCoords == null || !TryComp<FTLDestinationComponent>(diskCoords.Value, out var diskDestination) || diskDestination != destination)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        if (HasComp<FTLMapComponent>(mapUid))
            return false;

        return _whitelistSystem.IsWhitelistPassOrNull(destination.Whitelist, shuttleUid);
    }

    /// <summary>
    /// Gets the list of map objects relevant for the specified map.
    /// </summary>
    public IEnumerable<(ShuttleExclusionObject Exclusion, MapCoordinates Coordinates)> GetExclusions(MapId mapId, List<ShuttleExclusionObject> exclusions)
    {
        foreach (var exc in exclusions)
        {
            var beaconCoords = XformSystem.ToMapCoordinates(GetCoordinates(exc.Coordinates));

            if (beaconCoords.MapId != mapId)
                continue;

            yield return (exc, beaconCoords);
        }
    }

    /// <summary>
    /// Gets the list of map objects relevant for the specified map.
    /// </summary>
    public IEnumerable<(ShuttleBeaconObject Beacon, MapCoordinates Coordinates)> GetBeacons(MapId mapId, List<ShuttleBeaconObject> beacons)
    {
        foreach (var beacon in beacons)
        {
            var beaconCoords = XformSystem.ToMapCoordinates(GetCoordinates(beacon.Coordinates));

            if (beaconCoords.MapId != mapId)
                continue;

            yield return (beacon, beaconCoords);
        }
    }

    public bool CanDraw(EntityUid gridUid, PhysicsComponent? physics = null, IFFComponent? iffComp = null)
    {
        if (!Resolve(gridUid, ref physics))
            return true;

        if (physics.BodyType != BodyType.Static && physics.Mass < 10f)
        {
            return false;
        }

        if (!Resolve(gridUid, ref iffComp, false))
        {
            return true;
        }

        // Hide it entirely.
        return (iffComp.Flags & IFFFlags.Hide) == 0x0;
    }

    public bool IsBeaconMap(EntityUid mapUid)
    {
        return TryComp(mapUid, out FTLDestinationComponent? ftlDest) && ftlDest.BeaconsOnly;
    }

    /// <summary>
    /// Returns true if a beacon can be FTLd to.
    /// </summary>
    public bool CanFTLBeacon(NetCoordinates nCoordinates)
    {
        // Only beacons parented to map supported.
        var coordinates = GetCoordinates(nCoordinates);
        return HasComp<MapComponent>(coordinates.EntityId);
    }

    public float GetFTLRange(EntityUid shuttleUid) => FTLRange;

    public float GetFTLBufferRange(EntityUid shuttleUid, MapGridComponent? grid = null)
    {
        if (!_gridQuery.Resolve(shuttleUid, ref grid))
            return 0f;

        var localAABB = grid.LocalAABB;
        var maxExtent = localAABB.MaxDimension / 2f;
        var range = maxExtent + FTLBufferRange;
        return range;
    }

    /// <summary>
    /// Returns true if the spot is free to be FTLd to (not close to any objects and in range).
    /// </summary>
    public bool FTLFree(EntityUid shuttleUid, EntityCoordinates coordinates, Angle angle, List<ShuttleExclusionObject>? exclusionZones)
    {
        if (!_physicsQuery.TryGetComponent(shuttleUid, out var shuttlePhysics) ||
            !_xformQuery.TryGetComponent(shuttleUid, out var shuttleXform))
        {
            return false;
        }

        // Just checks if any grids inside of a buffer range at the target position.
        _grids.Clear();
        var mapCoordinates = coordinates.ToMap(EntityManager, XformSystem);

        var ourPos = Maps.GetGridPosition((shuttleUid, shuttlePhysics, shuttleXform));

        // This is the already adjusted position
        var targetPosition = mapCoordinates.Position;

        // Check range even if it's cross-map.
        if ((targetPosition - ourPos).Length() > FTLRange)
        {
            return false;
        }

        // Check exclusion zones.
        // This needs to be passed in manually due to PVS.
        if (exclusionZones != null)
        {
            foreach (var exclusion in exclusionZones)
            {
                var exclusionCoords = XformSystem.ToMapCoordinates(GetCoordinates(exclusion.Coordinates));

                if (exclusionCoords.MapId != mapCoordinates.MapId)
                    continue;

                if ((mapCoordinates.Position - exclusionCoords.Position).Length() <= exclusion.Range)
                    return false;
            }
        }

        var ourFTLBuffer = GetFTLBufferRange(shuttleUid);
        var circle = new PhysShapeCircle(ourFTLBuffer + FTLBufferRange, targetPosition);

        _mapManager.FindGridsIntersecting(mapCoordinates.MapId, circle, Robust.Shared.Physics.Transform.Empty,
            ref _grids, includeMap: false);

        // If any grids in range that aren't us then can't FTL.
        foreach (var grid in _grids)
        {
            if (grid.Owner == shuttleUid)
                continue;

            return false;
        }

        return true;
    }
}

[Flags]
public enum FTLState : byte
{
    Invalid = 0,

    /// <summary>
    /// A dummy state for presentation
    /// </summary>
    Available = 1 << 0,

    /// <summary>
    /// Sound played and launch started
    /// </summary>
    Starting = 1 << 1,

    /// <summary>
    /// When they're on the FTL map
    /// </summary>
    Travelling = 1 << 2,

    /// <summary>
    /// Approaching destination, play effects or whatever,
    /// </summary>
    Arriving = 1 << 3,
    Cooldown = 1 << 4,
}

