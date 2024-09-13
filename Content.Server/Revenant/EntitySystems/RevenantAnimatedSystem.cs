using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Content.Server.Revenant.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;

namespace Content.Server.Revenant.EntitySystems;

public sealed class RevenantAnimatedSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantAnimatedComponent, MeleeHitEvent>(OnMeleeHit, before: [typeof(SharedCuffableSystem)]);
        SubscribeLocalEvent<RevenantAnimatedComponent, UserActivateInWorldEvent>(OnCuffInteract, before: [typeof(SharedCuffableSystem)]);
    }

    private void OnMeleeHit(EntityUid uid, RevenantAnimatedComponent comp, MeleeHitEvent args)
    {
        if (args.Handled)
            return;

        if (args.HitEntities.Count == 0)
            return;

        var hitEntity = args.HitEntities[0];

        // Handcuffs will attempt to jump into the victim's hands/pockets before trying to cuff them
        if (HasComp<HandcuffComponent>(uid) && HasComp<CuffableComponent>(hitEntity))
            TryJumpIntoSlots(uid, hitEntity);
    }

    private void OnCuffInteract(EntityUid uid, RevenantAnimatedComponent comp, UserActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<HandcuffComponent>(uid) && HasComp<CuffableComponent>(args.Target))
            TryJumpIntoSlots(uid, args.Target);
    }

    private void TryJumpIntoSlots(EntityUid uid, EntityUid target)
    {
        if (_container.ContainsEntity(target, uid))
            return;

        Log.Debug($"{uid} trying to jump into {target} pocket1");

        if (_inventory.TryGetSlotContainer(target, "pocket1", out var pocket1, out _)
            && _container.Insert(uid, pocket1)
        )
        {
            _popup.PopupEntity(Loc.GetString("item-jump-into-pocket", ("name", Comp<MetaDataComponent>(uid).EntityName)), target, target);
            return;
        }

        Log.Debug($"{uid} trying to jump into {target} pocket2");

        if (_inventory.TryGetSlotContainer(target, "pocket2", out var pocket2, out _)
            && _container.Insert(uid, pocket2)
        )
        {
            _popup.PopupEntity(Loc.GetString("item-jump-into-pocket", ("name", Comp<MetaDataComponent>(uid).EntityName)), target, target);
            return;
        }

        Log.Debug($"{uid} trying to jump into {target} hands");
        if (_hands.TryPickupAnyHand(target, uid))
        {
            _popup.PopupEntity(Loc.GetString("item-jump-into-hands", ("name", Comp<MetaDataComponent>(uid).EntityName)), target, target);
            return;
        }
    }
}