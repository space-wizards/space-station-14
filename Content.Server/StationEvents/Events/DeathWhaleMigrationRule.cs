using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class DeathWhaleSpawnRule : StationEventSystem<DeathWhaleSpawnRuleComponent>
{
    protected override void Started(EntityUid uid, DeathWhaleSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var station))
        {
            return;
        }

        var locations = EntityQueryEnumerator<DeathWhaleSpawnLocationComponent, TransformComponent>();
        var validLocations = new List<EntityCoordinates>();


        while (locations.MoveNext(out var _, out var spawnLocation, out var transform))
        {

                validLocations.Add(transform.Coordinates);

                // Spawn the Death Whale at the location
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

     protected virtual void Ended(EntityUid uid, DeathWhaleSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        foreach (var whales in EntityManager.EntityQuery<DeathWhaleComponent>())
            {
                var uid = whales.Owner;
                QueueDel(uid);
            }
    }
}
