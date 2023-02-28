using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.Doors.Systems;
using Content.Server.DoAfter;
using Content.Server.Electrocution;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.Research.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Ninja.Systems;

public sealed class NinjaGlovesSystem : SharedNinjaGlovesSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DoAfterSystem _doafter = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly NinjaSystem _ninja = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<NinjaGlovesComponent, DoAfterEvent>(EndedDoAfter);
        // TODO: raising 1 event triggers for all 3 components sicne on same entity, change to be per-component?
        SubscribeLocalEvent<NinjaDrainComponent, DoAfterEvent>(OnDrainDoAfter);
        SubscribeLocalEvent<NinjaDownloadComponent, DoAfterEvent>(OnDownloadDoAfter);
        SubscribeLocalEvent<NinjaTerrorComponent, DoAfterEvent>(OnTerrorDoAfter);

        SubscribeLocalEvent<DoorComponent, DoorEmaggedEvent>(OnDoorEmagged);
    }

    protected override void NinjaEquippedGloves(EntityUid uid, NinjaGlovesComponent comp, EntityUid user, NinjaComponent ninja)
    {
        base.NinjaEquippedGloves(uid, comp, user, ninja);

        if (TryComp<ActionsComponent>(user, out var actions))
            _actions.AddAction(user, comp.ToggleAction, uid, actions);
    }

    protected override void UserUnequippedGloves(EntityUid uid, NinjaGlovesComponent comp, EntityUid user)
    {
        base.UserUnequippedGloves(uid, comp, user);

        _actions.RemoveProvidedActions(user, uid);
    }

    // TODO: make this per-ability
    private void OnActivate(ActivateInWorldEvent args)
    {
        var user = args.User;
        var target = args.Target;
        if (args.Handled
            || !TryComp<NinjaComponent>(user, out var ninja)
            || ninja.Gloves == null
            || !TryComp<NinjaGlovesComponent>(ninja.Gloves, out var comp)
            || !comp.Enabled)
            return;

        var uid = ninja.Gloves.Value;

        // doorjack ability
        if (TryComp<NinjaDoorjackComponent>(uid, out var doorjack) && HasComp<DoorComponent>(target))
        {
            if (_tags.HasTag(target, doorjack.EmagImmuneTag))
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
        if (user != target && TryComp<NinjaStunComponent>(uid, out var stun) && HasComp<StaminaComponent>(target))
        {
            // take charge from battery
            if (!_ninja.GetNinjaBattery(user, out var battery) || !battery.TryUseCharge(stun.StunCharge))
            {
                _popups.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
                return;
            }

            // not holding hands with target so insuls don't matter
            args.Handled = _electrocution.TryDoElectrocution(target, uid, stun.StunDamage, stun.StunTime, false, ignoreInsulation: true);
            return;
        }

        // drain ability
        if (TryComp<NinjaDrainComponent>(uid, out var drain) && HasComp<PowerNetworkBatteryComponent>(target))
        {
            // nicer for spam-clicking to not open apc ui so cancel it
            if (comp.Busy || !HasComp<BatteryComponent>(target))
            {
                args.Handled = true;
                return;
            }

            var doafterArgs = new DoAfterEventArgs(user, drain.DrainTime, target: target, used: uid)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnUserMove = true,
                MovementThreshold = 0.5f
            };

            comp.Busy = true;
            _doafter.DoAfter(doafterArgs);
            args.Handled = true;
            return;
        }

        // download ability
        if (TryComp<NinjaDownloadComponent>(uid, out var download) && TryComp<TechnologyDatabaseComponent>(target, out var database))
        {
            if (comp.Busy)
                return;

            // fail fast if theres no tech right now
            if (database.TechnologyIds.Count == 0)
            {
                _popups.PopupEntity(Loc.GetString("ninja-download-fail"), user, user);
                return;
            }

            var doafterArgs = new DoAfterEventArgs(user, download.DownloadTime, target: target, used: uid)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnUserMove = true,
                MovementThreshold = 0.5f
            };

            comp.Busy = true;
            _doafter.DoAfter(doafterArgs);
            args.Handled = true;
            return;
        }

        // terror ability
        if (TryComp<NinjaTerrorComponent>(uid, out var terror) && HasComp<CommunicationsConsoleComponent>(target))
        {
            if (comp.Busy)
                return;

            // can only do it once
            if (ninja.CalledInThreat)
            {
                _popups.PopupEntity(Loc.GetString("ninja-terror-already-called"), user, user);
                return;
            }

            var doafterArgs = new DoAfterEventArgs(user, terror.TerrorTime, target: target, used: uid)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnUserMove = true,
                MovementThreshold = 0.5f
            };

            comp.Busy = true;
            _doafter.DoAfter(doafterArgs);
            args.Handled = true;
        }
    }

    private void EndedDoAfter(EntityUid uid, NinjaGlovesComponent comp, DoAfterEvent args)
    {
        if (args.Handled)
            return;

        comp.Busy = false;
    }

    private void OnDrainDoAfter(EntityUid uid, NinjaDrainComponent comp, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.Args.User;
        var target = args.Args.Target;

        if (!_ninja.GetNinjaBattery(user, out var suitBattery))
            // took suit off or something, ignore draining
            return;

        if (!TryComp<BatteryComponent>(target, out var battery) || !TryComp<PowerNetworkBatteryComponent>(target, out var pnb))
            return;

        if (suitBattery.IsFullyCharged)
        {
            _popups.PopupEntity(Loc.GetString("ninja-drain-full"), user, user, PopupType.Medium);
            return;
        }

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

    private void OnDownloadDoAfter(EntityUid uid, NinjaDownloadComponent comp, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.Args.User;
        var target = args.Args.Target;

        if (!TryComp<NinjaComponent>(user, out var ninja)
            || !TryComp<TechnologyDatabaseComponent>(target, out var database))
            return;

        var gained = _ninja.Download(ninja, database.TechnologyIds);
        var str = gained == 0
            ? Loc.GetString("ninja-download-fail")
            : Loc.GetString("ninja-download-success", ("count", gained), ("server", target));

        _popups.PopupEntity(str, user, user, PopupType.Medium);
    }

    private void OnTerrorDoAfter(EntityUid uid, NinjaTerrorComponent comp, DoAfterEvent args)
    {
        var target = args.Args.Target;
        if (args.Cancelled || args.Handled || !HasComp<CommunicationsConsoleComponent>(target))
            return;

        var user = args.Args.User;
        if (!TryComp<NinjaComponent>(user, out var ninja) || ninja.CalledInThreat)
            return;

        _ninja.CallInThreat(ninja);

        var config = _ninja.RuleConfig();
        if (config.Threats.Count == 0)
            return;

        var threat = _random.Pick(config.Threats);
        if (_proto.TryIndex<GameRulePrototype>(threat.Rule, out var rule))
        {
            _gameTicker.AddGameRule(rule);
            _chat.DispatchGlobalAnnouncement(Loc.GetString(threat.Announcement), playSound: false, colorOverride: Color.Red);
        }
        else
        {
            Logger.Error($"Threat gamerule does not exist: {threat.Rule}");
        }
    }

    private void OnDoorEmagged(EntityUid uid, DoorComponent door, ref DoorEmaggedEvent args)
    {
        // make sure it's a ninja doorjacking it
        if (TryComp<NinjaComponent>(args.UserUid, out var ninja))
            ninja.DoorsJacked++;
    }
}
