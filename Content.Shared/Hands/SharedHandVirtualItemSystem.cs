using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Robust.Shared.Network;

namespace Content.Shared.Hands;

public abstract class SharedHandVirtualItemSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandVirtualItemComponent, BeingEquippedAttemptEvent>(OnBeingEquippedAttempt);
        SubscribeLocalEvent<HandVirtualItemComponent, BeforeRangedInteractEvent>(HandleBeforeInteract);
    }

    public bool TrySpawnVirtualItemInHand(EntityUid blockingEnt, EntityUid user)
    {
        return TrySpawnVirtualItemInHand(blockingEnt, user, out _);
    }

    public bool TrySpawnVirtualItemInHand(EntityUid blockingEnt, EntityUid user, [NotNullWhen(true)] out EntityUid? virtualItem)
    {
        if (_net.IsClient || !_hands.TryGetEmptyHand(user, out var hand))
        {
            virtualItem = null;
            return false;
        }

        var pos = Transform(user).Coordinates;
        virtualItem = Spawn("HandVirtualItem", pos);
        var virtualItemComp = EntityManager.GetComponent<HandVirtualItemComponent>(virtualItem.Value);
        virtualItemComp.BlockingEntity = blockingEnt;
        _hands.DoPickup(user, hand, virtualItem.Value);
        return true;
    }


    /// <summary>
    ///     Deletes all virtual items in a user's hands with
    ///     the specified blocked entity.
    /// </summary>
    public void DeleteInHandsMatching(EntityUid user, EntityUid matching)
    {
        // Client can't currently predict deleting network entities atm and this might happen due to the
        // hands leaving PVS for example, in which case we wish to ignore it.
        if (_net.IsClient)
            return;

        foreach (var hand in _hands.EnumerateHands(user))
        {
            if (TryComp(hand.HeldEntity, out HandVirtualItemComponent? virt) && virt.BlockingEntity == matching)
            {
                Delete(virt, user);
            }
        }
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
        if (_net.IsClient)
            return;

        var userEv = new VirtualItemDeletedEvent(comp.BlockingEntity, user);
        RaiseLocalEvent(user, userEv);
        var targEv = new VirtualItemDeletedEvent(comp.BlockingEntity, user);
        RaiseLocalEvent(comp.BlockingEntity, targEv);

        QueueDel(comp.Owner);
    }
}
