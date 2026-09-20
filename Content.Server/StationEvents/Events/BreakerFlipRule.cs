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
/// Handler for events that turn off a handful of APCs on a station.
/// </summary>
/// <seealso cref="BreakerFlipRuleComponent"/>
[UsedImplicitly]
public sealed partial class BreakerFlipRule : StationEventSystem<BreakerFlipRuleComponent>
{
    [Dependency] private ApcSystem _apcSystem = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [Dependency] private EntityQuery<StationEventComponent> _stationEventQuery;
    [Dependency] private EntityQuery<StationMemberComponent> _stationMemberQuery;

    protected override void Added(Entity<BreakerFlipRuleComponent, GameRuleComponent> ent, ref GameRuleAddedEvent args)
    {
        if (!_stationEventQuery.TryComp(uid, out var stationEvent))
            return;

        var str = Loc.GetString("station-event-breaker-flip-announcement", ("data", Loc.GetString($"random-sentience-event-data-{RobustRandom.Next(1, 6)}")));
        stationEvent.StartAnnouncement = str;

        base.Added(ent, ref args);
    }

    protected override void Started(Entity<BreakerFlipRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        Station.TryGetRandomStation<StationEventEligibleComponent>(out var chosenEnt);
        if (chosenEnt is not { } chosenStation)
            return;

        var stationApcs = new List<(Entity<ApcComponent> apc, EntityUid grid)>(Count<ApcComponent>());
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

        var breakerFlip = ent.Comp1;

        var toDisable = Math.Min(breakerFlip.ApcCount.Next(RobustRandom), stationApcs.Count);
        if (toDisable <= 0)
            return;

        RobustRandom.Shuffle(stationApcs);

        var disabled = 0;
        foreach (var (apc, grid) in stationApcs)
        {
            // If the APC's grid matches our blacklist, skip to the next one.
            if (_whitelist.IsWhitelistPass(breakerFlip.Blacklist, grid))
                continue;

            _apcSystem.ApcToggleBreaker(apc, apc);

            var stateString = apc.Comp.MainBreakerEnabled ? "Enabled" : "Disabled";
            AdminLogManager.Add(LogType.ItemConfigure, LogImpact.Medium,
                $"Station event {ToPrettyString(ent):user} set the main breaker state of {ToPrettyString(apc):entity} to {stateString:state}");

            disabled++;
            if (disabled >= toDisable)
                return;
        }
    }
}
