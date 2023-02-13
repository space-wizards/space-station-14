using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.Doors.Systems;
using Content.Server.Electrocution;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Components;
using Content.Server.Ninja.Components;
using Content.Server.Objectives;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Server.Traitor;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Roles;
using Content.Shared.Rounding;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Ninja.Systems;

public sealed partial class NinjaSystem : GameRuleSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    private readonly HashSet<SpaceNinjaComponent> _activeNinja = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceNinjaGlovesComponent, GotEquippedEvent>(OnGlovesEquipped);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, GotUnequippedEvent>(OnGlovesUnequipped);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, NinjaDoorjackEvent>(OnDoorjackAction);
        // for doorjack counting
        SubscribeLocalEvent<DoorComponent, DoorEmaggedEvent>(OnDoorEmagged);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, NinjaStunEvent>(OnStunAction);

        // TODO: maybe have suit activation stuff
        SubscribeLocalEvent<SpaceNinjaSuitComponent, GotEquippedEvent>(OnSuitEquipped);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, ContainerIsInsertingAttemptEvent>(OnSuitInsertAttempt);
        // TODO: enable if it causes trouble
        // SubscribeLocalEvent<SpaceNinjaSuitComponent, ContainerIsRemovingAttemptEvent>(OnSuitRemoveAttempt);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, GotUnequippedEvent>(OnSuitUnequipped);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, TogglePhaseCloakEvent>(OnTogglePhaseCloakAction);

        SubscribeLocalEvent<SpaceNinjaComponent, ComponentStartup>(OnNinjaStartup);
        SubscribeLocalEvent<SpaceNinjaComponent, MindAddedMessage>(OnNinjaMindAdded);
        SubscribeLocalEvent<SpaceNinjaComponent, AttackedEvent>(OnNinjaAttacked);
        SubscribeLocalEvent<SpaceNinjaComponent, ComponentRemove>(OnNinjaRemoved);
    }

    public override void Update(float frameTime)
    {
        var toRemove = new RemQueue<SpaceNinjaComponent>();

        foreach (var ninja in _activeNinja)
        {
            if (ninja.Deleted)
            {
                toRemove.Add(ninja);
                continue;
            }

            if (Paused(ninja.Owner))
                continue;

            UpdateNinja(ninja, frameTime);
        }

        foreach (var ninja in toRemove)
        {
            _activeNinja.Remove(ninja);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _activeNinja.Clear();
    }

    private void OnGlovesEquipped(EntityUid uid, SpaceNinjaGlovesComponent comp, GotEquippedEvent args)
    {
        var user = args.Equipee;
        if (IsNinja(user) && TryComp<ActionsComponent>(user, out var actions))
        {
            _actions.AddAction(user, comp.DoorjackAction, uid, actions);
            _actions.AddAction(user, comp.StunAction, uid, actions);
            // TODO: power drain ability
        }
    }

    private void OnGlovesUnequipped(EntityUid uid, SpaceNinjaGlovesComponent comp, GotUnequippedEvent args)
    {
        _actions.RemoveProvidedActions(args.Equipee, uid);
    }

    // stripped down version of EmagSystem's emagging code, only working on doors
    private void OnDoorjackAction(EntityUid uid, SpaceNinjaGlovesComponent comp, NinjaDoorjackEvent args)
    {
        var target = args.Target;
        if (_tags.HasTag(target, comp.EmagImmuneTag))
            return;

        if (!HasComp<DoorComponent>(target))
            return;

        var user = args.Performer;
        var handled = _emag.DoEmagEffect(user, target);
        if (!handled)
            return;

        _popups.PopupEntity(Loc.GetString("emag-success", ("target", Identity.Entity(target, EntityManager))), user,
            user, PopupType.Medium);
        args.Handled = true;

        _adminLogger.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(user):player} doorjacked {ToPrettyString(target):target}");
    }

    private void OnDoorEmagged(EntityUid uid, DoorComponent door, DoorEmaggedEvent args)
    {
        // make sure it's a ninja doorjacking it
        if (TryComp<SpaceNinjaComponent>(args.UserUid, out var ninja))
            ninja.DoorsJacked++;
    }

    private void OnStunAction(EntityUid uid, SpaceNinjaGlovesComponent comp, NinjaStunEvent args)
    {
        var target = args.Target;
        var user = args.Performer;

        // only target things that can be stunned, which excludes yourself
        if (user == target || !HasComp<StaminaComponent>(target))
            return;

        // take charge from battery
        if (!GetNinjaBattery(user, out var battery) || !battery.TryUseCharge(comp.StunCharge))
        {
            _popups.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        // not holding hands with target so insuls don't matter
        args.Handled = _electrocution.TryDoElectrocution(target, comp.Owner, comp.StunDamage, comp.StunTime, false, ignoreInsulation: true);
    }

    private void OnSuitEquipped(EntityUid uid, SpaceNinjaSuitComponent comp, GotEquippedEvent args)
    {
        var user = args.Equipee;
        if (TryComp<SpaceNinjaComponent>(user, out var ninja) && TryComp<ActionsComponent>(user, out var actions))
        {
            _actions.AddAction(user, comp.TogglePhaseCloakAction, uid, actions);
            // TODO: emp ability

            // mark the user as wearing this suit, used when being attacked
            ninja.Suit = uid;

            // initialize phase cloak
            AddComp<StealthComponent>(user);
            SetCloaked(user, comp.Cloaked);
	        SetSuitPowerAlert(user);
        }
    }

    // TODO: put in shared so client properly predicts insertion
    private void OnSuitInsertAttempt(EntityUid uid, SpaceNinjaSuitComponent comp, ContainerIsInsertingAttemptEvent args)
    {
        if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery))
        {
            // no power cell for some reason??? allow it
            return;
        }

        // can only upgrade power cell, not swap to recharge instantly otherwise ninja could just swap batteries with flashlights in maints for easy power
        if (!TryComp<BatteryComponent>(args.EntityUid, out var inserting) || inserting.MaxCharge <= battery.MaxCharge)
        {
            args.Cancel();
        }
    }
