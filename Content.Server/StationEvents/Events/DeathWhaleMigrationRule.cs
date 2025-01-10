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
                Log.Error($"Death Whale spawned");
        }

        if (validLocations.Count == 0)
        {
            return;
        }

        foreach (var location in validLocations)
        {
            Spawn(comp.Prototype, location);
            Log.Error($"Death Whale spawned");
        }
    }
}
