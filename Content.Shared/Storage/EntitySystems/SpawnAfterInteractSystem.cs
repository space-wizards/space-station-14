using Content.Shared.Storage.Components;
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

namespace Content.Shared.Storage.EntitySystems;

[UsedImplicitly]
public sealed partial class SpawnAfterInteractSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private SharedStackSystem _stackSystem = default!;
    [Dependency] private TurfSystem _turfSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _maps = default!;

    [SubscribeLocalEvent]
    private void OnInteract(Entity<SpawnAfterInteractComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach && !ent.Comp.IgnoreDistance)
            return;

        var gridUid = _transform.GetGrid(args.ClickLocation);

        if (!TryComp<MapGridComponent>(gridUid, out var grid) ||
            !_maps.TryGetTileRef(gridUid.Value, grid, args.ClickLocation, out var tileRef) ||
            tileRef.Tile.IsEmpty ||
            _turfSystem.IsTileBlocked(tileRef, CollisionGroup.MobMask))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.DoAfterTime,
            new SpawnAfterInteractEvent(GetNetCoordinates(args.ClickLocation.SnapToGrid(grid))),
            ent,
            used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
        };
        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    [SubscribeLocalEvent]
    private void AfterDoafter(Entity<SpawnAfterInteractComponent> ent, ref SpawnAfterInteractEvent args)
    {
        if (args.Cancelled ||
            TryComp<StackComponent>(ent, out var stackComp)
            && ent.Comp.RemoveOnInteract && !_stackSystem.TryUse((ent, stackComp), 1))
        {
            return;
        }

        PredictedSpawnAtPosition(ent.Comp.Prototype, GetCoordinates(args.Coordinates));

        if (ent.Comp.RemoveOnInteract && stackComp == null)
            PredictedQueueDel(ent);
    }
}

[Serializable, NetSerializable]
public sealed partial class SpawnAfterInteractEvent : SimpleDoAfterEvent
{
    [DataField(required:true)]
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
