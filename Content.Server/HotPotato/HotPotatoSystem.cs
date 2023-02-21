using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Hands;
using Content.Server.Explosion.Components;
using Robust.Shared.Containers;

namespace Content.Server.HotPotato;

public sealed class HotPotatoSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HotPotatoComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HotPotatoComponent, GotEquippedHandEvent>(OnGotEquipped);
        SubscribeLocalEvent<HotPotatoComponent, MeleeHitEvent>(TransferItem);
        SubscribeLocalEvent<HotPotatoComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
    }

    private void OnUseInHand(EntityUid uid, HotPotatoComponent comp, UseInHandEvent args)
    {
        comp.CanTransfer = false;
    }

    private void OnGotEquipped(EntityUid uid, HotPotatoComponent comp, GotEquippedHandEvent args)
    {
        if (HasComp<ActiveTimerTriggerComponent>(uid))
            comp.CanTransfer = false;
    }

    private void TransferItem(EntityUid uid, HotPotatoComponent comp, MeleeHitEvent args)
    {
        comp.CanTransfer = true;
        TryTransferItem(uid, args);
        comp.CanTransfer = false;
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