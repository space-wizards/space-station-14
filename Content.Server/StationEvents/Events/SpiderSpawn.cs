using Content.Server.StationEvents.Components;
using Content.Shared.Actions;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.StationEvents.Events;

public sealed class SpiderSpawn : StationEventSystem
{
    public override string Prototype => "SpiderSpawn";

    public override void Started()
    {
        base.Started();
        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        var mod = Math.Sqrt(GetSeverityModifier());

        var spawnAmount = (int) (RobustRandom.Next(4, 8) * mod);
        Sawmill.Info($"Spawning {spawnAmount} of spiders");
        foreach (var location in spawnLocations)
        {
            if (spawnAmount-- == 0)
                break;

            var coords = EntityManager.GetComponent<TransformComponent>(location.Owner);

            EntityManager.SpawnEntity("MobGiantSpiderAngry", coords.Coordinates);
        }
    }
}
