using Content.Server.Administration.Commands;
using Content.Server.Body.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.StationEvents.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Objectives;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Server.Roles;
using Content.Server.Warps;
using Content.Shared.Alert;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Doors.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
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

/// <summary>
/// Main ninja system that handles ninja setup, provides helper methods for the rest of the code to use.
/// </summary>
public sealed class SpaceNinjaSystem : SharedSpaceNinjaSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly InternalsSystem _internals = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StealthClothingSystem _stealthClothing = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceNinjaComponent, ComponentInit>(OnNinjaInit);
        SubscribeLocalEvent<SpaceNinjaComponent, MindAddedMessage>(OnNinjaMindAdded);
        SubscribeLocalEvent<SpaceNinjaComponent, EmaggedSomethingEvent>(OnDoorjack);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SpaceNinjaComponent>();
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
        if (HasComp<SpaceNinjaComponent>(user))
            return;

        AddComp<SpaceNinjaComponent>(user);
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
    public bool GetNinjaRole(Mind.Mind? mind, [NotNullWhen(true)] out NinjaRole? role)
    {
        role = null;
        if (mind == null)
            return false;

        foreach (var r in mind.AllRoles)
        {
            if (r is NinjaRole ninja)
            {
                role = ninja;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a ninja's role using the player's entity id
    /// </summary>
    public bool GetNinjaRole(EntityUid uid, [NotNullWhen(true)] out NinjaRole? role)
    {
        role = null;
        if (!TryComp<MindContainerComponent>(uid, out var mind))
            return false;

        return GetNinjaRole(mind.Mind, out role);
    }

    /// <summary>
    /// Returns the global ninja gamerule config
    /// </summary>
    public NinjaRuleComponent RuleConfig()
    {
        var rule = EntityQuery<NinjaRuleComponent>().FirstOrDefault();
        if (rule != null)
            return rule;

        _gameTicker.StartGameRule("Ninja", out var ruleEntity);
        return Comp<NinjaRuleComponent>(ruleEntity);
    }

    /// <summary>
    /// Update the alert for the ninja's suit power indicator.
    /// </summary>
    public void SetSuitPowerAlert(EntityUid uid, SpaceNinjaComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false) || comp.Deleted || comp.Suit == null)
        {
            _alerts.ClearAlert(uid, AlertType.SuitPower);
            return;
        }

        if (GetNinjaBattery(uid, out var _, out var battery))
        {
             var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, battery.CurrentCharge), battery.MaxCharge, 8);
            _alerts.ShowAlert(uid, AlertType.SuitPower, (short) severity);
        }
        else
        {
            _alerts.ClearAlert(uid, AlertType.SuitPower);
        }
    }

    /// <summary>
    /// Get the battery component in a ninja's suit, if it's worn.
    /// </summary>
    public bool GetNinjaBattery(EntityUid user, [NotNullWhen(true)] out EntityUid? uid, [NotNullWhen(true)] out BatteryComponent? battery)
    {
        if (TryComp<SpaceNinjaComponent>(user, out var ninja)
            && ninja.Suit != null
            && _powerCell.TryGetBatteryFromSlot(ninja.Suit.Value, out uid, out battery))
        {
            return true;
        }

        uid = null;
        battery = null;
        return false;
    }

    /// <inheritdoc/>
    public override bool TryUseCharge(EntityUid user, float charge)
    {
        return GetNinjaBattery(user, out var uid, out var battery) && _battery.TryUseCharge(uid.Value, charge, battery);
    }

    /// <summary>
    /// Completes the objective, makes announcement and adds rule of a random threat.
    /// </summary>
    public void CallInThreat(EntityUid uid)
    {
        var config = RuleConfig();
        if (config.Threats.Count == 0 || !GetNinjaRole(uid, out var role) || role.CalledInThreat)
            return;

        role.CalledInThreat = true;

        var threat = _random.Pick(config.Threats);
        _gameTicker.StartGameRule(threat.Rule, out _);
        _chat.DispatchGlobalAnnouncement(Loc.GetString(threat.Announcement), playSound: false, colorOverride: Color.Red);
    }

    /// <summary>
    /// Set up ninja when created.
    /// Runs before Implanted's ComponentStartup so it will work
    /// </summary>
    private void OnNinjaInit(EntityUid uid, SpaceNinjaComponent comp, ComponentInit args)
    {
        // start with internals on, only when spawned by event. antag control ninja won't do this due to component add order.
        _internals.ToggleInternals(uid, uid, true);

        // inject starting implants if made ninja in antag ctrl
        AddImplants(uid);
    }

    /// <summary>
    /// Add configured implants to the ninja.
    /// </summary>
    /// <remarks>
    /// Could be replaced with job specials ImplantSpecial if ninja became a job somehow.
    /// </remarks>
    private void AddImplants(EntityUid uid)
    {
        _implants.AddImplants(uid, RuleConfig().Implants);
    }

    /// <summary>
    /// Greets the ninja when a ghost takes over a ninja, if that happens.
    /// </summary>
    private void OnNinjaMindAdded(EntityUid uid, SpaceNinjaComponent comp, MindAddedMessage args)
    {
        if (TryComp<MindContainerComponent>(uid, out var mind) && mind.Mind != null)
            GreetNinja(mind.Mind);
    }

    /// <summary>
    /// Set up everything for ninja to work and send the greeting message/sound.
    /// </summary>
    /// <remarks>
    /// Currently this adds the ninja to traitors, this should be removed when objectives are separated from traitors.
    /// </remarks>
    private void GreetNinja(Mind.Mind mind)
    {
        if (!_mind.TryGetSession(mind, out var session) || mind.OwnedEntity == null)
            return;

        // make sure to enable the traitor rule for the sweet greentext
        var traitorRule = EntityQuery<TraitorRuleComponent>().FirstOrDefault();
        if (traitorRule == null)
        {
            // TODO: fuck me this shit is awful, see TraitorRuleSystem
            _gameTicker.StartGameRule("Traitor", out var ruleEntity);
            traitorRule = Comp<TraitorRuleComponent>(ruleEntity);
        }

        var config = RuleConfig();
        var role = new NinjaRole(mind, _proto.Index<AntagPrototype>("SpaceNinja"));
        _mind.AddRole(mind, role);
        // TODO: when objectives are not tied to traitor roles, remove this
        _traitorRule.AddToTraitors(traitorRule, role);

        // choose spider charge detonation point
        // currently based on warp points, something better could be done (but would likely require mapping work)
        var warps = new List<EntityUid>();
        var query = EntityQueryEnumerator<WarpPointComponent, TransformComponent>();
        var map = Transform(mind.OwnedEntity.Value).MapID;
        while (query.MoveNext(out var warpUid, out var warp, out var xform))
        {
            // won't be asked to detonate the nuke disk or singularity or centcomm
            if (warp.Location != null && !HasComp<PhysicsComponent>(warpUid) && xform.MapID == map)
                warps.Add(warpUid);
        }

        if (warps.Count > 0)
            role.SpiderChargeTarget = _random.Pick(warps);

        // assign objectives - must happen after spider charge target so that the obj requirement works
        foreach (var objective in config.Objectives)
        {
            AddObjective(mind, objective);
        }

        _audio.PlayGlobal(config.GreetingSound, Filter.Empty().AddPlayer(session), false, AudioParams.Default);
        _chatMan.DispatchServerMessage(session, Loc.GetString("ninja-role-greeting"));
    }

    /// <summary>
    /// Handle constant power drains from passive usage and cloak.
    /// </summary>
    private void UpdateNinja(EntityUid uid, SpaceNinjaComponent ninja, float frameTime)
    {
        if (ninja.Suit == null)
            return;

        float wattage = _suit.SuitWattage(ninja.Suit.Value);

        SetSuitPowerAlert(uid, ninja);
        if (!TryUseCharge(uid, wattage * frameTime))
        {
            // ran out of power, uncloak ninja
            _stealthClothing.SetEnabled(ninja.Suit.Value, uid, false);
        }
    }

    /// <summary>
    /// Helper function for objectives.
    /// </summary>
    private void AddObjective(Mind.Mind mind, string name)
    {
        if (_proto.TryIndex<ObjectivePrototype>(name, out var objective))
        {
            _mind.TryAddObjective(mind, objective);
        }
        else
        {
            Logger.Error($"Ninja has unknown objective prototype: {name}");
        }
    }

    /// <summary>
    /// Increment greentext when emagging a door.
    /// </summary>
    private void OnDoorjack(EntityUid uid, SpaceNinjaComponent comp, ref EmaggedSomethingEvent args)
    {
        // incase someone lets ninja emag non-doors double check it here
        if (!HasComp<DoorComponent>(args.Target))
            return;

        // this popup is serverside since door emag logic is serverside (power funnies)
        _popup.PopupEntity(Loc.GetString("ninja-doorjack-success", ("target", Identity.Entity(args.Target, EntityManager))), uid, uid, PopupType.Medium);

        // make sure it's a ninja doorjacking it
        if (GetNinjaRole(uid, out var role))
            role.DoorsJacked++;
    }
}
