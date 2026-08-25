using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.StationEvents.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared.Whitelist;
using JetBrains.Annotations;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// The system driving the logic for the breaker flip.
/// Disables a random number of APCs (default between 3-7) on a random station.
/// </summary>
[UsedImplicitly]
public sealed partial class BreakerFlipRule : StationEventSystem<BreakerFlipRuleComponent>
{
    [Dependency] private ApcSystem _apcSystem = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [Dependency] private EntityQuery<StationEventComponent> _stationEventQuery;
    [Dependency] private EntityQuery<StationMemberComponent> _stationMemberQuery;

    // Minimum/maximum number of APCs to trigger.
    private const int MinAPCs = 3;
    private const int MaxAPCs = 7;

    protected override void Added(EntityUid uid, BreakerFlipRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (!_stationEventQuery.TryComp(uid, out var stationEvent))
            return;

        var str = Loc.GetString("station-event-breaker-flip-announcement", ("data", Loc.GetString($"random-sentience-event-data-{RobustRandom.Next(1, 6)}")));
        stationEvent.StartAnnouncement = str;

        base.Added(uid, component, gameRule, args);
    }

    protected override void Started(EntityUid uid, BreakerFlipRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        var stationApcs = new List<(Entity<ApcComponent> apc, EntityUid grid)>();
        var query = EntityQueryEnumerator<ApcComponent, TransformComponent>();
        while (query.MoveNext(out var apcUid, out var apc, out var xform))
        {
            if (apc.MainBreakerEnabled
                && xform.GridUid is { } grid
                && _stationMemberQuery.CompOrNull(grid)?.Station == chosenStation)
            {
                stationApcs.Add(((apcUid, apc), grid));
            }
        }

        var toDisable = Math.Min(RobustRandom.Next(MinAPCs, MaxAPCs), stationApcs.Count);
        if (toDisable == 0)
            return;

        RobustRandom.Shuffle(stationApcs);

        foreach (var (apc, grid) in stationApcs)
        {
            // If the APC's grid matches our blacklist, skip to the next one.
            if (_whitelist.IsWhitelistPass(component.Blacklist, grid))
                continue;

            _apcSystem.ApcToggleBreaker(apc, apc);

            var stateString = apc.Comp.MainBreakerEnabled ? "Enabled" : "Disabled";
            AdminLogManager.Add(LogType.ItemConfigure, LogImpact.Medium,
                $"Station event {ToPrettyString(uid):user} set the main breaker state of {ToPrettyString(apc):entity} to {stateString:state}");

            if (--toDisable <= 0)
                break;
        }
    }
}
