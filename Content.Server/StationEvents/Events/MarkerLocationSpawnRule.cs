using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Spawns a selection of entities at locations with <see cref="MarkerSpawnLocationComponent"/> with a matching <see cref="MarkerLocationSpawnRuleComponent.TargetString"/>.
/// </summary>
public sealed class MarkerLocationSpawnRule : StationEventSystem<MarkerLocationSpawnRuleComponent>
{
    protected override void Started(EntityUid uid, MarkerLocationSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        var validMarkers = new List<(EntityUid, TransformComponent)>();

        var query = EntityQueryEnumerator<MarkerSpawnLocationComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var markerSpawn, out var xform))
        {
            if (markerSpawn.MarkerStrings.Contains(comp.TargetString))
                validMarkers.Add((ent, xform));
        }

        if (validMarkers.Count == 0)
            return;

        if (!comp.TargetAllEligible)
        {
            var (marker, xform) = RobustRandom.Pick(validMarkers);
            foreach (var spawn in EntitySpawnCollection.GetSpawns(comp.SpawnEntries, RobustRandom))
            {
                Spawn(spawn, xform.Coordinates);
            }
        }
        else
        {
            foreach (var marker in validMarkers)
            {
                foreach (var spawn in EntitySpawnCollection.GetSpawns(comp.SpawnEntries, RobustRandom))
                {
                    Spawn(spawn, marker.Item2.Coordinates);
                }
            }
        }
    }
}
