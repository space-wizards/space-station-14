using System.Linq;
using Content.Server.StationEvents.Components;

namespace Content.Server.StationEvents.Events;

public sealed class CluwneBeastSpawn : StationEventSystem
{
    public override string Prototype => "CluwneBeastSpawn";

    public override void Started()
    {
        base.Started();
        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        var mod = Math.Sqrt(GetSeverityModifier());

        var spawnAmount = (1);
        Sawmill.Info($"Spawning {spawnAmount} of cluwnebeasts");
        foreach (var location in spawnLocations)
        {
            if (spawnAmount-- == 0)
                break;

            var coords = EntityManager.GetComponent<TransformComponent>(location.Owner);

            EntityManager.SpawnEntity("SpawnPointGhostCluwneBeast", coords.Coordinates);
        }
    }
}
