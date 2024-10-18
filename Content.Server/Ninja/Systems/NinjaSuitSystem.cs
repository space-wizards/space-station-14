using Content.Server.Emp;
using Content.Server.Ninja.Events;
using Content.Server.Power.Components;
using Content.Server.PowerCell;
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

        SubscribeLocalEvent<NinjaSuitComponent, ContainerIsInsertingAttemptEvent>(OnSuitInsertAttempt);
        SubscribeLocalEvent<NinjaSuitComponent, EmpAttemptEvent>(OnEmpAttempt);
        SubscribeLocalEvent<NinjaSuitComponent, RecallKatanaEvent>(OnRecallKatana);
        SubscribeLocalEvent<NinjaSuitComponent, NinjaEmpEvent>(OnEmp);
    }

    protected override void NinjaEquipped(Entity<NinjaSuitComponent> ent, Entity<SpaceNinjaComponent> user)
    {
        base.NinjaEquipped(ent, user);

        _ninja.SetSuitPowerAlert(user);

        // raise event to let ninja components get starting battery
        _ninja.GetNinjaBattery(user.Owner, out var uid, out var _);

        if (uid is not {} battery_uid)
            return;

        var ev = new NinjaBatteryChangedEvent(battery_uid, ent.Owner);
        RaiseLocalEvent(ent, ref ev);
        RaiseLocalEvent(user, ref ev);
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
        RaiseLocalEvent(uid, ref ev);
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
