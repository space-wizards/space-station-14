using Content.Server.Doors.Systems;
using Content.Server.DoAfter;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.Administration.Logs;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Tag;

namespace Content.Server.Ninja.Systems;

public sealed class SpaceNinjaGlovesSystem : SharedSpaceNinjaGlovesSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SpaceNinjaSystem _ninja = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<NinjaDrainComponent, DoAfterEvent>(OnDrainDoAfter);
        SubscribeLocalEvent<NinjaDownloadComponent, DoAfterEvent>(OnDownloadDoAfter);
        SubscribeLocalEvent<NinjaTerrorComponent, DoAfterEvent>(OnTerrorDoAfter);

        SubscribeLocalEvent<DoorComponent, DoorEmaggedEvent>(OnDoorEmagged);
    }

    protected override void NinjaEquippedGloves(EntityUid uid, SpaceNinjaGlovesComponent comp, EntityUid user, SpaceNinjaComponent ninja)
    {
        base.NinjaEquippedGloves(uid, comp, user, ninja);

        if (TryComp<ActionsComponent>(user, out var actions))
            _actions.AddAction(user, comp.ToggleAction, uid, actions);
    }

    protected override void UserUnequippedGloves(EntityUid uid, SpaceNinjaGlovesComponent comp, EntityUid user)
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
            || !TryComp<SpaceNinjaComponent>(user, out var ninja)
            || ninja.Gloves == null
            || !HasComp<GlovesEnabledComponent>(ninja.Gloves))
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
            if (!GetNinjaBattery(user, out var battery) || !battery.TryUseCharge(stun.StunCharge))
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
            if (!HasComp<BatteryComponent>(target))
                return;

            var doafterArgs = new DoAfterEventArgs(user, drain.DrainTime, target: target, used: uid)
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
        if (TryComp<NinjaDownloadComponent>(uid, out var download) && TryComp<TechnologyDatabaseComponent>(target, out var database))
        {
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

            _doafter.DoAfter(doafterArgs);
            args.Handled = true;
            return;
        }

        // terror ability
        if (TryComp<NinjaTerrorComponent>(uid, out var terror) && HasComp<CommunicationsConsoleComponent>(target))
        {
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

            _doafter.DoAfter(doafterArgs);
            args.Handled = true;
        }
    }

    private void OnDrainDoAfter(EntityUid uid, NinjaDrainComponent comp, DoAfterEvent args)
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

    private void OnDownloadDoAfter(EntityUid uid, NinjaDownloadComponent comp, DoAfterEvent args)
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

    private void OnTerrorDoAfter(EntityUid uid, NinjaTerrorComponent comp, DoAfterEvent args)
    {
        var user = args.Args.User;
        if (args.Cancelled || args.Handled)
        {
            _popups.PopupEntity($"sorry bub {args.Cancelled} {args.Handled}", user, user);
            return;
        }

        if (!TryComp<SpaceNinjaComponent>(user, out var ninja) || ninja.CalledInThreat)
            return;

        ninja.CalledInThreat = true;

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
        if (TryComp<SpaceNinjaComponent>(args.UserUid, out var ninja))
            ninja.DoorsJacked++;
    }
}
