using Content.Server.Administration.Commands;
using Content.Server.Communications;
using Content.Server.Chat.Managers;
using Content.Server.StationEvents.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Objectives;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.PowerCell;
using Content.Server.Research.Systems;
using Content.Server.Roles;
using Content.Server.GenericAntag;
using Content.Server.Warps;
using Content.Shared.Alert;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Doors.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.PowerCell.Components;
using Content.Shared.Rounding;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Ninja.Systems;

// TODO: when syndiborgs are a thing have a borg converter with 6 second doafter
// engi -> saboteur
// medi -> idk reskin it
// other -> assault
// TODO: when criminal records is merged, hack it to set everyone to arrest

/// <summary>
/// Main ninja system that handles ninja setup, provides helper methods for the rest of the code to use.
/// </summary>
public sealed class SpaceNinjaSystem : SharedSpaceNinjaSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly GenericAntagSystem _genericAntag = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StealthClothingSystem _stealthClothing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceNinjaComponent, GenericAntagCreatedEvent>(OnNinjaCreated);
        SubscribeLocalEvent<SpaceNinjaComponent, EmaggedSomethingEvent>(OnDoorjack);
        SubscribeLocalEvent<SpaceNinjaComponent, ResearchStolenEvent>(OnResearchStolen);
        SubscribeLocalEvent<SpaceNinjaComponent, ThreatCalledInEvent>(OnThreatCalledIn);
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
    /// Download the given set of nodes, returning how many new nodes were downloaded.
    /// </summary>
    private int Download(EntityUid uid, List<string> ids)
    {
        if (!_mind.TryGetRole<NinjaRoleComponent>(uid, out var role))
            return 0;

        var oldCount = role.DownloadedNodes.Count;
        role.DownloadedNodes.UnionWith(ids);
        var newCount = role.DownloadedNodes.Count;
        return newCount - oldCount;
    }

    /// <summary>
    /// Returns a ninja's gamerule config data.
    /// If the gamerule was not started then it will be started automatically.
    /// </summary>
    public NinjaRuleComponent? NinjaRule(EntityUid uid, GenericAntagComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return null;

        // mind not added yet so no rule
        if (comp.RuleEntity == null)
            return null;

        return CompOrNull<NinjaRuleComponent>(comp.RuleEntity);
    }

    // TODO: can probably copy paste borg code here
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

        if (GetNinjaBattery(uid, out _, out var battery))
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
    /// Set up everything for ninja to work and send the greeting message/sound.
    /// Objectives are added by <see cref="GenericAntagSystem"/>.
    /// </summary>
    private void OnNinjaCreated(EntityUid uid, SpaceNinjaComponent comp, ref GenericAntagCreatedEvent args)
    {
        var mindId = args.MindId;
        var mind = args.Mind;

        if (mind.Session == null)
            return;

        var config = NinjaRule(uid);
        if (config == null)
            return;

        var role = new NinjaRoleComponent
        {
            PrototypeId = "SpaceNinja"
        };
        _role.MindAddRole(mindId, role, mind);

        // choose spider charge detonation point
        var warps = new List<EntityUid>();
        var query = EntityQueryEnumerator<BombingTargetComponent, WarpPointComponent, TransformComponent>();
        var map = Transform(uid).MapID;
        while (query.MoveNext(out var warpUid, out _, out var warp, out var xform))
        {
            if (warp.Location != null)
                warps.Add(warpUid);
        }

        if (warps.Count > 0)
            role.SpiderChargeTarget = _random.Pick(warps);

        var session = mind.Session;
        _audio.PlayGlobal(config.GreetingSound, Filter.Empty().AddPlayer(session), false, AudioParams.Default);
        _chatMan.DispatchServerMessage(session, Loc.GetString("ninja-role-greeting"));
    }

    // TODO: PowerCellDraw, modify when cloak enabled
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
    /// Increment greentext when emagging a door.
    /// </summary>
    private void OnDoorjack(EntityUid uid, SpaceNinjaComponent comp, ref EmaggedSomethingEvent args)
    {
        // incase someone lets ninja emag non-doors double check it here
        if (!HasComp<DoorComponent>(args.Target))
            return;

        // this popup is serverside since door emag logic is serverside (power funnies)
        _popup.PopupEntity(Loc.GetString("ninja-doorjack-success", ("target", Identity.Entity(args.Target, EntityManager))), uid, uid, PopupType.Medium);

        // handle greentext
        if (_mind.TryGetRole<NinjaRoleComponent>(uid, out var role))
            role.DoorsJacked++;
    }

    /// <summary>
    /// Add to greentext when stealing technologies.
    /// </summary>
    private void OnResearchStolen(EntityUid uid, SpaceNinjaComponent comp, ref ResearchStolenEvent args)
    {
        var gained = Download(uid, args.Techs);
        var str = gained == 0
            ? Loc.GetString("ninja-research-steal-fail")
            : Loc.GetString("ninja-research-steal-success", ("count", gained), ("server", args.Target));

        _popup.PopupEntity(str, uid, uid, PopupType.Medium);
    }

    private void OnThreatCalledIn(EntityUid uid, SpaceNinjaComponent comp, ref ThreatCalledInEvent args)
    {
        if (_mind.TryGetRole<NinjaRoleComponent>(uid, out var role))
        {
            role.CalledInThreat = true;
        }
    }
}
