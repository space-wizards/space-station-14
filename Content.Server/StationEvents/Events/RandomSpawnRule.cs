using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.StationEvents.Events;

public sealed class RandomSpawnRule : StationEventSystem<RandomSpawnRuleComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    protected override void Started(EntityUid uid, RandomSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        for (var i = 0; i < 10; i++) // infinite loop prevention
        {
            if (TryFindRandomTile(out _, out _, out _, out var coords))
            {
                if (_lookup.GetEntitiesInRange<PreventEventMobsSpawnComponent>(coords, range: 1).Any())
                {
                    continue;
                }

                Sawmill.Info($"Spawning {comp.Prototype} at {coords}");
                Spawn(comp.Prototype, coords);
                break;
            }
        }
    }
}
