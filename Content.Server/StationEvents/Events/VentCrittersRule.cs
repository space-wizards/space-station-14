using Content.Server.StationEvents.Components;
using Robust.Shared.Random;
using System.Linq;
using Content.Server.GameTicking.Rules.Components;

namespace Content.Server.StationEvents.Events;

public sealed class VentCrittersRule : StationEventSystem<VentCrittersRuleComponent>
{
    protected override void Started(EntityUid uid, VentCrittersRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var spawnChoice = RobustRandom.Pick(component.SpawnedPrototypeChoices);
        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        var spawnAmount = RobustRandom.Next(4, 12); // A small colony of critters.
        Sawmill.Info($"Spawning {spawnAmount} of {spawnChoice}");
        foreach (var location in spawnLocations)
        {
            if (spawnAmount-- == 0)
                break;

            var coords = Transform(location.Owner);
            Spawn(spawnChoice, coords.Coordinates);
        }
    }
}
