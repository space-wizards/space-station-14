using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.HotPotato;

namespace Content.Server.HotPotato;

public sealed class HotPotatoSystem : SharedHotPotatoSystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HotPotatoComponent, MeleeHitEvent>(TransferItem);
        SubscribeLocalEvent<HotPotatoComponent, ActiveTimerTriggerEvent>(OnActiveTimer);
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

    private void OnActiveTimer(EntityUid uid, HotPotatoComponent comp, ActiveTimerTriggerEvent args)
    {
        EnsureComp<ActiveHotPotatoComponent>(uid);
        comp.CanTransfer = false;
        Dirty(comp);
    }
}
