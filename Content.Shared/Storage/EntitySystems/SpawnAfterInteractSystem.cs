using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage.EntitySystems;

[UsedImplicitly]
public sealed partial class SpawnAfterInteractSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    [SubscribeLocalEvent]
    private void OnInteract(Entity<SpawnAfterInteractComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach && !ent.Comp.IgnoreDistance)
            return;

        if (!CanSpawn(args.ClickLocation))
            return;

        var gridUid = _transform.GetGrid(args.ClickLocation);
        if (gridUid == null)
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.DoAfterTime,
            new SpawnAfterInteractEvent(GetNetCoordinates(args.ClickLocation.SnapToGrid())),
            ent,
            used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<SpawnAfterInteractComponent> ent, ref SpawnAfterInteractEvent args)
    {
        if (args.Cancelled)
            return;

        var coords = GetCoordinates(args.Coordinates);
        if (!CanSpawn(coords))
            return;

        if (ent.Comp.RemoveOnInteract && !_stack.TryUse(ent.Owner, 1))
            return;

        PredictedSpawnAtPosition(ent.Comp.Prototype, coords);

        if (ent.Comp.RemoveOnInteract && !HasComp<StackComponent>(ent))
            PredictedQueueDel(ent);
    }

    private bool CanSpawn(EntityCoordinates coords)
    {
        var gridUid = _transform.GetGrid(coords);
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        if (!_maps.TryGetTileRef(gridUid.Value, grid, coords, out var tileRef))
            return false;

        return !tileRef.Tile.IsEmpty && !_turf.IsTileBlocked(tileRef, CollisionGroup.MobMask);
    }
}

[Serializable, NetSerializable]
public sealed partial class SpawnAfterInteractEvent : DoAfterEvent
{
    [DataField(required: true)]
    public NetCoordinates Coordinates;

    private SpawnAfterInteractEvent()
    {
    }

    public SpawnAfterInteractEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
