using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.HotPotato;
using Robust.Shared.Network;

namespace Content.Server.HotPotato;

public sealed class HotPotatoSystem : SharedHotPotatoSystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HotPotatoComponent, ActiveTimerTriggerEvent>(OnActiveTimer);
        SubscribeLocalEvent<HotPotatoComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<HotPotatoComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnActiveTimer(EntityUid uid, HotPotatoComponent comp, ref ActiveTimerTriggerEvent args)
    {
        EnsureComp<ActiveHotPotatoComponent>(uid);
        comp.CanTransfer = false;
        Dirty(comp);
    }

    private void OnRemoveAttempt(EntityUid uid, HotPotatoComponent comp, ContainerGettingRemovedAttemptEvent args)
    {
        if (!comp.CanTransfer)
            args.Cancel();
    }

    private void OnMeleeHit(EntityUid uid, HotPotatoComponent comp, MeleeHitEvent args)
    {
        comp.CanTransfer = true;
        TryTransferItem(uid, args);
        comp.CanTransfer = !HasComp<ActiveHotPotatoComponent>(uid);
        Dirty(comp);
    }

    private void TryTransferItem(EntityUid uid, MeleeHitEvent args)
    {
        foreach (var hitEntity in args.HitEntities)
        {
            if (TryComp<SharedHandsComponent>(hitEntity, out var hands))
            {
                if (_hands.TryForcePickupAnyHand(hitEntity, uid, handsComp: hands))
                {
                    _popup.PopupEntity(Loc.GetString("hot-potato-passed", ("from", args.User), ("to", hitEntity)), uid);
                    return;
                }
            }
        }
    }
}
