using Content.Server.StationEvents.Components;
using Content.Shared.Actions;
using Robust.Shared.Random;
using System.Linq;
using Content.Server.GameTicking.Rules.Components;

namespace Content.Server.StationEvents.Events;

public sealed class SlimesSpawnRule : StationEventSystem<SlimesSpawnRuleComponent>
{
    protected override void Started(EntityUid uid, SlimesSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
		
        var spawnLocations = EntityManager.EntityQuery<MobScrubberSpawnLocationComponent, TransformComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        var mod = Math.Sqrt(GetSeverityModifier());

        var spawnAmount = (int) (RobustRandom.Next(6, 10) * mod);
        for (int i = 0; i < spawnAmount && i < spawnLocations.Count - 1; i++)
        {
            if (spawnAmount-- == 0)
                break;
            var spawnChoice = RobustRandom.Pick(component.SpawnedPrototypeChoices.ToList());
            Sawmill.Info($"Spawning {spawnAmount} of {spawnChoice}");
            var coords = spawnLocations[i].Item2.Coordinates;

            Spawn(spawnChoice, coords);
        }
    }
}
