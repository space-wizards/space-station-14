using Content.Server.StationEvents.Components;
using Content.Shared.Actions;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.StationEvents.Events;

public sealed class SlimesSpawn : StationEventSystem
{
    public static List<string> SpawnedPrototypeChoices = new List<string>()
        {"MobAdultSlimesBlueAngry", "MobAdultSlimesGreenAngry", "MobAdultSlimesYellowAngry"};

    public override string Prototype => "SlimesSpawn";

    public override void Started()
    {
        base.Started();
        var spawnChoice = RobustRandom.Pick(SpawnedPrototypeChoices);
        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        var mod = Math.Sqrt(GetSeverityModifier());

        var spawnAmount = (int) (RobustRandom.Next(4, 8) * mod);
        Sawmill.Info($"Spawning {spawnAmount} of {spawnChoice}");
        foreach (var location in spawnLocations)
        {
            if (spawnAmount-- == 0)
                break;

            var coords = EntityManager.GetComponent<TransformComponent>(location.Owner);

            EntityManager.SpawnEntity(spawnChoice, coords.Coordinates);
        }
    }
}
