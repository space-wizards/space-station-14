using System.Diagnostics;
using System.Numerics;
using Content.Server.StationEvents.Components;
using Content.Shared.Antag;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Station event component for spawning entities in space around a station.
/// </summary>
/// <remarks>
/// Commonly used to spawn antags, like the space ninja.
/// </remarks>
/// <seealso cref="SpaceSpawnRuleComponent"/>
public sealed partial class SpaceSpawnRule : StationEventSystem<SpaceSpawnRuleComponent>
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<MapGridComponent> _gridQuery;

    /// <summary>
    /// Tries to find a position to spawn in space around a given station.
    /// </summary>
    /// <remarks>
    /// Rough flow:
    /// Select an eligible station.
    /// Find the bounding box of the largest grid on the selected station.
    /// Pick a random starting angle ANG.
    /// Create a circle some distance from the bounding box extremities.
    /// Check points on that circle evenly spaced from ANG in a random order for nearby grids.
    /// If there are any nearby, continue to the next point.
    /// Otherwise, return that point.
    /// </remarks>
    protected override void Added(Entity<SpaceSpawnRuleComponent, GameRuleComponent> ent, ref GameRuleAddedEvent args)
    {
        base.Added(ent, ref args);

        var spaceSpawn = ent.Comp1;

        if (!Station.TryGetRandomStation<StationEventEligibleComponent>(out var station))
        {
            Sawmill.Warning($"No eligible stations, cannot pick location for {ToPrettyString(ent):rule}");
            ForceEndSelf((ent.Owner, ent.Comp2));
            return;
        }

        // find a station grid
        var gridUid = Station.GetLargestGrid(station.Value.Owner);
        if (gridUid == null || !_gridQuery.TryComp(gridUid, out var grid))
        {
            Sawmill.Warning($"Chosen station has no grids, cannot pick location for {ToPrettyString(ent):rule}");
            ForceEndSelf((ent.Owner, ent.Comp2));
            return;
        }

        Debug.Assert(spaceSpawn.MaxAttempts > 0, $"Rule {ToPrettyString(ent):rule} has a non-positive MaxAttempts value");
        var maxAttempts = int.Max(1, spaceSpawn.MaxAttempts);

        // figure out its AABB size and use that as a guide to how far the spawner should be
        var size = grid.LocalAABB.Size.Length() / 2;
        var distance = size + spaceSpawn.SpawnDistance;

        // Pick a random angle, find our angles per index (0-MaxAttempts).
        var angleOffset = RobustRandom.NextAngle();
        var arcPerIndex = Math.Tau / maxAttempts;

        var gridCenter = _transform.ToMapCoordinates(new EntityCoordinates(gridUid.Value, grid.LocalAABB.Center));

        List<int> list = new(maxAttempts);
        List<Entity<MapGridComponent>> grids = [];
        for (var i = 0; i < maxAttempts; i++)
        {
            list.Add(i);
        }

        var gridClearOffset = new Vector2(spaceSpawn.ClearDistance, spaceSpawn.ClearDistance);

        while (list.Count > 0)
        {
            var index = RobustRandom.Next(list.Count);
            var arcIndex = list[index];
            list.RemoveSwap(index);
            var arcAngle = angleOffset + arcPerIndex * arcIndex;
            // position relative to station center
            var spawnOffset = arcAngle.ToVec() * distance;

            var spawnLocation = gridCenter.Offset(spawnOffset);

            // Check area immediately around point for grids.
            var spawnBox = new Box2(spawnLocation.Position - gridClearOffset, spawnLocation.Position + gridClearOffset);
            grids.Clear();
            _map.FindGridsIntersecting(spawnLocation.MapId, spawnBox, ref grids, approx: true, includeMap: false);

            if (grids.Count > 0)
                continue;

            // TODO: raycast towards the grid's center (colliding only with grids - AFAIK not currently possible),
            //       move towards the first collision, stopping SpawnDistance away.

            // create the spawner!
            spaceSpawn.Coords = spawnLocation;
            Sawmill.Info($"Picked location {spaceSpawn.Coords} for {ToPrettyString(ent.Owner):rule}");
            return;
        }

        // All points had grids nearby.  Spawn at the first point and pray.
        spaceSpawn.Coords = new MapCoordinates(gridCenter.Position + angleOffset.ToVec() * distance, gridCenter.MapId);
        Sawmill.Warning($"All {maxAttempts} positions collided, spawning at {spaceSpawn.Coords} for {ToPrettyString(ent.Owner):rule}");
    }

    [SubscribeLocalEvent]
    private void OnSelectLocation(Entity<SpaceSpawnRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords is { } coords)
            args.Coordinates.Add(coords);
    }
}
