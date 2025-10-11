using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Projectiles;
using Content.Shared.Timing;
using Robust.Shared.Containers;
using Robust.Shared.Map;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// This system implements <see cref="QuickPickupComponent"/>'s behavior.
/// </summary>
public sealed partial class QuickPickupSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedProjectileSystem _projectile = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    private EntityQuery<ItemComponent> _itemQuery;

    private const string DelayId = "quickPickup";

    public override void Initialize()
    {
        _itemQuery = GetEntityQuery<ItemComponent>();

        SubscribeLocalEvent<QuickPickupComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<QuickPickupComponent, AfterInteractEvent>(
            AfterInteract,
            // Prioritize picking up a single item rather than trying to area pickup "through" an item.
            before: [typeof(AreaPickupSystem)]
        );
    }

    private void OnMapInit(Entity<QuickPickupComponent> entity, ref MapInitEvent args)
    {
        _useDelay.SetLength(entity.Owner, entity.Comp.Cooldown, DelayId);
    }

    private void AfterInteract(Entity<QuickPickupComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled ||
            args.Target is not { Valid: true } target ||
            target == args.User ||
            !args.CanReach ||
            !_useDelay.TryResetDelay(entity, checkDelayed: true, id: DelayId) ||
            _container.IsEntityInContainer(target) ||
            !_itemQuery.TryComp(target, out var targetItemComp))
            return;

        // Let other systems decide if they want to try to pick up the item.
        var ev = new QuickPickupEvent(entity, (target, targetItemComp), args.User);
        RaiseLocalEvent(entity, ev);
        args.Handled = ev.Handled;
    }

    /// <summary>
    /// This function helps with handling <see cref="QuickPickupEvent"/> by handling
    /// <see cref="AnimateInsertingEntitiesEvent">animating picked up entities</see> while also invoking
    /// <paramref name="tryPickup"/>. Returns true if the entity in <paramref name="ev"/> was actually picked up and the
    /// event should be considered handled, false otherwise.
    /// </summary>
    public bool TryDoQuickPickup(QuickPickupEvent ev, Func<bool> tryPickup)
    {
        if (!TryComp(ev.QuickPickupEntity, out TransformComponent? pickupEntityXform) ||
            !TryComp(ev.PickedUp, out TransformComponent? targetXform))
            return false;

        if (TryComp<EmbeddableProjectileComponent>(ev.PickedUp, out var embeddable))
        {
            _projectile.EmbedDetach(ev.PickedUp, embeddable, ev.User);
        }

        // Get the picked up entity's position _before_ inserting it, because that changes its position.
        var position = _transform.ToCoordinates(
            pickupEntityXform.ParentUid.IsValid() ? pickupEntityXform.ParentUid : ev.QuickPickupEntity.Owner,
            _transform.GetMapCoordinates(targetXform)
        );

        if (!tryPickup())
            return false;

        // Animate the item getting picked up.
        EntityManager.RaiseSharedEvent(
            new AnimateInsertingEntitiesEvent(
                GetNetEntity(ev.QuickPickupEntity),
                new List<NetEntity> { GetNetEntity(ev.PickedUp) },
                new List<NetCoordinates> { GetNetCoordinates(position) },
                new List<Angle> { pickupEntityXform.LocalRotation }
            ),
            ev.User
        );
        return true;

    }
}
