using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Placeable;

/// <summary>
/// Tracks placed entities
/// Subscribe to <see cref="ItemPlacedEvent"/> or <see cref="ItemRemovedEvent"/> to do things when items or placed or removed.
/// </summary>
public sealed class ItemPlacerSystem : EntitySystem
{
    [Dependency] private readonly PlaceableSurfaceSystem _placeableSurface = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemPlacerComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<ItemPlacerComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnStartCollide(EntityUid uid, ItemPlacerComponent comp, ref StartCollideEvent args)
    {
        if (comp.Whitelist != null && !comp.Whitelist.IsValid(args.OtherEntity))
            return;

        // Disallow sleeping so we can detect when entity is removed from the heater.
        _physics.SetSleepingAllowed(args.OtherEntity, args.OtherBody, false);

        var count = comp.PlacedEntities.Count;
        if (comp.MaxEntities == 0 || count < comp.MaxEntities)
        {
            comp.PlacedEntities.Add(args.OtherEntity);

            var ev = new ItemPlacedEvent(args.OtherEntity);
            RaiseLocalEvent(uid, ref ev);
        }

        if (comp.MaxEntities > 0 && count >= (comp.MaxEntities - 1))
        {
            // Don't let any more items be placed if it's reached its limit.
            _placeableSurface.SetPlaceable(uid, false);
        }
    }

    private void OnEndCollide(EntityUid uid, ItemPlacerComponent comp, ref EndCollideEvent args)
    {
        // Re-allow sleeping.
        _physics.SetSleepingAllowed(args.OtherEntity, args.OtherBody, true);

        comp.PlacedEntities.Remove(args.OtherEntity);

        var ev = new ItemRemovedEvent(args.OtherEntity);
        RaiseLocalEvent(uid, ref ev);

        _placeableSurface.SetPlaceable(uid, true);
    }
}

/// <summary>
/// Raised on the <see cref="ItemPlacer"/> when an item is placed and it is under the item limit.
/// </summary>
[ByRefEvent]
public readonly record struct ItemPlacedEvent(EntityUid OtherEntity);

/// <summary>
/// Raised on the <see cref="ItemPlacer"/> when an item is removed from it.
/// </summary>
[ByRefEvent]
public readonly record struct ItemRemovedEvent(EntityUid OtherEntity);
