using Content.Server.StationEvents.Components;
using Content.Server.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Server.GameTicking.Rules.Components;

namespace Content.Server.StationEvents.Events;

public sealed class BlobSpawnRule : StationEventSystem<BlobSpawnRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Started(EntityUid uid, BlobSpawnRuleComponent component, GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
        {
            return;
        }

        var locations = EntityQueryEnumerator<VentCritterSpawnLocationComponent, TransformComponent>();
        var validLocations = new List<EntityCoordinates>();
        while (locations.MoveNext(out _, out _, out var transform))
        {
            if (CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == station)
            {
                validLocations.Add(transform.Coordinates);
            }
        }

        if (validLocations.Count == 0)
        {
            Sawmill.Info("No find any valid spawn location for blob");
            return;
        }

        var coords = _random.Pick(validLocations);
        Sawmill.Info($"Creating blob spawnpoint at {coords}");
        var spawner = Spawn(component.SpawnPointProto, coords);

        // start blob rule incase it isn't, for the sweet greentext
        GameTicker.StartGameRule("Blob");
    }
}
