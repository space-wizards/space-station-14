using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Doors.Components;
using Content.Server.Doors.Systems;
using Content.Shared.Doors.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class CargoIntruders : StationEventSystem
{
    public static List<string> CommonPrototypeChoices = new List<string>()
        {"MobMouse", "MobMouse1", "MobMouse2", "MobRatServant", "MobGiantSpider", "SalvageHumanCorpse", "SalvageMobSpawner75"};

    public static List<string> RarePrototypeChoices = new List<string>()
        {"MobGiantSpider", "MobXeno", "MobDragon"};

    public override string Prototype => "CargoIntruders";

    public override void Started()
    {
        base.Started();

        var modifier = GetSeverityModifier();

        var intruderAmount = (int) (RobustRandom.Next(1, 2) * Math.Sqrt(modifier)); // a teeny amount of intruders
        var spawnLocations = EntityQuery<CargoPalletComponent, TransformComponent>(true).ToList();
        RobustRandom.Shuffle(spawnLocations);

        for (var i = 0; i < intruderAmount && i < spawnLocations.Count; i++)
        {
            var spawnChoice = RobustRandom.Pick(CommonPrototypeChoices);
            if (RobustRandom.Prob(0.1f))
            {
                spawnChoice = RobustRandom.Pick(RarePrototypeChoices);
            }

            var coords = spawnLocations[i].Item2.Coordinates;
            Sawmill.Info($"Spawning intruder {spawnChoice}");

            EntityManager.SpawnEntity(spawnChoice, coords);
        }
    }
}
