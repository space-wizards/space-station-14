using Content.Server.StationEvents.Components;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.StationEvents.Events;

public sealed class VentCritters : StationEventSystem
{
    public static List<string> SpawnedPrototypeChoices = new List<string>()
        {"MobGiantSpiderAngry", "MobMouse", "MobMouse1", "MobMouse2"};

    public override string Prototype => "VentCritters";

    public override void Started()
    {
        base.Started();
        var spawnChoice = RobustRandom.Pick(SpawnedPrototypeChoices);
        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        var mod = Math.Sqrt(GetSeverityModifier());

        var spawnAmount = (int) (RobustRandom.Next(4, 12) * mod); // A small colony of critters.
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
