using System.Linq;
using Content.Server.StationEvents.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class MouseMigration : StationEventSystem
{
    public static List<string> SpawnedPrototypeChoices = new List<string>() //we double up for that ez fake probability
        {"MobMouse", "MobMouse1", "MobMouse2", "MobRatServant"};

    public override string Prototype => "MouseMigration";

    public override void Started()
    {
        base.Started();

        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        var spawnAmount = RobustRandom.Next(7, 15); // A small colony of critters.

        for (int i = 0; i < spawnAmount && i < spawnLocations.Count - 1; i++)
        {
            var spawnChoice = RobustRandom.Pick(SpawnedPrototypeChoices);
            if (RobustRandom.Prob(0.01f) || i == 0) //small chance for multiple, but always at least 1
                spawnChoice = "SpawnPointGhostRatKing";

            var coords = spawnLocations[i].Item2.Coordinates;
            Sawmill.Info($"Spawning mouse {spawnChoice} at {coords}");
            EntityManager.SpawnEntity(spawnChoice, coords);
        }
    }
}
