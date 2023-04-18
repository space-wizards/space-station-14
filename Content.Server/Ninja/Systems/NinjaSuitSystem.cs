using Content.Server.Emp;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Server.Ninja.Systems;

public sealed class NinjaSuitSystem : SharedNinjaSuitSystem
{
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly new NinjaSystem _ninja = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        // TODO: maybe have suit activation stuff
        SubscribeLocalEvent<NinjaSuitComponent, ContainerIsInsertingAttemptEvent>(OnSuitInsertAttempt);
        SubscribeLocalEvent<NinjaSuitComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NinjaSuitComponent, TogglePhaseCloakEvent>(OnTogglePhaseCloak);
        SubscribeLocalEvent<NinjaSuitComponent, CreateSoapEvent>(OnCreateSoap);
        SubscribeLocalEvent<NinjaSuitComponent, RecallKatanaEvent>(OnRecallKatana);
        SubscribeLocalEvent<NinjaSuitComponent, NinjaEmpEvent>(OnEmp);
    }

    protected override void NinjaEquippedSuit(EntityUid uid, NinjaSuitComponent comp, EntityUid user, NinjaComponent ninja)
    {
        base.NinjaEquippedSuit(uid, comp, user, ninja);

        _ninja.SetSuitPowerAlert(user);
    }

    // TODO: if/when battery is in shared, put this there too
    private void OnSuitInsertAttempt(EntityUid uid, NinjaSuitComponent comp, ContainerIsInsertingAttemptEvent args)
    {
        // no power cell for some reason??? allow it
        if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            return;

        // can only upgrade power cell, not swap to recharge instantly otherwise ninja could just swap batteries with flashlights in maints for easy power
        if (!TryComp<BatteryComponent>(args.EntityUid, out var inserting) || inserting.MaxCharge <= battery.MaxCharge)
        {
            args.Cancel();
        }
    }

    private void OnExamined(EntityUid uid, NinjaSuitComponent comp, ExaminedEvent args)
    {
        // TODO: make this also return the uid of the battery
        if (_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            RaiseLocalEvent(battery.Owner, args);
    }

    protected override void UserUnequippedSuit(EntityUid uid, NinjaSuitComponent comp, EntityUid user)
    {
        base.UserUnequippedSuit(uid, comp, user);

        // remove power indicator
        _ninja.SetSuitPowerAlert(user);
    }

    private void OnTogglePhaseCloak(EntityUid uid, NinjaSuitComponent comp, TogglePhaseCloakEvent args)
    {
        args.Handled = true;
        var user = args.Performer;
        // need 1 second of charge to turn on stealth
        var chargeNeeded = SuitWattage(comp);
        if (!comp.Cloaked && (!_ninja.GetNinjaBattery(user, out var battery) || battery.CurrentCharge < chargeNeeded || _useDelay.ActiveDelay(uid)))
        {
            _popups.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        comp.Cloaked = !comp.Cloaked;
        SetCloaked(args.Performer, comp.Cloaked);
        RaiseNetworkEvent(new SetCloakedMessage()
        {
            User = user,
            Cloaked = comp.Cloaked
        });
    }

    private void OnCreateSoap(EntityUid uid, NinjaSuitComponent comp, CreateSoapEvent args)
    {
        args.Handled = true;
        var user = args.Performer;
        if (!_ninja.TryUseCharge(user, comp.SoapCharge) || _useDelay.ActiveDelay(uid))
        {
            _popups.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        // try to put soap in hand, otherwise it goes on the ground
        var soap = Spawn(comp.SoapPrototype, Transform(user).Coordinates);
        _hands.TryPickupAnyHand(user, soap);
    }

    private void OnRecallKatana(EntityUid uid, NinjaSuitComponent comp, RecallKatanaEvent args)
    {
        args.Handled = true;
        var user = args.Performer;
        if (!TryComp<NinjaComponent>(user, out var ninja) || ninja.Katana == null)
            return;

        // 1% charge per tile
        var katana = ninja.Katana.Value;
        var coords = _transform.GetWorldPosition(katana);
        var distance = (_transform.GetWorldPosition(user) - coords).Length;
        var chargeNeeded = (float) distance * 3.6f;
        if (!_ninja.TryUseCharge(user, chargeNeeded) || _useDelay.ActiveDelay(uid))
        {
            _popups.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        // TODO: teleporting into belt slot
        var message = _hands.TryPickupAnyHand(user, katana)
            ? "ninja-katana-recalled"
            : "ninja-hands-full";
        _popups.PopupEntity(Loc.GetString(message), user, user);
    }

    private void OnEmp(EntityUid uid, NinjaSuitComponent comp, NinjaEmpEvent args)
    {
        args.Handled = true;
        var user = args.Performer;
        if (!_ninja.TryUseCharge(user, comp.EmpCharge) || _useDelay.ActiveDelay(uid))
        {
            _popups.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        // I don't think this affects the suit battery, but if it ever does in the future add a blacklist for it
        var coords = Transform(user).MapPosition;
        _emp.EmpPulse(coords, comp.EmpRange, comp.EmpConsumption);
    }
}
