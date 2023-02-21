using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Content.Server.Explosion.EntitySystems;

namespace Content.Server.HotPotato;

public sealed class HotPotatoSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HotPotatoComponent, ActiveTimerTriggerEvent>(OnActiveTimer);
        SubscribeLocalEvent<HotPotatoComponent, MeleeHitEvent>(TransferItem);
        SubscribeLocalEvent<HotPotatoComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
    }

    private void OnActiveTimer(EntityUid uid, HotPotatoComponent comp, ActiveTimerTriggerEvent args)
    {
        comp.Activated = true;
    }

    private void TransferItem(EntityUid uid, HotPotatoComponent comp, MeleeHitEvent args)
    {
        comp.CanTransfer = true;
        TryTransferItem(uid, args);
        comp.CanTransfer = !comp.Activated;
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