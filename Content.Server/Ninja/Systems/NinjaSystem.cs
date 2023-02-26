using Content.Server.Actions;
using Content.Server.Administration.Commands;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.Doors.Systems;
using Content.Server.DoAfter;
using Content.Server.Electrocution;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Components;
using Content.Server.Objectives;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Server.Traitor;
using Content.Server.Warps;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Research.Components;
using Content.Shared.Roles;
using Content.Shared.Rounding;
using Content.Shared.Stealth.Components;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Content.Server.Ninja.Systems;

public sealed partial class NinjaSystem : SharedNinjaSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly DoAfterSystem _doafter = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceNinjaGlovesComponent, GotEquippedEvent>(OnGlovesEquipped);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, GotUnequippedEvent>(OnGlovesUnequipped);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, ToggleNinjaGlovesEvent>(OnToggleGloves);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, ExaminedEvent>(OnGlovesExamine);
        SubscribeLocalEvent<ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, DoAfterEvent<PowerDrainData>>(OnDrainDoAfter);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, DoAfterEvent<ResearchDownloadData>>(OnDownloadDoAfter);
        SubscribeLocalEvent<SpaceNinjaGlovesComponent, DoAfterEvent<TerrorData>>(OnTerrorDoAfter);

        // TODO: maybe have suit activation stuff
        SubscribeLocalEvent<SpaceNinjaSuitComponent, GotEquippedEvent>(OnSuitEquipped);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, ContainerIsInsertingAttemptEvent>(OnSuitInsertAttempt);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, GotUnequippedEvent>(OnSuitUnequipped);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, TogglePhaseCloakEvent>(OnTogglePhaseCloakAction);
        SubscribeLocalEvent<SpaceNinjaSuitComponent, RecallKatanaEvent>(OnRecallKatanaAction);

        SubscribeLocalEvent<SpaceNinjaComponent, ComponentStartup>(OnNinjaStartup);
        SubscribeLocalEvent<SpaceNinjaComponent, MindAddedMessage>(OnNinjaMindAdded);
        SubscribeLocalEvent<SpaceNinjaComponent, AttackedEvent>(OnNinjaAttacked);

        SubscribeLocalEvent<DoorComponent, DoorEmaggedEvent>(OnDoorEmagged);
    }

    public override void Update(float frameTime)
    {
        foreach (var ninja in EntityQuery<SpaceNinjaComponent>())
        {
            var uid = ninja.Owner;
            UpdateNinja(uid, ninja, frameTime);
        }
    }

    /// <summary>
    /// Turns the player into a space ninja
    /// </summary>
    public void MakeNinja(Mind.Mind mind)
    {
        if (mind.OwnedEntity == null)
            return;

        // prevent double ninja'ing
        var user = mind.OwnedEntity.Value;
        if (HasComp<SpaceNinjaComponent>(user))
            return;

        AddComp<SpaceNinjaComponent>(user);
        SetOutfitCommand.SetOutfit(user, "SpaceNinjaGear", EntityManager);
        GreetNinja(mind);
    }

    private void OnGlovesEquipped(EntityUid uid, SpaceNinjaGlovesComponent comp, GotEquippedEvent args)
    {
        var user = args.Equipee;
        if (TryComp<SpaceNinjaComponent>(user, out var ninja) && TryComp<ActionsComponent>(user, out var actions))
        {
            ninja.Gloves = uid;
            _actions.AddAction(user, comp.ToggleAction, uid, actions);
        }
    }

    private void OnGlovesUnequipped(EntityUid uid, SpaceNinjaGlovesComponent comp, GotUnequippedEvent args)
    {
        var user = args.Equipee;
        DisableGloves(comp, user);

        _actions.RemoveProvidedActions(args.Equipee, uid);
        if (TryComp<SpaceNinjaComponent>(user, out var ninja))
            ninja.Gloves = null;
    }

    private void OnToggleGloves(EntityUid uid, SpaceNinjaGlovesComponent comp, ToggleNinjaGlovesEvent args)
    {
        var user = args.Performer;
        // need to wear suit to enable gloves
        if (!TryComp<SpaceNinjaComponent>(user, out var ninja)
            || ninja.Suit == null
            || !HasComp<SpaceNinjaSuitComponent>(ninja.Suit.Value))
        {
            _popups.PopupEntity(Loc.GetString("ninja-gloves-not-wearing-suit"), user, user);
            return;
        }

        comp.Enabled = !comp.Enabled;
        var message = Loc.GetString(comp.Enabled ? "ninja-gloves-on" : "ninja-gloves-off");
        _popups.PopupEntity(message, user, user);
    }

    private void OnGlovesExamine(EntityUid uid, SpaceNinjaGlovesComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(Loc.GetString(comp.Enabled ? "ninja-gloves-examine-on" : "ninja-gloves-examine-off"));
    }

    private void OnActivate(ActivateInWorldEvent args)
    {
        var user = args.User;
        var target = args.Target;
        if (args.Handled
            || !TryComp<SpaceNinjaComponent>(user, out var ninja)
            || ninja.Gloves == null
            || !TryComp<SpaceNinjaGlovesComponent>(ninja.Gloves, out var comp)
            || !comp.Enabled)
            return;

        var uid = ninja.Gloves.Value;

        // doorjack ability
        if (HasComp<DoorComponent>(target))
        {
            if (_tags.HasTag(target, comp.EmagImmuneTag))
                return;

            var handled = _emag.DoEmagEffect(user, target);
            if (!handled)
                return;

            _popups.PopupEntity(Loc.GetString("emag-success", ("target", Identity.Entity(target, EntityManager))), user,
                user, PopupType.Medium);
            _adminLogger.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(user):player} doorjacked {ToPrettyString(target):target}");

            args.Handled = true;
            return;
        }

        // stun ability
        if (user != target && HasComp<StaminaComponent>(target))
        {
            // take charge from battery
            if (!GetNinjaBattery(user, out var battery) || !battery.TryUseCharge(comp.StunCharge))
            {
                _popups.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
                return;
            }

            // not holding hands with target so insuls don't matter
            args.Handled = _electrocution.TryDoElectrocution(target, uid, comp.StunDamage, comp.StunTime, false, ignoreInsulation: true);
            return;
        }

        // drain ability
        if (HasComp<PowerNetworkBatteryComponent>(target))
        {
            if (!HasComp<BatteryComponent>(target))
                return;

            var doafterArgs = new DoAfterEventArgs(user, comp.DrainTime, target: target, used: uid)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnUserMove = true,
                MovementThreshold = 0.5f
            };

            _doafter.DoAfter(doafterArgs);
            args.Handled = true;
            return;
        }

        // download ability
        if (TryComp<TechnologyDatabaseComponent>(target, out var database))
        {
            // fail fast if theres no tech right now
            if (database.TechnologyIds.Count == 0)
            {
                _popups.PopupEntity(Loc.GetString("ninja-download-fail"), user, user);
                return;
            }

            var doafterArgs = new DoAfterEventArgs(user, comp.DownloadTime, target: target, used: uid)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnUserMove = true,
                MovementThreshold = 0.5f
            };

            _doafter.DoAfter(doafterArgs);
            args.Handled = true;
            return;
        }

        // terror ability
        if (HasComp<CommunicationsConsoleComponent>(target))
        {
            // can only do it once
            if (ninja.CalledInThreat)
            {
                _popups.PopupEntity(Loc.GetString("ninja-terror-already-called"), user, user);
                return;
            }

            var doafterArgs = new DoAfterEventArgs(user, comp.TerrorTime, target: target, used: uid)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnUserMove = true,
                MovementThreshold = 0.5f
            };

            _doafter.DoAfter(doafterArgs);
            args.Handled = true;
        }
    }

    private void OnDrainDoAfter(EntityUid uid, SpaceNinjaGlovesComponent comp, DoAfterEvent<PowerDrainData> args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.Args.User;
        var target = args.Args.Target;

        if (!GetNinjaBattery(user, out var suitBattery))
            // took suit off or something, ignore draining
            return;

        if (suitBattery.IsFullyCharged)
        {
            _popups.PopupEntity(Loc.GetString("ninja-drain-full"), user, user, PopupType.Medium);
            return;
        }

        if (!TryComp<BatteryComponent>(target, out var battery) || !TryComp<PowerNetworkBatteryComponent>(target, out var pnb))
            return;

        if (MathHelper.CloseToPercent(battery.CurrentCharge, 0))
        {
            _popups.PopupEntity(Loc.GetString("ninja-drain-empty", ("battery", target)), user, user, PopupType.Medium);
            return;
        }

        var available = battery.CurrentCharge;
        var required = suitBattery.MaxCharge - suitBattery.CurrentCharge;
        // higher tier storages can charge more
        var maxDrained = pnb.MaxSupply * comp.DrainTime;
        var input = Math.Min(Math.Min(available, required / comp.DrainEfficiency), maxDrained);
        if (battery.TryUseCharge(input))
        {
            var output = input * comp.DrainEfficiency;
            suitBattery.CurrentCharge += output;
            _popups.PopupEntity(Loc.GetString("ninja-drain-success", ("battery", target)), user, user);
            // TODO: spark effects
            _audio.PlayPvs(comp.SparkSound, uid);
        }
    }

    private void OnDownloadDoAfter(EntityUid uid, SpaceNinjaGlovesComponent comp, DoAfterEvent<ResearchDownloadData> args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.Args.User;
        var target = args.Args.Target;

        if (!TryComp<SpaceNinjaComponent>(user, out var ninja)
            || !TryComp<TechnologyDatabaseComponent>(target, out var database))
            return;

        var oldCount = ninja.DownloadedNodes.Count;
        ninja.DownloadedNodes.UnionWith(database.TechnologyIds);
        var newCount = ninja.DownloadedNodes.Count;

        var gained = newCount - oldCount;
        var str = gained == 0
            ? Loc.GetString("ninja-download-fail")
            : Loc.GetString("ninja-download-success", ("count", gained), ("server", target));

        _popups.PopupEntity(str, user, user, PopupType.Medium);
    }

    private void OnTerrorDoAfter(EntityUid uid, SpaceNinjaGlovesComponent comp, DoAfterEvent<TerrorData> args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.Args.User;

        if (!TryComp<SpaceNinjaComponent>(user, out var ninja) || ninja.CalledInThreat)
            return;

        ninja.CalledInThreat = true;

        var config = RuleConfig();
        if (config.Threats.Count == 0)
            return;

        var threat = _random.Pick(config.Threats);
        if (_proto.TryIndex<GameRulePrototype>(threat.Rule, out var rule))
        {
            _gameTicker.AddGameRule(rule);
            _chat.DispatchGlobalAnnouncement(Loc.GetString(threat.Announcement), playSound: false, colorOverride: Color.Red);
        }
    }

    private void OnSuitEquipped(EntityUid uid, SpaceNinjaSuitComponent comp, GotEquippedEvent args)
    {
        var user = args.Equipee;
        if (!TryComp<SpaceNinjaComponent>(user, out var ninja) || !TryComp<ActionsComponent>(user, out var actions))
            return;

        _actions.AddAction(user, comp.TogglePhaseCloakAction, uid, actions);
        _actions.AddAction(user, comp.RecallKatanaAction, uid, actions);
        _actions.AddAction(user, comp.KatanaDashAction, uid, actions);
        // TODO: emp ability
        // TODO: ninja star ability

        // mark the user as wearing this suit, used when being attacked among other things
        ninja.Suit = uid;

        // initialize phase cloak
        AddComp<StealthComponent>(user);
        SetCloaked(user, comp.Cloaked);
        SetSuitPowerAlert(user);
    }

    // TODO: put in shared so client properly predicts insertion
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

    private void OnSuitUnequipped(EntityUid uid, SpaceNinjaSuitComponent comp, GotUnequippedEvent args)
    {
        var user = args.Equipee;
        _actions.RemoveProvidedActions(user, uid);

        // mark the user as not wearing a suit
        if (TryComp<SpaceNinjaComponent>(user, out var ninja))
        {
            ninja.Suit = null;
            // disable glove abilities
            if (ninja.Gloves != null && TryComp<SpaceNinjaGlovesComponent>(ninja.Gloves.Value, out var gloves))
                DisableGloves(gloves, user);
        }

        // force uncloak
        comp.Cloaked = false;
        SetCloaked(user, false);
        RemComp<StealthComponent>(user);

        // remove power indicator
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

    private void OnNinjaStartup(EntityUid uid, SpaceNinjaComponent comp, ComponentStartup args)
    {
        var config = RuleConfig();

        // inject starting implants
        var coords = Transform(uid).Coordinates;
        foreach (var id in config.Implants)
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
        if (TryComp<MindComponent>(uid, out var mind) && mind.Mind != null)
            GreetNinja(mind.Mind);
    }

    private void GreetNinja(Mind.Mind mind)
    {
        if (!mind.TryGetSession(out var session))
            return;

        var config = RuleConfig();
        var role = new TraitorRole(mind, _proto.Index<AntagPrototype>("SpaceNinja"));
        mind.AddRole(role);
        _traitorRule.Traitors.Add(role);
        foreach (var objective in config.Objectives)
            AddObjective(mind, objective);

        _chatMan.DispatchServerMessage(session, Loc.GetString("ninja-role-greeting"));
        _audio.PlayGlobal(config.GreetingSound, Filter.Empty().AddPlayer(session), false, AudioParams.Default);
    }

    private NinjaRuleConfiguration RuleConfig()
    {
        return (NinjaRuleConfiguration) _proto.Index<GameRulePrototype>("SpaceNinjaSpawn").Configuration;
    }

    private void DisableGloves(SpaceNinjaGlovesComponent comp, EntityUid user)
    {
        if (comp.Enabled)
        {
            comp.Enabled = false;
            _popups.PopupEntity(Loc.GetString("ninja-gloves-on"), user, user);
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

    private void OnDoorEmagged(EntityUid uid, DoorComponent door, DoorEmaggedEvent args)
    {
        // make sure it's a ninja doorjacking it
        if (TryComp<SpaceNinjaComponent>(args.UserUid, out var ninja))
            ninja.DoorsJacked++;
    }

    private void UpdateNinja(EntityUid uid, SpaceNinjaComponent ninja, float frameTime)
    {
        if (ninja.Suit == null || !TryComp<SpaceNinjaSuitComponent>(ninja.Suit.Value, out var suit))
            return;

        float wattage = SuitWattage(suit);

        SetSuitPowerAlert(uid, ninja);
        if (!GetNinjaBattery(uid, out var battery) || !battery.TryUseCharge(wattage * frameTime))
        {
            // ran out of power, reveal ninja
            if (suit.Cloaked)
            {
                suit.Cloaked = false;
                SetCloaked(uid, false);
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
        else
            Logger.Error($"Ninja has unknown objective prototype: {name}");
    }
}
