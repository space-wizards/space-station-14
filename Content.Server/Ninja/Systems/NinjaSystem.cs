using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.DoAfter;
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
using Content.Server.Warps;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Research.Components;
using Content.Shared.Roles;
using Content.Shared.Rounding;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Content.Server.Ninja.Systems;

public sealed partial class NinjaSystem : GameRuleSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DoAfterSystem _doafter = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;
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
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, NinjaDrainEvent>(OnDrainAction);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, DrainSuccessEvent>(OnDrainSuccess);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, DrainCancelledEvent>(OnDrainCancel);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, NinjaDownloadEvent>(OnDownloadAction);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, DownloadSuccessEvent>(OnDownloadSuccess);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, DownloadCancelledEvent>(OnDownloadCancel);

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
            // TODO: add glove action / glove toggle for using with left click (ideally right click but yeah)
            _actions.AddAction(user, comp.DoorjackAction, uid, actions);
            _actions.AddAction(user, comp.StunAction, uid, actions);
            _actions.AddAction(user, comp.DrainAction, uid, actions);
            _actions.AddAction(user, comp.DownloadAction, uid, actions);
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

    private void OnDrainAction(EntityUid uid, SpaceNinjaGlovesComponent comp, NinjaDrainEvent args)
    {
        var target = args.Target;
        var user = args.Performer;

        // only target devices that store power
        if (HasComp<PowerNetworkBatteryComponent>(target))
        {
            if (HasComp<BatteryComponent>(target))
            {
                if (comp.DrainCancelToken != null)
                {
                    comp.DrainCancelToken.Cancel();
                    return;
                }

                comp.DrainCancelToken = new CancellationTokenSource();
                var doafterArgs = new DoAfterEventArgs(user, comp.DrainTime, comp.DrainCancelToken.Token, used: uid)
                {
                    BreakOnDamage = true,
                    BreakOnStun = true,
                    BreakOnUserMove = true,
                    MovementThreshold = 0.5f,
                    UsedCancelledEvent = new DrainCancelledEvent(),
                    UsedFinishedEvent = new DrainSuccessEvent(user, target)
                };

                _doafter.DoAfter(doafterArgs);
                args.Handled = true;
            }
        }
    }

    private void OnDrainSuccess(EntityUid uid, SpaceNinjaGlovesComponent comp, DrainSuccessEvent args)
    {
        var user = args.User;
        var target = args.Battery;

        comp.DrainCancelToken = null;
        if (!GetNinjaBattery(user, out var suitBattery))
            // took suit off or something, ignore draining
            return;

        if (suitBattery.IsFullyCharged)
        {
            _popups.PopupEntity(Loc.GetString("ninja-drain-full"), user, user, PopupType.Medium);
            return;
        }

        if (TryComp<BatteryComponent>(target, out var battery) && TryComp<PowerNetworkBatteryComponent>(target, out var pnb))
        {
            if (MathHelper.CloseToPercent(battery.CurrentCharge, 0))
            {
                _popups.PopupEntity(Loc.GetString("ninja-drain-empty", ("battery", target)), user, user, PopupType.Medium);
                return;
            }

            // TODO: sparks, sound
            // higher tier storages can charge more
            var available = battery.CurrentCharge;
            var required = suitBattery.MaxCharge - suitBattery.CurrentCharge;
            var maxDrained = pnb.MaxSupply * comp.DrainTime;
            var input = Math.Min(Math.Min(available, required / comp.DrainEfficiency), maxDrained);
            if (battery.TryUseCharge(input))
            {
                var output = input * comp.DrainEfficiency;
                suitBattery.CurrentCharge += output;
                _popups.PopupEntity(Loc.GetString("ninja-drain-success", ("battery", target)), user, user);
            }
        }
    }

    private void OnDrainCancel(EntityUid uid, SpaceNinjaGlovesComponent comp, DrainCancelledEvent args)
    {
        comp.DrainCancelToken = null;
    }

    private void OnDownloadAction(EntityUid uid, SpaceNinjaGlovesComponent comp, NinjaDownloadEvent args)
    {
        var target = args.Target;
        var user = args.Performer;

        // only target research servers that have unlocks
        if (TryComp<TechnologyDatabaseComponent>(target, out var database))
        {
            if (comp.DownloadCancelToken != null)
            {
                comp.DownloadCancelToken.Cancel();
                return;
            }

            // fail fast if theres no tech right now
            if (database.TechnologyIds.Count == 0)
            {
                _popups.PopupEntity(Loc.GetString("ninja-download-fail"), user, user);
                return;
            }

            comp.DownloadCancelToken = new CancellationTokenSource();
            var doafterArgs = new DoAfterEventArgs(user, comp.DownloadTime, comp.DownloadCancelToken.Token, used: uid)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnUserMove = true,
                MovementThreshold = 0.5f,
                UsedCancelledEvent = new DownloadCancelledEvent(),
                UsedFinishedEvent = new DownloadSuccessEvent(user, target)
            };

            _doafter.DoAfter(doafterArgs);
            args.Handled = true;
        }
    }

    private void OnDownloadSuccess(EntityUid uid, SpaceNinjaGlovesComponent comp, DownloadSuccessEvent args)
    {
        var user = args.User;
        var target = args.Server;

        if (!TryComp<SpaceNinjaComponent>(user, out var ninja))
            return;

        comp.DownloadCancelToken = null;

        if (TryComp<TechnologyDatabaseComponent>(target, out var database))
        {
            var oldCount = ninja.DownloadedNodes.Count;
            ninja.DownloadedNodes.UnionWith(database.TechnologyIds);
            var newCount = ninja.DownloadedNodes.Count;

            var gained = newCount - oldCount;
            var str = gained == 0
                ? Loc.GetString("ninja-download-fail")
                : Loc.GetString("ninja-download-success", ("count", gained), ("server", target));

            _popups.PopupEntity(str, user, user, PopupType.Medium);
        }
    }

    private void OnDownloadCancel(EntityUid uid, SpaceNinjaGlovesComponent comp, DownloadCancelledEvent args)
    {
        comp.DownloadCancelToken = null;
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

        // inject starting implants
        var coords = Transform(uid).Coordinates;
        foreach (var id in comp.Implants)
        {
            var implant = Spawn(id, coords);

            if (!TryComp<SubdermalImplantComponent>(implant, out var implantComp))
                return;

            _implants.ForceImplant(uid, implant, implantComp);
        }

        // choose spider charge detonation point
        // currently based on warp points, something better could be done
        var warps = new List<EntityUid>();
        foreach (var warp in EntityManager.EntityQuery<WarpPointComponent>(true))
        {
        	if (warp.Location != null)
        		warps.Add(warp.Owner);
        }

		if (warps.Count > 0)
	    	comp.SpiderChargeTarget = _random.Pick(warps);
    }

    private void OnNinjaMindAdded(EntityUid uid, SpaceNinjaComponent comp, MindAddedMessage args)
    {
        // TODO: put in yaml somehow
        if (TryComp<MindComponent>(uid, out var mind) && mind.Mind != null && mind.Mind.TryGetSession(out var session))
        {
            mind.Mind.AddRole(new TraitorRole(mind.Mind, _proto.Index<AntagPrototype>(comp.SpaceNinjaRoleId)));
            foreach (var objective in comp.Objectives)
            {
                AddObjective(mind.Mind, objective);
            }

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

    private void AddObjective(Mind.Mind mind, string name)
    {
        if (_proto.TryIndex<ObjectivePrototype>(name, out var objective))
            mind.TryAddObjective(objective);
    }
}
