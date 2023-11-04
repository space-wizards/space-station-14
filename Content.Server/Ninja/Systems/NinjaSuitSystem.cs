using Content.Server.Emp;
using Content.Server.Ninja.Events;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Shared.Actions;
using Content.Shared.Clothing.EntitySystems;
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
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaSuitComponent, ContainerIsInsertingAttemptEvent>(OnSuitInsertAttempt);
        SubscribeLocalEvent<NinjaSuitComponent, EmpAttemptEvent>(OnEmpAttempt);
        SubscribeLocalEvent<NinjaSuitComponent, AttemptStealthEvent>(OnAttemptStealth);
        SubscribeLocalEvent<NinjaSuitComponent, CreateThrowingStarEvent>(OnCreateThrowingStar);
        SubscribeLocalEvent<NinjaSuitComponent, RecallKatanaEvent>(OnRecallKatana);
        SubscribeLocalEvent<NinjaSuitComponent, NinjaEmpEvent>(OnEmp);
    }

    protected override void NinjaEquippedSuit(EntityUid uid, NinjaSuitComponent comp, EntityUid user, SpaceNinjaComponent ninja)
    {
        base.NinjaEquippedSuit(uid, comp, user, ninja);

        _ninja.SetSuitPowerAlert(user);
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
        {
            args.Cancel();
        }

        // tell ninja abilities that use battery to update it so they don't use charge from the old one
        var user = Transform(uid).ParentUid;
        if (!HasComp<SpaceNinjaComponent>(user))
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

    protected override void UserUnequippedSuit(EntityUid uid, NinjaSuitComponent comp, EntityUid user)
    {
        base.UserUnequippedSuit(uid, comp, user);

        // remove power indicator
        _ninja.SetSuitPowerAlert(user);
    }

    private void OnAttemptStealth(EntityUid uid, NinjaSuitComponent comp, AttemptStealthEvent args)
    {
        var user = args.User;
        // need 1 second of charge to turn on stealth
        var chargeNeeded = SuitWattage(uid, comp);
        // being attacked while cloaked gives no power message since it overloads the power supply or something
        if (!_ninja.GetNinjaBattery(user, out var _, out var battery) || battery.CurrentCharge < chargeNeeded || UseDelay.ActiveDelay(user))
        {
            _popup.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            args.Cancel();
            return;
        }

        StealthClothing.SetEnabled(uid, user, true);
    }

    private void OnCreateThrowingStar(EntityUid uid, NinjaSuitComponent comp, CreateThrowingStarEvent args)
    {
        args.Handled = true;
        var user = args.Performer;
        if (!_ninja.TryUseCharge(user, comp.ThrowingStarCharge) || UseDelay.ActiveDelay(user))
        {
            _popup.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        // try to put throwing star in hand, otherwise it goes on the ground
        var star = Spawn(comp.ThrowingStarPrototype, Transform(user).Coordinates);
        _hands.TryPickupAnyHand(user, star);
    }

    private void OnRecallKatana(EntityUid uid, NinjaSuitComponent comp, RecallKatanaEvent args)
    {
        args.Handled = true;
        var user = args.Performer;
        if (!TryComp<SpaceNinjaComponent>(user, out var ninja) || ninja.Katana == null)
            return;

        var katana = ninja.Katana.Value;
        var coords = _transform.GetWorldPosition(katana);
        var distance = (_transform.GetWorldPosition(user) - coords).Length();
        var chargeNeeded = (float) distance * comp.RecallCharge;
        if (!_ninja.TryUseCharge(user, chargeNeeded) || UseDelay.ActiveDelay(user))
        {
            _popup.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        // TODO: teleporting into belt slot
        var message = _hands.TryPickupAnyHand(user, katana)
            ? "ninja-katana-recalled"
            : "ninja-hands-full";
        _popup.PopupEntity(Loc.GetString(message), user, user);
    }

    private void OnEmp(EntityUid uid, NinjaSuitComponent comp, NinjaEmpEvent args)
    {
        args.Handled = true;
        var user = args.Performer;
        if (!_ninja.TryUseCharge(user, comp.EmpCharge) || UseDelay.ActiveDelay(user))
        {
            _popup.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        // I don't think this affects the suit battery, but if it ever does in the future add a blacklist for it
        var coords = Transform(user).MapPosition;
        _emp.EmpPulse(coords, comp.EmpRange, comp.EmpConsumption, comp.EmpDuration);
    }
}
