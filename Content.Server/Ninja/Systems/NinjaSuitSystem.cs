using Content.Server.Emp;
using Content.Server.Ninja.Events;
using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Shared.Clothing;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Containers;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Handles power cell upgrading and actions.
/// </summary>
public sealed class NinjaSuitSystem : SharedNinjaSuitSystem
{
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SpaceNinjaSystem _ninja = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaSuitComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NinjaSuitComponent, ContainerIsInsertingAttemptEvent>(OnSuitInsertAttempt);
        SubscribeLocalEvent<NinjaSuitComponent, EmpAttemptEvent>(OnEmpAttempt);
        SubscribeLocalEvent<NinjaSuitComponent, CreateThrowingStarEvent>(OnCreateThrowingStar);
        SubscribeLocalEvent<NinjaSuitComponent, RecallKatanaEvent>(OnRecallKatana);
        SubscribeLocalEvent<NinjaSuitComponent, NinjaEmpEvent>(OnEmp);
    }

    private void OnEquipped(Entity<NinjaSuitComponent> ent, ref ClothingGotEquippedEvent args)
    {
        var user = args.Wearer;
        if (!_ninja.NinjaQuery.TryComp(user, out var ninja));
            return;

        _ninja.SetSuitPowerAlert((user, ninja));
        // mark the user as wearing this suit, used when being attacked among other things
        _ninja.AssignSuit((user, ninja), ent);
    }

    // TODO: if/when battery is in shared, put this there too
    // TODO: or put MaxCharge in shared along with powercellslot
    private void OnSuitInsertAttempt(EntityUid uid, NinjaSuitComponent comp, ContainerIsInsertingAttemptEvent args)
    {
        // this is for handling battery upgrading, not stopping actions from being added
        // if another container like ActionsContainer is specified, don't handle it
        if (TryComp<PowerCellSlotComponent>(uid, out var slot) && args.Container.ID != slot.CellSlotId)
            return;

        // no power cell for some reason??? allow it
        if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            return;

        // can only upgrade power cell, not swap to recharge instantly otherwise ninja could just swap batteries with flashlights in maints for easy power
        if (!TryComp<BatteryComponent>(args.EntityUid, out var inserting) || inserting.MaxCharge <= battery.MaxCharge)
            args.Cancel();

        // tell ninja abilities that use battery to update it so they don't use charge from the old one
        var user = Transform(uid).ParentUid;
        if (!_ninja.IsNinja(user))
            return;

        var ev = new NinjaBatteryChangedEvent(args.EntityUid, uid);
        RaiseLocalEvent(user, ref ev);
    }

    private void OnEmpAttempt(EntityUid uid, NinjaSuitComponent comp, EmpAttemptEvent args)
    {
        // ninja suit (battery) is immune to emp
        // powercell relays the event to suit
        args.Cancel();
    }

    protected override void UserUnequippedSuit(Entity<NinjaSuitComponent> ent, Entity<SpaceNinjaComponent> user)
    {
        base.UserUnequippedSuit(ent, user);

        // remove power indicator
        _ninja.SetSuitPowerAlert(user);
    }

    private void OnCreateThrowingStar(Entity<NinjaSuitComponent> ent, ref CreateThrowingStarEvent args)
    {
        var (uid, comp) = ent;
        args.Handled = true;

        var user = args.Performer;
        if (!_ninja.TryUseCharge(user, comp.ThrowingStarCharge))
        {
            Popup.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        if (CheckDisabled(ent, user))
            return;

        // try to put throwing star in hand, otherwise it goes on the ground
        var star = Spawn(comp.ThrowingStarPrototype, Transform(user).Coordinates);
        _hands.TryPickupAnyHand(user, star);
    }

    private void OnRecallKatana(Entity<NinjaSuitComponent> ent, ref RecallKatanaEvent args)
    {
        var (uid, comp) = ent;
        var user = args.Performer;
        if (!_ninja.NinjaQuery.TryComp(user, out var ninja) || ninja.Katana == null)
            return;

        args.Handled = true;

        var katana = ninja.Katana.Value;
        var coords = _transform.GetWorldPosition(katana);
        var distance = (_transform.GetWorldPosition(user) - coords).Length();
        var chargeNeeded = distance * comp.RecallCharge;
        if (!_ninja.TryUseCharge(user, chargeNeeded))
        {
            Popup.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        if (CheckDisabled(ent, user))
            return;

        // TODO: teleporting into belt slot
        var message = _hands.TryPickupAnyHand(user, katana)
            ? "ninja-katana-recalled"
            : "ninja-hands-full";
        Popup.PopupEntity(Loc.GetString(message), user, user);
    }

    private void OnEmp(Entity<NinjaSuitComponent> ent, ref NinjaEmpEvent args)
    {
        var (uid, comp) = ent;
        args.Handled = true;

        var user = args.Performer;
        if (!_ninja.TryUseCharge(user, comp.EmpCharge))
        {
            Popup.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        if (CheckDisabled(ent, user))
            return;

        var coords = _transform.GetMapCoordinates(user);
        _emp.EmpPulse(coords, comp.EmpRange, comp.EmpConsumption, comp.EmpDuration);
    }
}
