using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ninja.Systems;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Event for spawning a Space Ninja mid-game.
/// </summary>
public sealed class NinjaSpawnRule : StationEventSystem<NinjaSpawnRuleComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly NinjaSystem _ninja = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Started(EntityUid uid, NinjaSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        var stations = StationSystem.GetStations().ToList();
        if (stations.Count == 0)
            return;

        var station = _random.Pick(stations);
        var stationData = Comp<StationDataComponent>(station);

        // find a station grid
        var gridUid = StationSystem.GetLargestGrid(stationData);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            Sawmill.Error("Chosen station has no grids, cannot spawn space ninja!");
            return;
        }

        // figure out its AABB size and use that as a guide to how far ninja should be
        var size = grid.LocalAABB.Size.Length / 2;
        var distance = size + comp.SpawnDistance;
        var angle = _random.NextAngle();
        // position relative to station center
        var location = angle.ToVec() * distance;

        // create the spawner, the ninja will appear when a ghost has picked the role
        var xform = Transform(gridUid.Value);
        var position = _transform.GetWorldPosition(xform) + location;
        var coords = new MapCoordinates(position, xform.MapID);
        Sawmill.Info($"Creating ninja spawnpoint at {coords}");
        var spawner = Spawn("SpawnPointGhostSpaceNinja", coords);

        // tell the player where the station is when they pick the role
        _ninja.SetNinjaSpawnerData(spawner, gridUid.Value);
    }
}
