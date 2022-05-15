using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;

namespace Content.Shared.Hands;

public abstract class SharedHandVirtualItemSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandVirtualItemComponent, BeingEquippedAttemptEvent>(OnBeingEquippedAttempt);
        SubscribeLocalEvent<HandVirtualItemComponent, BeforeRangedInteractEvent>(HandleBeforeInteract);
    }

    private void OnBeingEquippedAttempt(EntityUid uid, HandVirtualItemComponent component, BeingEquippedAttemptEvent args)
    {
        args.Cancel();
    }

    private static void HandleBeforeInteract(
        EntityUid uid,
        HandVirtualItemComponent component,
        BeforeRangedInteractEvent args)
    {
        // No interactions with a virtual item, please.
        args.Handled = true;
    }

    /// <summary>
    ///     Queues a deletion for a virtual item and notifies the blocking entity and user.
    /// </summary>
    public void Delete(HandVirtualItemComponent comp, EntityUid user)
    {
        var userEv = new VirtualItemDeletedEvent(comp.BlockingEntity, user);
        RaiseLocalEvent(user, userEv, false);
        var targEv = new VirtualItemDeletedEvent(comp.BlockingEntity, user);
        RaiseLocalEvent(comp.BlockingEntity, targEv, false);

        EntityManager.QueueDeleteEntity(comp.Owner);
    }
}
