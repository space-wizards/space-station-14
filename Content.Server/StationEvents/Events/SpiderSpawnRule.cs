using Content.Server.StationEvents.Components;
using System.Linq;
using Content.Server.GameTicking.Rules.Components;

namespace Content.Server.StationEvents.Events;

public sealed class SpiderSpawnRule : StationEventSystem<SpiderSpawnRuleComponent>
{
    protected override void Started(EntityUid uid, SpiderSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        var spawnLocations = EntityQuery<VentCritterSpawnLocationComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        var mod = Math.Sqrt(GetSeverityModifier());

        var spawnAmount = (int) (RobustRandom.Next(4, 8) * mod);
        Sawmill.Info($"Spawning {spawnAmount} of spiders");
        foreach (var location in spawnLocations)
        {
            if (spawnAmount-- == 0)
                break;

            var xform = Transform(location.Owner);
            Spawn("MobGiantSpiderAngry", xform.Coordinates);
        }
    }
}
