using Content.Server.StationEvents.Components;
using Robust.Shared.Random;
using System.Linq;
using Content.Server.GameTicking.Rules.Components;

namespace Content.Server.StationEvents.Events;

public sealed class VentCrittersRule : StationEventSystem<VentCrittersRuleComponent>
{
    /*
     * DO NOT COPY PASTE THIS TO MAKE YOUR MOB EVENT.
     * USE THE PROTOTYPE.
     */

    protected override void Started(EntityUid uid, VentCrittersRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var spawnChoice = RobustRandom.Pick(component.Entries);
        // TODO: What we should actually do is take the component count and then multiply a prob by that
        // then just iterate until we get it
        // This will be on average twice as fast.
        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent>().ToList();
        RobustRandom.Shuffle(spawnLocations);

        // A small colony of critters.
        var spawnAmount = RobustRandom.Next(spawnChoice.Amount, spawnChoice.MaxAmount);
        Sawmill.Info($"Spawning {spawnAmount} of {spawnChoice}");
        foreach (var location in spawnLocations)
        {
            if (spawnAmount-- == 0)
                break;

            var coords = Transform(location.Owner);
            Spawn(spawnChoice.PrototypeId, coords.Coordinates);
        }
    }
}
