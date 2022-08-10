using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
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

        var allApcs = EntityQuery<ApcComponent>().ToList();
        var toDisable = Math.Min(RobustRandom.Next(3, 7), allApcs.Count);
        if (toDisable == 0)
            return;

        RobustRandom.Shuffle(allApcs);

        for (var i = 0; i < toDisable; i++)
        {
            _apcSystem.ApcToggleBreaker(allApcs[i].Owner, allApcs[i]);
        }
    }
}
