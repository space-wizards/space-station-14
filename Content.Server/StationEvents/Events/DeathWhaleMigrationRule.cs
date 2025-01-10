using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

//summary
// This system allows for more control than ventcritters does, letting you choose the target component to spawn them at.
// This allows for multiple spawn point markers to be chosen depending on what you want, and also lets you choose the creature.
//summary

public sealed class OceanSpawnRule : StationEventSystem<OceanSpawnSpawnRuleComponent>
{
    protected override void Started(EntityUid uid, OceanSpawnSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var station))
        {
            return;
        }

        var locations = EntityQueryEnumerator<comp.Target, TransformComponent>();
        var validLocations = new List<EntityCoordinates>();


        while (locations.MoveNext(out var _, out var spawnLocation, out var transform))
        {

                validLocations.Add(transform.Coordinates);

                // Spawn the creature at the location
                Spawn(comp.Prototype, transform.Coordinates);
        }

        if (validLocations.Count == 0)
        {
            return;
        }

        foreach (var location in validLocations)
        {
            Spawn(comp.Prototype, location);
        }
    }

     protected virtual void Ended(EntityUid uid, OceanSpawnSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        foreach (var whales in EntityManager.EntityQuery<DeathWhaleComponent>()) // Clears out Deathwhales after they've spawned for DeathWhaleMigration
            {
                var uid = whales.Owner;
                QueueDel(uid);
            }
    }
}
