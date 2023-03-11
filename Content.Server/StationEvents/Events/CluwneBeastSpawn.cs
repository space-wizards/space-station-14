using System.Linq;
using Content.Server.StationEvents.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class CluwneBeastSpawn : StationEventSystem
{
    public static List<string> SpawnedPrototypeChoices = new List<string>()
        {"MobMouse", "MobMouse1", "MobMouse2"};
    public override string Prototype => "CluwneBeastSpawn";

    public override void Started()
    {
        base.Started();

        var modifier = GetSeverityModifier();

        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        var spawnAmount = (int) (RobustRandom.Next(1, 3) * Math.Sqrt(modifier));

        for (int i = 0; i < spawnAmount && i < spawnLocations.Count - 1; i++)
        {
            var spawnChoice = RobustRandom.Pick(SpawnedPrototypeChoices);
            if (RobustRandom.Prob(Math.Min(0.01f * modifier, 1.0f)) || i == 0)
                spawnChoice = "SpawnPointGhostCluwneBeast";

            var coords = spawnLocations[i].Item2.Coordinates;
            Sawmill.Info($"Spawning cluwnebeast {spawnChoice} at {coords}");
            EntityManager.SpawnEntity(spawnChoice, coords);
        }
    }
}
