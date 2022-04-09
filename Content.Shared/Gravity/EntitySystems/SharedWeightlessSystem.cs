using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Shared.Gravity.EntitySystems;

[UsedImplicitly]
public abstract class SharedWeightlessSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    /// <summary>
    ///     List of mover-entities on a given grid. Required when the grid gains or looses gravity.
    /// </summary>
    public Dictionary<GridId, HashSet<EntityUid>> GridMovers = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MovementIgnoreGravityComponent, ComponentStartup>(OnIgnoreStartup);
        SubscribeLocalEvent<MovementIgnoreGravityComponent, ComponentShutdown>(OnIgnoreShutdown);

        SubscribeLocalEvent<IMoverComponent, ComponentStartup>(OnMoverStartup);
        SubscribeLocalEvent<IMoverComponent, ComponentShutdown>(OnMoverShutdown);
        SubscribeLocalEvent<IMoverComponent, ChangedGridEvent>(OnGridChanged);
        SubscribeLocalEvent<IMoverComponent, PhysicsBodyTypeChangedEvent>(OnBodyTypeChanged);

        SubscribeLocalEvent<GravityChangedMessage>(OnGravityChanged);
    }

    private void OnMoverStartup(EntityUid uid, IMoverComponent component, ComponentStartup args)
    {
        var xform = Transform(uid);
        UpdateMoverWeightlessness(uid, component, xform: xform);

        if (!xform.GridID.IsValid())
            return;

        if (GridMovers.TryGetValue(xform.GridID, out var set))
            set.Add(uid);
        else
            GridMovers[xform.GridID] = new() { uid };
    }

    private void OnMoverShutdown(EntityUid uid, IMoverComponent component, ComponentShutdown args)
    {
        var xform = Transform(uid);

        if (!GridMovers.TryGetValue(xform.GridID, out var set))
            return;

        set.Remove(uid);
        if (set.Count == 0)
            GridMovers.Remove(xform.GridID);
    }

    private void OnBodyTypeChanged(EntityUid uid, IMoverComponent component, PhysicsBodyTypeChangedEvent args)
    {
        UpdateMoverWeightlessness(uid, component);
    }

    private void OnIgnoreShutdown(EntityUid uid, MovementIgnoreGravityComponent component, ComponentShutdown args)
    {
        UpdateMoverWeightlessness(uid);
    }

    private void OnIgnoreStartup(EntityUid uid, MovementIgnoreGravityComponent component, ComponentStartup args)
    {
        if (!TryComp(uid, out IMoverComponent? mover))
            return;

        mover.Weightless = false;
        if (component is Component comp)
            Dirty(comp);
    }

    private void OnGravityChanged(GravityChangedMessage ev)
    {
        if (!GridMovers.TryGetValue(ev.ChangedGridIndex, out var movers))
            return;

        foreach (var mover in movers)
        {
            UpdateMoverWeightlessness(mover);
        }
    }

    private void OnGridChanged(EntityUid uid, IMoverComponent component, ChangedGridEvent args)
    {
        if (GridMovers.TryGetValue(args.OldGrid, out var old))
        {
            old.Remove(uid);
            if (old.Count == 0)
                GridMovers.Remove(args.OldGrid);
        }

        if (args.NewGrid.IsValid())
        {
            if (GridMovers.TryGetValue(args.NewGrid, out var set))
                set.Add(uid);
            else
                GridMovers[args.NewGrid] = new() { uid };
        }

        UpdateMoverWeightlessness(uid, component);
    }

    public void UpdateMoverWeightlessness(EntityUid uid, IMoverComponent? mover = null, PhysicsComponent? body = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref mover, false))
            return;

        mover.Weightless = IsWeightless(uid, out _, body, xform);
        if (mover is Component comp)
            Dirty(comp);
    }

    public bool IsWeightless(EntityUid uid, PhysicsComponent? body = null, TransformComponent? xform = null) => IsWeightless(uid, out _, body, xform);

    public bool IsWeightless(EntityUid uid, out TileRef? tile, PhysicsComponent? body = null, TransformComponent? xform = null)
    {
        tile = null;
        if (!Resolve(uid, ref xform))
            return true;

        if (Resolve(uid, ref body, false)
            && (body.BodyType & (BodyType.Static | BodyType.Kinematic)) != 0)
            return false;

        if (TryComp(uid, out MovementIgnoreGravityComponent? ignore) && ignore.LifeStage <= ComponentLifeStage.Running)
            return false;

        if (!_mapManager.TryGetGrid(xform.GridID, out var grid))
        {
            // Not on a grid = no gravity for now.
            // In the future, may want to allow maps to override to always have gravity instead.
            return true;
        }

        // Is there a tile at the players location?
        tile = grid.GetTileRef(xform.Coordinates);
        if (tile.Value.Tile.IsEmpty)
            return true;

        if (Comp<GravityComponent>(grid.GridEntityId).Enabled)
            return false;

        // Gravity is disabled, but entity may have magboots
        if (_inventorySystem.TryGetSlotEntity(uid, "shoes", out var ent))
        {
            if (TryComp<SharedMagbootsComponent>(ent, out var boots) && boots.On)
                return false;
        }

        return true;
    }
}