/*
    private void OnSuitRemoveAttempt(EntityUid uid, SpaceNinjaSuitComponent comp, ContainerIsRemovingAttemptEvent args)
    {
        // ejecting cell then putting in charger would bypass glove recharging, bad
        args.Cancel();
    }
*/
    private void OnSuitUnequipped(EntityUid uid, SpaceNinjaSuitComponent comp, GotUnequippedEvent args)
    {
        var user = args.Equipee;
        _actions.RemoveProvidedActions(user, uid);

        // mark the user as not wearing a suit
        if (TryComp<SpaceNinjaComponent>(user, out var ninja))
        {
            ninja.Suit = null;
        }

        // force uncloak
        comp.Cloaked = false;
        RemComp<StealthComponent>(user);
        SetSuitPowerAlert(user);
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

    private void SetCloaked(EntityUid user, bool cloaked)
    {
        if (TryComp<StealthComponent>(user, out var stealth))
        {
            // slightly visible, but doesn't change when moving so it's ok
            var visibility = cloaked ? stealth.MinVisibility + 0.25f : stealth.MaxVisibility;
            _stealth.SetVisibility(user, visibility, stealth);

            _stealth.SetEnabled(user, cloaked, stealth);
        }
    }

    private void OnNinjaStartup(EntityUid uid, SpaceNinjaComponent comp, ComponentStartup args)
    {
        _activeNinja.Add(comp);
    }

    private void OnNinjaMindAdded(EntityUid uid, SpaceNinjaComponent comp, MindAddedMessage args)
    {
        // Mind was added, shutdown the ghost role stuff so it won't get in the way
//        if (EntityManager.HasComponent<GhostTakeoverAvailableComponent>(uid))
//            EntityManager.RemoveComponent<GhostTakeoverAvailableComponent>(uid);

        // TODO: put in yaml somehow
        if (TryComp<MindComponent>(uid, out var mind) && mind.Mind != null && mind.Mind.TryGetSession(out var session))
        {
            mind.Mind.AddRole(new TraitorRole(mind.Mind, _proto.Index<AntagPrototype>(comp.SpaceNinjaRoleId)));
            if (_proto.TryIndex<ObjectivePrototype>("DoorjackObjective", out var objective))
                mind.Mind.TryAddObjective(objective);

            _chatMan.DispatchServerMessage(session, Loc.GetString("ninja-role-greeting"));
        }
    }

    private void OnNinjaAttacked(EntityUid uid, SpaceNinjaComponent comp, AttackedEvent args)
    {
        if (comp.Suit != null && TryComp<SpaceNinjaSuitComponent>(comp.Suit, out var suit))
        {
            if (suit.Cloaked)
            {
                suit.Cloaked = false;
                SetCloaked(uid, false);
                // TODO: disable all actions for 5 seconds
            }
        }
    }

    private void OnNinjaRemoved(EntityUid uid, SpaceNinjaComponent comp, ComponentRemove args)
    {
        _activeNinja.Remove(comp);
    }

    private void UpdateNinja(SpaceNinjaComponent ninja, float frameTime)
    {
        if (ninja.Suit == null || !TryComp<SpaceNinjaSuitComponent>(ninja.Suit.Value, out var suit))
            return;

        float wattage = SuitWattage(suit);

		SetSuitPowerAlert(ninja.Owner, ninja);
        if (!GetNinjaBattery(ninja.Owner, out var battery) || !battery.TryUseCharge(wattage * frameTime))
        {
            // ran out of power, reveal ninja
            if (suit.Cloaked)
            {
                suit.Cloaked = false;
                SetCloaked(ninja.Owner, false);
            }
        }
    }

	private void SetSuitPowerAlert(EntityUid uid, SpaceNinjaComponent? comp = null)
	{
        if (!Resolve(uid, ref comp, false) || comp.Deleted || comp.Suit == null)
        {
            _alerts.ClearAlert(uid, AlertType.SuitPower);
            return;
        }

		if (GetNinjaBattery(uid, out var battery))
		{
 	        var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, battery.CurrentCharge), battery.MaxCharge, 7);
	        _alerts.ShowAlert(uid, AlertType.SuitPower, (short) severity);
        }
        else
        {
            _alerts.ClearAlert(uid, AlertType.SuitPower);
        }
	}

    private bool IsNinja(EntityUid user)
    {
        return HasComp<SpaceNinjaComponent>(user);
    }

    private bool GetNinjaBattery(EntityUid user, [NotNullWhen(true)] out BatteryComponent? battery)
    {
        if (TryComp<SpaceNinjaComponent>(user, out var ninja)
            && ninja.Suit != null
            && _powerCell.TryGetBatteryFromSlot(ninja.Suit.Value, out battery))
        {
            return true;
        }

        battery = null;
        return false;
    }

    private float SuitWattage(SpaceNinjaSuitComponent suit)
    {
        float wattage = suit.PassiveWattage;
        if (suit.Cloaked)
            wattage += suit.CloakWattage;
        return wattage;
    }
}
