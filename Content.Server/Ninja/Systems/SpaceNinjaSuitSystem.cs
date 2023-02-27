using Content.Server.Actions;
using Content.Shared.Ninja.Components;

namespace Content.Server.Ninja.Systems;

public sealed class SpaceNinjaSuitSystem : SharedSpaceNinjaSuitSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
	[Dependency] private readonly SpaceNinjaSystem _ninja = default!;

    public override void Initialize()
    {
        base.Initialize();

        // TODO: maybe have suit activation stuff
        SubscribeLocalEvent<SpaceNinjaSuitComponent, ContainerIsInsertingAttemptEvent>(OnSuitInsertAttempt);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, TogglePhaseCloakEvent>(OnTogglePhaseCloakAction);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, RecallKatanaEvent>(OnRecallKatanaAction);
    }

	/// <summary>
	/// Force uncloak the user and disable suit abilities for 5 seconds.
	/// </summary>
	public void Attacked(SpaceNinjaSuitComponent comp, EntityUid user)
	{
        if (comp.Cloaked)
        {
            comp.Cloaked = false;
            SetCloaked(user, false);
            // TODO: disable all actions for 5 seconds
            // TODO: add the box open thing its funny
        }
	}

    protected override void NinjaEquippedSuit(EntityUid uid, SpaceNinjaSuitComponent comp, EntityUid user, SpaceNinjaComponent ninja)
    {
        base.NinjaEquippedSuit(uid, comp, user, ninja);

        _ninja.SetSuitPowerAlert(user);

        if (!TryComp<ActionsComponent>(user, out var actions))
            return;

        _actions.AddAction(user, comp.TogglePhaseCloakAction, uid, actions);
        _actions.AddAction(user, comp.RecallKatanaAction, uid, actions);
        _actions.AddAction(user, comp.KatanaDashAction, uid, actions);
        // TODO: emp ability
        // TODO: ninja star ability
    }

    // TODO: put in shared so client properly predicts insertion, but it uses powercell so how???
    private void OnSuitInsertAttempt(EntityUid uid, SpaceNinjaSuitComponent comp, ContainerIsInsertingAttemptEvent args)
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

    protected override void UserUnequippedSuit(EntityUid uid, SpaceNinjaSuitComponent comp, EntityUid user)
    {
        base.UserUnequippedSuit(uid, comp, user);

        // remove suit ability actions
        _actions.RemoveProvidedActions(user, uid);

        // remove power indicator
        _ninja.SetSuitPowerAlert(user);
    }

    private void OnTogglePhaseCloakAction(EntityUid uid, SpaceNinjaSuitComponent comp, TogglePhaseCloakEvent args)
    {
        args.Handled = true;
        var user = args.Performer;
        // need 1 second of charge to turn on stealth
        var chargeNeeded = SuitWattage(comp);
        if (!comp.Cloaked && (!GetNinjaBattery(user, out var battery) || battery.CurrentCharge < chargeNeeded))
        {
            _popups.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        comp.Cloaked = !comp.Cloaked;
        SetCloaked(args.Performer, comp.Cloaked);
    }

    private void OnRecallKatanaAction(EntityUid uid, SpaceNinjaSuitComponent comp, RecallKatanaEvent args)
    {
        args.Handled = true;
        var user = args.Performer;
        if (!TryComp<SpaceNinjaComponent>(user, out var ninja) || ninja.Katana == null)
            return;

        // 1% charge per tile
        var katana = ninja.Katana.Value;
        var coords = _transform.GetWorldPosition(katana);
        var distance = (_transform.GetWorldPosition(user) - coords).Length;
        var chargeNeeded = (float) distance * 3.6f;
        if (!GetNinjaBattery(user, out var battery) || !battery.TryUseCharge(chargeNeeded))
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

