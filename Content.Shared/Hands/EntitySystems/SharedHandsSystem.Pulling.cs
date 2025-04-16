using Content.Shared.Hands.Components;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Robust.Shared.Utility;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    private void InitializePulling()
    {
        SubscribeLocalEvent<HandsComponent, PullStartedMessage>(HandlePullStarted);
        SubscribeLocalEvent<HandsComponent, PullStoppedMessage>(HandlePullStopped);
    }

    private void HandlePullStarted(EntityUid uid, HandsComponent component, PullStartedMessage args)
    {
        if (args.PullerUid != uid)
            return;

        if (TryComp<PullerComponent>(args.PullerUid, out var pullerComp) && !pullerComp.NeedsHands)
            return;

        if (!VirtualSystem.TrySpawnVirtualItemInHand(args.PulledUid, uid))
        {
            DebugTools.Assert("Unable to find available hand when starting pulling??");
        }
    }

    private void HandlePullStopped(EntityUid uid, HandsComponent component, PullStoppedMessage args)
    {
        if (args.PullerUid != uid)
            return;

        // Try find hand that is doing this pull.
        // and clear it.
        foreach (var hand in component.Hands.Values)
        {
            if (hand.HeldEntity == null
                || !TryComp(hand.HeldEntity, out VirtualItemComponent? virtualItem)
                || virtualItem.BlockingEntity != args.PulledUid)
            {
                continue;
            }

            TryDrop(args.PullerUid, hand, handsComp: component);
            break;
        }
    }
}
