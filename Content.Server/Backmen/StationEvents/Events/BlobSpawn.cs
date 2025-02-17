using System.Linq;
using Content.Server.Backmen.StationEvents.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Server.StationEvents.Events;
using Content.Shared.Backmen.Blob.Components;
using Robust.Server.Player;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;

namespace Content.Server.Backmen.StationEvents.Events;

public sealed class BlobSpawnRule : StationEventSystem<BlobSpawnRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _playerSystem = default!;

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
            if (!HasComp<BecomesStationComponent>(transform.GridUid))
                continue;

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

        var playerPool = _playerSystem.Sessions.ToList();
        var numBlobs = MathHelper.Clamp(playerPool.Count / component.PlayersPerCarrierBlob, 1, component.MaxCarrierBlob);

        for (var i = 0; i < numBlobs; i++)
        {
            var coords = _random.Pick(validLocations);
            Sawmill.Info($"Creating carrier blob at {coords}");
            var carrier = Spawn(_random.Pick(component.CarrierBlobProtos), coords);
            EnsureComp<BlobCarrierComponent>(carrier);
        }

        // start blob rule incase it isn't, for the sweet greentext
        GameTicker.StartGameRule("Blob");
    }
}
