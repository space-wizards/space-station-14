using Content.Server.Communications;
using Content.Server.CriminalRecords.Systems;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Research.Systems;
using Content.Shared.Alert;
using Content.Shared.Doors.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Popups;
using Content.Shared.Rounding;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Main ninja system that handles ninja setup, provides helper methods for the rest of the code to use.
/// </summary>
public sealed class SpaceNinjaSystem : SharedSpaceNinjaSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly PredictedBatterySystem _battery = default!;
    [Dependency] private readonly CodeConditionSystem _codeCondition = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceNinjaComponent, EmaggedSomethingEvent>(OnDoorjack);
        SubscribeLocalEvent<SpaceNinjaComponent, ResearchStolenEvent>(OnResearchStolen);
        SubscribeLocalEvent<SpaceNinjaComponent, ThreatCalledInEvent>(OnThreatCalledIn);
        SubscribeLocalEvent<SpaceNinjaComponent, CriminalRecordsHackedEvent>(OnCriminalRecordsHacked);
    }

    // TODO: Make this charge rate based instead of updating it every single tick.
    // Or make it client side, since power cells are predicted.
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SpaceNinjaComponent>();
        while (query.MoveNext(out var uid, out var ninja))
        {
            SetSuitPowerAlert((uid, ninja));
        }
    }

    /// <summary>
    /// Download the given set of nodes, returning how many new nodes were downloaded.
    /// </summary>
    private int Download(EntityUid uid, List<string> ids)
    {
        if (!_mind.TryGetObjectiveComp<StealResearchConditionComponent>(uid, out var obj))
            return 0;

        var oldCount = obj.DownloadedNodes.Count;
        obj.DownloadedNodes.UnionWith(ids);
        var newCount = obj.DownloadedNodes.Count;
        return newCount - oldCount;
    }

    // TODO: Generic charge indicator that is combined with borg code.
    /// <summary>
    /// Update the alert for the ninja's suit power indicator.
    /// </summary>
    public void SetSuitPowerAlert(Entity<SpaceNinjaComponent> ent)
    {
        var (uid, comp) = ent;
        if (comp.Deleted || comp.Suit == null)
        {
            _alerts.ClearAlert(uid, comp.SuitPowerAlert);
            return;
        }

        if (GetNinjaBattery(uid, out var batteryUid, out var batteryComp))
        {
            var severity = ContentHelpers.RoundToLevels(MathF.Max(0f, _battery.GetCharge((batteryUid.Value, batteryComp))), batteryComp.MaxCharge, 8);
            _alerts.ShowAlert(uid, comp.SuitPowerAlert, (short)severity);
        }
        else
        {
            _alerts.ClearAlert(uid, comp.SuitPowerAlert);
        }
    }

    /// <summary>
    /// Get the battery component in a ninja's suit, if it's worn.
    /// </summary>
    public bool GetNinjaBattery(EntityUid user, [NotNullWhen(true)] out EntityUid? batteryUid, [NotNullWhen(true)] out PredictedBatteryComponent? batteryComp)
    {
        if (TryComp<SpaceNinjaComponent>(user, out var ninja)
            && ninja.Suit != null
            && _powerCell.TryGetBatteryFromSlot(ninja.Suit.Value, out var battery))
        {
            batteryUid = battery.Value.Owner;
            batteryComp = battery.Value.Comp;
            return true;
        }

        batteryUid = null;
        batteryComp = null;
        return false;
    }

    /// <inheritdoc/>
    public override bool TryUseCharge(EntityUid user, float charge)
    {
        return GetNinjaBattery(user, out var uid, out var battery) && _battery.TryUseCharge((uid.Value, battery), charge);
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
        Popup.PopupEntity(Loc.GetString("ninja-doorjack-success", ("target", Identity.Entity(args.Target, EntityManager))), uid, uid, PopupType.Medium);

        // handle greentext
        if (_mind.TryGetObjectiveComp<DoorjackConditionComponent>(uid, out var obj))
            obj.DoorsJacked++;
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

        Popup.PopupEntity(str, uid, uid, PopupType.Medium);
    }

    private void OnThreatCalledIn(Entity<SpaceNinjaComponent> ent, ref ThreatCalledInEvent args)
    {
        _codeCondition.SetCompleted(ent.Owner, ent.Comp.TerrorObjective);
    }

    private void OnCriminalRecordsHacked(Entity<SpaceNinjaComponent> ent, ref CriminalRecordsHackedEvent args)
    {
        _codeCondition.SetCompleted(ent.Owner, ent.Comp.MassArrestObjective);
    }

    /// <summary>
    /// Called by <see cref="SpiderChargeSystem"/> when it detonates.
    /// </summary>
    public void DetonatedSpiderCharge(Entity<SpaceNinjaComponent> ent)
    {
        _codeCondition.SetCompleted(ent.Owner, ent.Comp.SpiderChargeObjective);
    }
}
