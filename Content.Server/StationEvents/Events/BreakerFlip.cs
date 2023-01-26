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

        var stationApcs = new List<ApcComponent>();
        foreach (var (apc, transform) in EntityQuery<ApcComponent, TransformComponent>()) 
        {
            if (apc.MainBreakerEnabled && CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == chosenStation)
            {
                stationApcs.Add(apc);
            }
        }
        
        var toDisable = Math.Min(RobustRandom.Next(3, 7), stationApcs.Count);
        if (toDisable == 0)
            return;

        RobustRandom.Shuffle(stationApcs);

        for (var i = 0; i < toDisable; i++)
        {
            _apcSystem.ApcToggleBreaker(stationApcs[i].Owner, stationApcs[i]);
        }
    }
}
