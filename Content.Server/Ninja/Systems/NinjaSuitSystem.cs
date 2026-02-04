using Content.Server.Ninja.Events;
using Content.Shared.Emp;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Containers;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Handles power cell upgrading and actions.
/// TODO: Move all of this to shared and predict it
/// </summary>
public sealed class NinjaSuitSystem : SharedNinjaSuitSystem
{
    [Dependency] private readonly SharedEmpSystem _emp = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SpaceNinjaSystem _ninja = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // How much the cell score should be increased per 1 AutoRechargeRate.
    private const int AutoRechargeValue = 100;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaSuitComponent, ContainerIsInsertingAttemptEvent>(OnSuitInsertAttempt);
        SubscribeLocalEvent<NinjaSuitComponent, RecallKatanaEvent>(OnRecallKatana);
        SubscribeLocalEvent<NinjaSuitComponent, NinjaEmpEvent>(OnEmp);
    }

    protected override void NinjaEquipped(Entity<NinjaSuitComponent> ent, Entity<SpaceNinjaComponent> user)
    {
        base.NinjaEquipped(ent, user);

        _ninja.SetSuitPowerAlert(user);

        // raise event to let ninja components get starting battery
        _ninja.GetNinjaBattery(user.Owner, out var uid, out var _);

        if (uid is not { } battery_uid)
            return;

        var ev = new NinjaBatteryChangedEvent(battery_uid, ent.Owner);
        RaiseLocalEvent(ent, ref ev);
        RaiseLocalEvent(user, ref ev);
    }

    private void OnSuitInsertAttempt(EntityUid uid, NinjaSuitComponent comp, ContainerIsInsertingAttemptEvent args)
    {
        // this is for handling battery upgrading, not stopping actions from being added
        // if another container like ActionsContainer is specified, don't handle it
        if (TryComp<PowerCellSlotComponent>(uid, out var slot) && args.Container.ID != slot.CellSlotId)
            return;

        // no power cell for some reason??? allow it
        if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            return;

        if (!TryComp<BatteryComponent>(args.EntityUid, out var inserting))
        {
            args.Cancel();
            return;
        }

        var user = Transform(uid).ParentUid;

        // can only upgrade power cell, not swap to recharge instantly otherwise ninja could just swap batteries with flashlights in maints for easy power
        if (GetCellScore(args.EntityUid, inserting) <= GetCellScore(battery.Value, battery.Value))
        {
            args.Cancel();
            Popup.PopupEntity(Loc.GetString("ninja-cell-downgrade"), user, user);
            return;
        }

        // tell ninja abilities that use battery to update it so they don't use charge from the old one
        if (!_ninja.IsNinja(user))
            return;

        var ev = new NinjaBatteryChangedEvent(args.EntityUid, uid);
        RaiseLocalEvent(uid, ref ev);
        RaiseLocalEvent(user, ref ev);
    }

    // this function assigns a score to a power cell depending on the capacity, to be used when comparing which cell is better.
    private float GetCellScore(EntityUid uid, BatteryComponent battcomp)
    {
        // if a cell is able to automatically recharge, boost the score drastically depending on the recharge rate,
        // this is to ensure a ninja can still upgrade to a micro reactor cell even if they already have a medium or high.
        if (TryComp<BatterySelfRechargerComponent>(uid, out var selfcomp))
            return battcomp.MaxCharge + selfcomp.AutoRechargeRate * AutoRechargeValue;
        return battcomp.MaxCharge;
    }

    protected override void UserUnequippedSuit(Entity<NinjaSuitComponent> ent, Entity<SpaceNinjaComponent> user)
    {
        base.UserUnequippedSuit(ent, user);

        // remove power indicator
        _ninja.SetSuitPowerAlert(user);
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

        _emp.EmpPulse(Transform(user).Coordinates, comp.EmpRange, comp.EmpConsumption, comp.EmpDuration, user);
    }
}
