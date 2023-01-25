using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Components;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class BreakerFlip : StationEventSystem
{
    [Dependency] private readonly ApcSystem _apcSystem = default!;

    public override string Prototype => "BreakerFlip";

    public override void Added()
    {
        base.Added();

        var str = Loc.GetString("station-event-breaker-flip-announcement", ("data", Loc.GetString(Loc.GetString($"random-sentience-event-data-{RobustRandom.Next(1, 6)}"))));
        ChatSystem.DispatchGlobalAnnouncement(str, playSound: false, colorOverride: Color.Gold);
    }

    public override void Started()
    {
        base.Started();

        if (StationSystem.Stations.Count == 0)
            return;
        var chosenStation = RobustRandom.Pick(StationSystem.Stations.ToList());

        var allApcs = EntityQuery<ApcComponent, TransformComponent>().ToList();
        allApcs = allApcs.FindAll((ent) => 
        {
            var (apc, transform) = ent;
            return apc.MainBreakerEnabled && CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == chosenStation;
        });
        var toDisable = Math.Min(RobustRandom.Next(3, 7), allApcs.Count);
        if (toDisable == 0)
            return;

        RobustRandom.Shuffle(allApcs);

        for (var i = 0; i < toDisable; i++)
        {
            var (apc, _) = allApcs[i];
            _apcSystem.ApcToggleBreaker(apc.Owner, apc);
        }
    }
}
