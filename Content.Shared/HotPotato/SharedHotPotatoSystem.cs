using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;

namespace Content.Shared.HotPotato;

public abstract class SharedHotPotatoSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HotPotatoComponent, MeleeHitEvent>(TransferItem);
        SubscribeLocalEvent<HotPotatoComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
    }

    private void TransferItem(EntityUid uid, HotPotatoComponent comp, MeleeHitEvent args)
    {
        comp.CanTransfer = true;
        TryTransferItem(uid, args);
        comp.CanTransfer = !HasComp<ActiveHotPotatoComponent>(uid);
    }

    private void TryTransferItem(EntityUid uid, MeleeHitEvent args)
    {
        foreach (var hitEntity in args.HitEntities) {
            if (TryComp<SharedHandsComponent>(hitEntity, out var hands)) 
            {
                if (_hands.TryPickupAnyHand(hitEntity, uid))
                    return;
                foreach (var hand in hands.Hands.Values)
                    if (_hands.TryDrop(hitEntity, hand) && _hands.TryPickup(hitEntity, uid, hand.Name))
                        return;
            }
        }
    }

    private void OnRemoveAttempt(EntityUid uid, HotPotatoComponent comp, ContainerGettingRemovedAttemptEvent args)
    {
        if (!comp.CanTransfer)
            args.Cancel();
    }
}