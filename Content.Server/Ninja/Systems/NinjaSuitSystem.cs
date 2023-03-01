using Content.Server.Actions;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Server.Ninja.Systems;

public sealed class NinjaSuitSystem : SharedNinjaSuitSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
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
        SubscribeLocalEvent<NinjaSuitComponent, TogglePhaseCloakEvent>(OnTogglePhaseCloakAction);
        SubscribeLocalEvent<NinjaSuitComponent, RecallKatanaEvent>(OnRecallKatanaAction);
    }

    /// <summary>
    /// Force uncloak the user, does not disable suit abilities.
    /// </summary>
    public void RevealNinja(NinjaSuitComponent comp, EntityUid user)
    {
        if (comp.Cloaked)
        {
            comp.Cloaked = false;
            SetCloaked(user, false);
            // TODO: add the box open thing its funny
        }
    }

    /// <summary>
    /// Returns the power used by a suit
    /// </summary>
    public float SuitWattage(NinjaSuitComponent suit)
    {
        float wattage = suit.PassiveWattage;
        if (suit.Cloaked)
            wattage += suit.CloakWattage;
        return wattage;
    }

    protected override void NinjaEquippedSuit(EntityUid uid, NinjaSuitComponent comp, EntityUid user, NinjaComponent ninja)
    {
        base.NinjaEquippedSuit(uid, comp, user, ninja);

        _ninja.SetSuitPowerAlert(user);

        if (!TryComp<ActionsComponent>(user, out var actions))
            return;

		// TODO: do actions like with gloves are now
        _actions.AddAction(user, comp.TogglePhaseCloakAction, uid, actions);
        _actions.AddAction(user, comp.RecallKatanaAction, uid, actions);
        _actions.AddAction(user, comp.KatanaDashAction, uid, actions);
        // TODO: emp ability
        // TODO: ninja star ability
    }

    // TODO: put in shared so client properly predicts insertion, but it uses powercell so how???
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

    protected override void UserUnequippedSuit(EntityUid uid, NinjaSuitComponent comp, EntityUid user)
    {
        base.UserUnequippedSuit(uid, comp, user);

        // remove suit ability actions
        _actions.RemoveProvidedActions(user, uid);

        // remove power indicator
        _ninja.SetSuitPowerAlert(user);
    }

    private void OnTogglePhaseCloakAction(EntityUid uid, NinjaSuitComponent comp, TogglePhaseCloakEvent args)
    {
        args.Handled = true;
        var user = args.Performer;
        // need 1 second of charge to turn on stealth
        var chargeNeeded = SuitWattage(comp);
        if (!comp.Cloaked && (!_ninja.GetNinjaBattery(user, out var battery) || battery.CurrentCharge < chargeNeeded))
        {
            _popups.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        comp.Cloaked = !comp.Cloaked;
        SetCloaked(args.Performer, comp.Cloaked);
    }

    private void OnRecallKatanaAction(EntityUid uid, NinjaSuitComponent comp, RecallKatanaEvent args)
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
        if (!_ninja.GetNinjaBattery(user, out var battery) || !battery.TryUseCharge(chargeNeeded))
        {
            _popups.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        // TODO: teleporting into belt slot
        var message = _hands.TryPickup(user, katana)
            ? "ninja-katana-recalled"
            : "ninja-hands-full";
        _popups.PopupEntity(Loc.GetString(message), user, user);
    }
}
