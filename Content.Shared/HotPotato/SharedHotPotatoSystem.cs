using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

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

        SubscribeLocalEvent<HotPotatoComponent, ComponentGetState>(GetCompState);
        SubscribeLocalEvent<HotPotatoComponent, ComponentHandleState>(HandleCompState);
        SubscribeLocalEvent<HotPotatoComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnRemoveAttempt(EntityUid uid, HotPotatoComponent comp, ContainerGettingRemovedAttemptEvent args)
    {
        if (!comp.CanTransfer)
            args.Cancel();
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

    private void OnMeleeHit(EntityUid uid, HotPotatoComponent comp, MeleeHitEvent args)
    {
        if (_net.IsClient)
            return;
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

    [Serializable, NetSerializable]
    public sealed class HotPotatoComponentState : ComponentState
    {
        public bool CanTransfer;
        
        public HotPotatoComponentState(bool canTransfer)
        {
            CanTransfer = canTransfer;
        }
    }
}