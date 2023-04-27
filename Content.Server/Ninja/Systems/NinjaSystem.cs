using Content.Server.Administration.Commands;
using Content.Server.Body.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Doors.Systems;
using Content.Server.StationEvents.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Mind.Components;
using Content.Server.Ninja;
using Content.Server.Ninja.Components;
using Content.Server.Objectives;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
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
using System.Linq;

namespace Content.Server.Ninja.Systems;

public sealed class NinjaSystem : SharedNinjaSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
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
        if (HasComp<NinjaComponent>(user) || HasComp<NinjaSpawnerDataComponent>(user))
            return;

        // add a game rule for this ninja, but will not start it so no spawner is created
        AddComp<NinjaSpawnerDataComponent>(user).Rule = _gameTicker.AddGameRule("NinjaSpawn");
        AddComp<NinjaComponent>(user);
        SetOutfitCommand.SetOutfit(user, "SpaceNinjaGear", EntityManager);
        GreetNinja(mind);
    }

    /// <summary>
    /// Download the given set of nodes, returning how many new nodes were downloaded.'
    /// </summary>
    public int Download(EntityUid uid, List<string> ids)
    {
        if (!GetNinjaRole(uid, out var role))
            return 0;

        var oldCount = role.DownloadedNodes.Count;
        role.DownloadedNodes.UnionWith(ids);
        var newCount = role.DownloadedNodes.Count;
        return newCount - oldCount;
    }

    /// <summary>
    /// Gets a ninja's role using the player's mind
    /// </summary>
    public static bool GetNinjaRole(Mind.Mind? mind, [NotNullWhen(true)] out NinjaRole? role)
    {
        if (mind == null)
        {
            role = null;
            return false;
        }

        role = (NinjaRole?) mind.AllRoles
            .Where(r => r is NinjaRole)
            .FirstOrDefault();
        return role != null;
    }

    /// <summary>
    /// Gets a ninja's role using the player's entity id
    /// </summary>
    public bool GetNinjaRole(EntityUid uid, [NotNullWhen(true)] out NinjaRole? role)
    {
        role = null;
        if (!TryComp<MindComponent>(uid, out var mind))
            return false;

        return GetNinjaRole(mind.Mind, out role);
    }

    /// <summary>
    /// Returns the space ninja's gamerule config
    /// </summary>
    public NinjaSpawnRuleComponent RuleConfig(EntityUid uid)
    {
        var data = Comp<NinjaSpawnerDataComponent>(uid);
        return Comp<NinjaSpawnRuleComponent>(data.Rule);
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
    public void SetNinjaSpawnerData(EntityUid uid, EntityUid grid, EntityUid rule)
    {
        var comp = EnsureComp<NinjaSpawnerDataComponent>(uid);
        comp.Grid = grid;
        comp.Rule = rule;
    }

    /// <summary>
    /// Get the battery component in a ninja's suit, if it's worn.
    /// </summary>
    // TODO: modify TryGetBatteryFromSlot, return uid as well
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
        return GetNinjaBattery(user, out var battery) && _battery.TryUseCharge(battery.Owner, charge, battery);
    }

    /// <summary>
    /// Completes the objective, makes announcement and adds rule of a random threat.
    /// </summary>
    public void CallInThreat(EntityUid uid)
    {
        var config = RuleConfig(uid);
        if (config.Threats.Count == 0 || !GetNinjaRole(uid, out var role) || role.CalledInThreat)
            return;

        role.CalledInThreat = true;

        var threat = _random.Pick(config.Threats);
        _gameTicker.StartGameRule(threat.Rule, out _);
        _chat.DispatchGlobalAnnouncement(Loc.GetString(threat.Announcement), playSound: false, colorOverride: Color.Red);
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
        if (_battery.TryUseCharge(target, input, battery))
        {
            var output = input * drain.DrainEfficiency;
            _battery.SetCharge(suitBattery.Owner, suitBattery.CurrentCharge + output, suitBattery);
            _popups.PopupEntity(Loc.GetString("ninja-drain-success", ("battery", target)), user, user);
            // TODO: spark effects
            _audio.PlayPvs(drain.SparkSound, target);
        }
    }

    private void OnNinjaStartup(EntityUid uid, NinjaComponent comp, ComponentStartup args)
    {
        // start with internals on, only when spawned by event. antag control ninja won't do this due to component add order.
        _internals.ToggleInternals(uid, uid, true);

        // inject starting implants if made ninja in antag ctrl
        AddImplants(uid);
    }

    private void OnNinjaSpawned(EntityUid uid, NinjaComponent comp, GhostRoleSpawnerUsedEvent args)
    {
        // inherit spawner's data
        if (TryComp<NinjaSpawnerDataComponent>(args.Spawner, out var data))
        {
            SetNinjaSpawnerData(uid, data.Grid, data.Rule);
            AddImplants(uid);
        }
    }

    private void AddImplants(EntityUid uid)
    {
        if (!HasComp<NinjaSpawnerDataComponent>(uid))
            return;

        var config = RuleConfig(uid);
        var coords = Transform(uid).Coordinates;
        foreach (var id in config.Implants)
        {
            var implant = Spawn(id, coords);

            if (!TryComp<SubdermalImplantComponent>(implant, out var implantComp))
                return;

            _implants.ForceImplant(uid, implant, implantComp);
        }
    }

    private void OnNinjaMindAdded(EntityUid uid, NinjaComponent comp, MindAddedMessage args)
    {
        if (TryComp<MindComponent>(uid, out var mind) && mind.Mind != null)
            GreetNinja(mind.Mind);
    }

    private void GreetNinja(Mind.Mind mind)
    {
        if (!mind.TryGetSession(out var session) || mind.OwnedEntity == null)
            return;

        var traitorRule = EntityQuery<TraitorRuleComponent>().FirstOrDefault();
        if (traitorRule == null)
        {
            // TODO: fuck me this shit is awful, see TraitorRuleSystem
            _gameTicker.StartGameRule("Traitor", out var ruleEntity);
            traitorRule = Comp<TraitorRuleComponent>(ruleEntity);
        }

        var config = RuleConfig(mind.OwnedEntity.Value);
        var role = new NinjaRole(mind, _proto.Index<AntagPrototype>("SpaceNinja"));
        mind.AddRole(role);
        _traitorRule.AddToTraitors(traitorRule, role);
        foreach (var objective in config.Objectives)
        {
            AddObjective(mind, objective);
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
            role.SpiderChargeTarget = _random.Pick(warps);

        _audio.PlayGlobal(config.GreetingSound, Filter.Empty().AddPlayer(session), false, AudioParams.Default);
        _chatMan.DispatchServerMessage(session, Loc.GetString("ninja-role-greeting"));

        if (TryComp<NinjaSpawnerDataComponent>(mind.OwnedEntity, out var data) && data.Grid != EntityUid.Invalid)
        {
            var gridPos = _transform.GetWorldPosition(data.Grid);
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
        if (GetNinjaRole(args.UserUid, out var role))
            role.DoorsJacked++;
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
