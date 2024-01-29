using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Shuttles.Systems;

public abstract partial class SharedShuttleSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;

    public const float FTLRange = 512f;
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
    public bool FTLFree(EntityUid shuttleUid, EntityCoordinates coordinates, Angle angle)
    {
        if (!_physicsQuery.TryGetComponent(shuttleUid, out var shuttlePhysics) ||
            !_xformQuery.TryGetComponent(shuttleUid, out var shuttleXform))
        {
            return false;
        }

        // Just checks if any grids inside of a buffer range at the target position.
        _grids.Clear();
        var ftlRange = FTLRange;
        var mapCoordinates = coordinates.ToMap(EntityManager, _xformSystem);

        var ourPos = _maps.GetGridPosition((shuttleUid, shuttlePhysics, shuttleXform));

        // This is the already adjusted position
        var targetPosition = mapCoordinates.Position;

        // If it's a cross-map FTL no range limit.
        if (mapCoordinates.MapId == shuttleXform.MapID)
        {
            if ((targetPosition - ourPos).Length() > FTLRange)
            {
                return false;
            }
        }

        var ourFTLBuffer = GetFTLBufferRange(shuttleUid);
        var circle = new PhysShapeCircle(ourFTLBuffer + FTLBufferRange, targetPosition);

        _mapManager.FindGridsIntersecting(mapCoordinates.MapId, circle, Robust.Shared.Physics.Transform.Empty,
            ref _grids, includeMap: false);

        return _grids.Count == 0;
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

