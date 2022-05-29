using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class BreakerFlip : StationEvent
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override string Name => "BreakerFlip";
    public override string? StartAnnouncement =>
        Loc.GetString("station-event-breaker-flip-announcement", ("data", Loc.GetString(Loc.GetString($"random-sentience-event-data-{_random.Next(1, 6)}"))));
    public override float Weight => WeightNormal;
    protected override float EndAfter => 1.0f;
    public override int? MaxOccurrences => 5;
    public override int MinimumPlayers => 15;

    public override void Startup()
    {
        base.Startup();

        var apcSys = EntitySystem.Get<ApcSystem>();
        var allApcs = _entityManager.EntityQuery<ApcComponent>().ToList();
        var toDisable = Math.Min(_random.Next(3, 7), allApcs.Count);
        if (toDisable == 0)
            return;

        _random.Shuffle(allApcs);

        for (var i = 0; i < toDisable; i++)
        {
            apcSys.ApcToggleBreaker(allApcs[i].Owner, allApcs[i]);
        }
    }
}
