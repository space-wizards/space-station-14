using Content.Server.StationEvents.Components;
using Content.Shared.Actions;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.StationEvents.Events;

public sealed class SlimesSpawn : StationEventSystem
{
    public static List<string> SpawnedPrototypeChoices = new()
        {"MobAdultSlimesBlueAngry", "MobAdultSlimesGreenAngry", "MobAdultSlimesYellowAngry"};

    public override string Prototype => "SlimesSpawn";

    public override void Started()
    {
        base.Started();
        var spawnLocations = EntityManager.EntityQuery<VentScrubberSpawnLocationComponent, TransformComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        var mod = Math.Sqrt(GetSeverityModifier());

        var spawnAmount = (int) (RobustRandom.Next(6, 10) * mod);
        for (int i = 0; i < spawnAmount && i < spawnLocations.Count - 1; i++)
        {
            if (spawnAmount-- == 0)
                break;
            var spawnChoice = RobustRandom.Pick(SpawnedPrototypeChoices.ToList());
            Sawmill.Info($"Spawning {spawnAmount} of {spawnChoice}");
            var coords = spawnLocations[i].Item2.Coordinates;

            Spawn(spawnChoice, coords);
        }
    }
}
