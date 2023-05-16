using System.Linq;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class MouseMigrationRule : StationEventSystem<MouseMigrationRuleComponent>
{
    protected override void Started(EntityUid uid, MouseMigrationRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var modifier = GetSeverityModifier();

        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        // sqrt so we dont get insane values for ramping events
        var spawnAmount = (int) (RobustRandom.Next(7, 15) * Math.Sqrt(modifier)); // A small colony of critters.

        for (var i = 0; i < spawnAmount && i < spawnLocations.Count - 1; i++)
        {
            var spawnChoice = RobustRandom.Pick(component.SpawnedPrototypeChoices);
            if (RobustRandom.Prob(Math.Min(0.01f * modifier, 1.0f)) || i == 0) //small chance for multiple, but always at least 1
                spawnChoice = "SpawnPointGhostRatKing";

            var coords = spawnLocations[i].Item2.Coordinates;
            Sawmill.Info($"Spawning mouse {spawnChoice} at {coords}");
            EntityManager.SpawnEntity(spawnChoice, coords);
        }
    }
}
