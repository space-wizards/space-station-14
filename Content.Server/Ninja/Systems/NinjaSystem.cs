using Content.Server.Administration.Commands;
using Content.Server.Body.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Doors.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Mind.Components;
using Content.Server.Ninja.Components;
using Content.Server.Objectives;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Server.Traitor;
using Content.Server.Warps;
using Content.Shared.Alert;
using Content.Shared.Doors.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Roles;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Rounding;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Ninja.Systems;

public sealed class NinjaSystem : SharedNinjaSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;
    [Dependency] private readonly InternalsSystem _internals = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaComponent, ComponentStartup>(OnNinjaStartup);
        SubscribeLocalEvent<NinjaComponent, GhostRoleSpawnerUsedEvent>(OnNinjaSpawned);
        SubscribeLocalEvent<NinjaComponent, MindAddedMessage>(OnNinjaMindAdded);

        SubscribeLocalEvent<DoorComponent, DoorEmaggedEvent>(OnDoorEmagged);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<NinjaComponent>();
        while (query.MoveNext(out var uid, out var ninja))
        {
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
        if (HasComp<NinjaComponent>(user))
            return;

        AddComp<NinjaComponent>(user);
        SetOutfitCommand.SetOutfit(user, "SpaceNinjaGear", EntityManager);
        GreetNinja(mind);
    }

    /// <summary>
    /// Returns the space ninja spawn gamerule's config
    /// </summary>
    public NinjaRuleConfiguration RuleConfig()
    {
        return (NinjaRuleConfiguration) _proto.Index<GameRulePrototype>("SpaceNinjaSpawn").Configuration;
    }

    /// <summary>
    /// Update the alert for the ninja's suit power indicator.
    /// </summary>
    public void SetSuitPowerAlert(EntityUid uid, NinjaComponent? comp = null)
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

    /// <summary>
    /// Set the station grid on an entity, either ninja spawner or the ninja itself.
    /// Used to tell a ghost that takes ninja role where the station is.
    /// </summary>
    public void SetNinjaStationGrid(EntityUid uid, EntityUid grid)
    {
        var station = EnsureComp<NinjaStationGridComponent>(uid);
        station.Grid = grid;
    }

    /// <summary>
    /// Get the battery component in a ninja's suit, if it's worn.
    /// </summary>
    public bool GetNinjaBattery(EntityUid user, [NotNullWhen(true)] out BatteryComponent? battery)
    {
        if (TryComp<NinjaComponent>(user, out var ninja)
            && ninja.Suit != null
            && _powerCell.TryGetBatteryFromSlot(ninja.Suit.Value, out battery))
        {
            return true;
        }

        battery = null;
        return false;
    }

    public override bool TryUseCharge(EntityUid user, float charge)
    {
        return GetNinjaBattery(user, out var battery) && battery.TryUseCharge(charge);
    }

    public override void CallInThreat(NinjaComponent comp)
    {
        base.CallInThreat(comp);

        var config = RuleConfig();
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

    public override void TryDrainPower(EntityUid user, NinjaDrainComponent drain, EntityUid target)
    {
        if (!GetNinjaBattery(user, out var suitBattery))
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
        var maxDrained = pnb.MaxSupply * drain.DrainTime;
        var input = Math.Min(Math.Min(available, required / drain.DrainEfficiency), maxDrained);
        if (battery.TryUseCharge(input))
        {
            var output = input * drain.DrainEfficiency;
            suitBattery.CurrentCharge += output;
            _popups.PopupEntity(Loc.GetString("ninja-drain-success", ("battery", target)), user, user);
            // TODO: spark effects
            _audio.PlayPvs(drain.SparkSound, target);
        }
    }

    private void OnNinjaStartup(EntityUid uid, NinjaComponent comp, ComponentStartup args)
    {
        var config = RuleConfig();

        // start with internals on, only when spawned by event. antag control ninja won't do this due to component add order.
        _internals.ToggleInternals(uid, uid, true);

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
        // currently based on warp points, something better could be done (but would likely require mapping work)
        var warps = new List<EntityUid>();
        var query = EntityQueryEnumerator<WarpPointComponent>();
        while (query.MoveNext(out var warpUid, out var warp))
        {
            // won't be asked to detonate the nuke disk or singularity
            if (warp.Location != null && !HasComp<PhysicsComponent>(warpUid))
                warps.Add(warpUid);
        }

        if (warps.Count > 0)
            comp.SpiderChargeTarget = _random.Pick(warps);
    }

    private void OnNinjaSpawned(EntityUid uid, NinjaComponent comp, GhostRoleSpawnerUsedEvent args)
    {
        // inherit spawner's station grid
        if (TryComp<NinjaStationGridComponent>(args.Spawner, out var station))
            SetNinjaStationGrid(uid, station.Grid);
    }

    private void OnNinjaMindAdded(EntityUid uid, NinjaComponent comp, MindAddedMessage args)
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
        {
            AddObjective(mind, objective);
        }

        _audio.PlayGlobal(config.GreetingSound, Filter.Empty().AddPlayer(session), false, AudioParams.Default);
        _chatMan.DispatchServerMessage(session, Loc.GetString("ninja-role-greeting"));

        if (TryComp<NinjaStationGridComponent>(mind.OwnedEntity, out var station))
        {
            var gridPos = _transform.GetWorldPosition(station.Grid);
            var ninjaPos = _transform.GetWorldPosition(mind.OwnedEntity.Value);
            var vector = gridPos - ninjaPos;
            var direction = vector.GetDir();
            var position = $"({(int) gridPos.X}, {(int) gridPos.Y})";
            var msg = Loc.GetString("ninja-role-greeting-direction", ("direction", direction), ("position", position));
            _chatMan.DispatchServerMessage(session, msg);
        }
    }

    private void OnDoorEmagged(EntityUid uid, DoorComponent door, ref DoorEmaggedEvent args)
    {
        // make sure it's a ninja doorjacking it
        if (TryComp<NinjaComponent>(args.UserUid, out var ninja))
            ninja.DoorsJacked++;
    }

    private void UpdateNinja(EntityUid uid, NinjaComponent ninja, float frameTime)
    {
        if (ninja.Suit == null || !TryComp<NinjaSuitComponent>(ninja.Suit, out var suit))
            return;

        float wattage = _suit.SuitWattage(suit);

        SetSuitPowerAlert(uid, ninja);
        if (!TryUseCharge(uid, wattage * frameTime))
        {
            // ran out of power, reveal ninja
            _suit.RevealNinja(ninja.Suit.Value, suit, uid);
        }
    }

    private void AddObjective(Mind.Mind mind, string name)
    {
        if (_proto.TryIndex<ObjectivePrototype>(name, out var objective))
            mind.TryAddObjective(objective);
        else
            Logger.Error($"Ninja has unknown objective prototype: {name}");
    }
}
