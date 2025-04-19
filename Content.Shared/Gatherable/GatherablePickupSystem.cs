using Content.Shared.Inventory;
using Content.Shared.Storage.EntitySystems;

namespace Content.Shared.Gatherable;

/// <summary>
/// Tries to automatically insert gathered items into a storage.
/// Does nothing if it can't be inserted.
/// </summary>
public sealed class GatherablePickupSystem : EntitySystem
{
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GatherablePickupComponent, InventoryRelayedEvent<ItemGatheredEvent>>(OnGathered);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<GatherablePickupComponent>();
        while (query.MoveNext(out var comp))
        {
            comp.Gathered = false; // reset it for the next tick to play a sound
        }
    }

    private void OnGathered(Entity<GatherablePickupComponent> ent, ref InventoryRelayedEvent<ItemGatheredEvent> args)
    {
        if (!_storage.HasSpace(ent.Owner))
            return;

        var item = args.Args.Item;

        var xform = Transform(ent);
        var finalCoords = xform.Coordinates;
        var moverCoords = _transform.GetMoverCoordinates(ent, xform);

        var itemXform = Transform(item);
        var itemMap = _transform.GetMapCoordinates(item, xform: itemXform);
        var itemCoords = _transform.ToCoordinates(moverCoords.EntityId, itemMap);

        if (!_storage.Insert(ent, item, out var stacked, playSound: !ent.Comp.Gathered))
            return;

        ent.Comp.Gathered = true;

        var rot = itemXform.LocalRotation;
        if (stacked is {} stackedUid)
            _storage.PlayPickupAnimation(stackedUid, itemCoords, finalCoords, rot);
        else
            _storage.PlayPickupAnimation(item, itemCoords, finalCoords, rot);
    }
}
