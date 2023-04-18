using Content.Server.Ninja.Systems;
using Content.Server.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Content.Server.GameTicking.Rules.Components;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Event for spawning a Space Ninja mid-game.
/// </summary>
public sealed class NinjaRule : StationEventSystem<NinjaRuleComponent>
{
    [Dependency] private readonly NinjaSystem _ninja = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Started(EntityUid uid, NinjaRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);if (StationSystem.Stations.Count == 0)
        {
            Sawmill.Error("No stations exist, cannot spawn space ninja!");
            return;
        }

        var station = RobustRandom.Pick(StationSystem.Stations);
        if (!TryComp<StationDataComponent>(station, out var stationData))
        {
            Sawmill.Error("Chosen station isn't a station, cannot spawn space ninja!");
            return;
        }

        // find a station grid
        var gridUid = StationSystem.GetLargestGrid(stationData);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            Sawmill.Error("Chosen station has no grids, cannot spawn space ninja!");
            return;
        }

        // figure out its AABB size and use that as a guide to how far ninja should be
        var size = grid.LocalAABB.Size.Length / 2;
        var distance = size + component.SpawnDistance;
        var angle = RobustRandom.NextAngle();
        // position relative to station center
        var location = angle.ToVec() * distance;

        // create the spawner, the ninja will appear when a ghost has picked the role
        var xform = Transform(gridUid.Value);
        var position = _transform.GetWorldPosition(xform) + location;
        var coords = new MapCoordinates(position, xform.MapID);
        Sawmill.Info($"Creating ninja spawnpoint at {coords}");
        var spawner = Spawn("SpawnPointGhostSpaceNinja", coords);

        // tell the player where the station is when they pick the role
        _ninja.SetNinjaStationGrid(spawner, gridUid.Value);

        // start traitor rule incase it isn't, for the sweet greentext
        GameTicker.StartGameRule("Traitor");
    }
}
