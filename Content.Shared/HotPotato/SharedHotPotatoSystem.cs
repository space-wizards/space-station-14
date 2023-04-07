using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared.HotPotato;

public abstract class SharedHotPotatoSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HotPotatoComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<HotPotatoComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<HotPotatoComponent, ComponentGetState>(GetCompState);
        SubscribeLocalEvent<HotPotatoComponent, ComponentHandleState>(HandleCompState);
    }

    private void OnRemoveAttempt(EntityUid uid, HotPotatoComponent comp, ContainerGettingRemovedAttemptEvent args)
    {
        if (!comp.CanTransfer)
            args.Cancel();
    }

    private void OnMeleeHit(EntityUid uid, HotPotatoComponent comp, MeleeHitEvent args)
    {
        if (!HasComp<ActiveHotPotatoComponent>(uid))
            return;

        comp.CanTransfer = true;
        foreach (var hitEntity in args.HitEntities)
        {
            if (!TryComp<HandsComponent>(hitEntity, out var hands))
                continue;

            if (_hands.TryForcePickupAnyHand(hitEntity, uid, handsComp: hands) && _net.IsServer)
            {
                _popup.PopupEntity(Loc.GetString("hot-potato-passed",
                    ("from", args.User), ("to", hitEntity)), uid, PopupType.Medium);
                break;
            }

            if (_net.IsServer)
            {
                _popup.PopupEntity(Loc.GetString("hot-potato-failed",
                    ("to", hitEntity)), uid, PopupType.Medium);
            }

            break;
        }
        comp.CanTransfer = false;
        Dirty(comp);
    }

    private void GetCompState(EntityUid uid, HotPotatoComponent comp, ref ComponentGetState args)
    {
        args.State = new HotPotatoComponentState(comp.CanTransfer);
    }

    private void HandleCompState(EntityUid uid, HotPotatoComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not HotPotatoComponentState state)
            return;

        comp.CanTransfer = state.CanTransfer;
    }
}
