using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.StationEvents.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared.Whitelist;
using JetBrains.Annotations;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed partial class BreakerFlipRule : StationEventSystem<BreakerFlipRuleComponent>
{
    [Dependency] private ApcSystem _apcSystem = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    protected override void Added(EntityUid uid, BreakerFlipRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        if (!TryComp<StationEventComponent>(uid, out var stationEvent))
            return;

        var str = Loc.GetString("station-event-breaker-flip-announcement", ("data", Loc.GetString($"random-sentience-event-data-{RobustRandom.Next(1, 6)}")));
        stationEvent.StartAnnouncement = str;

        base.Added(uid, component, gameRule, args);
    }

    protected override void Started(EntityUid uid, BreakerFlipRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var stationApcs = GetEntitiesWithComponentOnStation<ApcComponent>(true).ToList();

        var toDisable = Math.Min(RobustRandom.Next(3, 7), stationApcs.Count);
        if (toDisable == 0)
            return;

        RobustRandom.Shuffle(stationApcs);

        for (var i = 0; i < toDisable; i++)
        {
            var apc = stationApcs[i];
            _apcSystem.ApcToggleBreaker(apc, apc);

            var stateString = apc.Comp.MainBreakerEnabled ? "Enabled" : "Disabled";
            AdminLogManager.Add(LogType.ItemConfigure, LogImpact.Medium,
                $"Station event {ToPrettyString(uid):user} set the main breaker state of {ToPrettyString(apc):entity} to {stateString:state}");
        }
    }
}
