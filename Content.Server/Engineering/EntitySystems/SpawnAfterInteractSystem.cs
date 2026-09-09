using Content.Shared.Storage.Components;
using Content.Server.Stack;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization;

namespace Content.Server.Engineering.EntitySystems;

[UsedImplicitly]
public sealed partial class SpawnAfterInteractSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private StackSystem _stackSystem = default!;
    [Dependency] private TurfSystem _turfSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _maps = default!;

    [SubscribeLocalEvent]
    private void OnInteract(Entity<SpawnAfterInteractComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach && !ent.Comp.IgnoreDistance)
            return;

        if (string.IsNullOrEmpty(ent.Comp.Prototype))
            return;

        var gridUid = _transform.GetGrid(args.ClickLocation);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        if (!_maps.TryGetTileRef(gridUid.Value, grid, args.ClickLocation, out var tileRef))
            return;

        if (!IsTileClear())
            return;

        if (ent.Comp.DoAfterTime <= 0)
        {
            var ev = new SpawnAfterInteractEvent(args.ClickLocation.SnapToGrid(grid));
            RaiseLocalEvent(ent, ev);
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.DoAfterTime, new SpawnAfterInteractEvent(args.ClickLocation.SnapToGrid(grid)), ent, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
        };
        _doAfterSystem.TryStartDoAfter(doAfterArgs);

        return;

        bool IsTileClear()
        {
            return !tileRef.Tile.IsEmpty && !_turfSystem.IsTileBlocked(tileRef, CollisionGroup.MobMask);
        }
    }

    [SubscribeLocalEvent]
    private void AfterDoafter(Entity<SpawnAfterInteractComponent> ent, ref SpawnAfterInteractEvent args)
    {
        if (TryComp<StackComponent>(ent, out var stackComp)
            && ent.Comp.RemoveOnInteract && !_stackSystem.TryUse((ent, stackComp), 1))
        {
            return;
        }

        PredictedSpawnAtPosition(ent.Comp.Prototype, args.Coordinates);

        if (ent.Comp.RemoveOnInteract && stackComp == null)
            PredictedQueueDel(ent);
    }
}

[Serializable, NetSerializable]
public sealed partial class SpawnAfterInteractEvent : SimpleDoAfterEvent
{
    [DataField(required:true)]
    public EntityCoordinates Coordinates;

    private SpawnAfterInteractEvent()
    {
    }

    public SpawnAfterInteractEvent(EntityCoordinates coordinates)
    {
        Coordinates = coordinates;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
