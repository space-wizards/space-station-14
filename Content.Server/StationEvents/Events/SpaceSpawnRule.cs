using Content.Server.StationEvents.Components;
using Content.Shared.Antag;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

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
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceSpawnRuleComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    protected override void Added(Entity<SpaceSpawnRuleComponent, GameRuleComponent> ent, ref GameRuleAddedEvent args)
    {
        base.Added(ent, ref args);

        if (!Station.TryGetRandomStation<StationEventEligibleComponent>(out var station))
        {
            ForceEndSelf((ent.Owner, ent.Comp2));
            return;
        }

        // find a station grid
        var gridUid = Station.GetLargestGrid(station.Value.Owner);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            Sawmill.Warning("Chosen station has no grids, cannot pick location for {ToPrettyString(uid):rule}");
            ForceEndSelf((ent.Owner, ent.Comp2));
            return;
        }

        var spaceSpawn = ent.Comp1;

        // figure out its AABB size and use that as a guide to how far the spawner should be
        var size = grid.LocalAABB.Size.Length() / 2;
        var distance = size + spaceSpawn.SpawnDistance;
        var angle = RobustRandom.NextAngle();
        // position relative to station center
        var location = angle.ToVec() * distance;

        // create the spawner!
        var xform = Transform(gridUid.Value);
        var position = _transform.GetWorldPosition(xform) + location;
        spaceSpawn.Coords = new MapCoordinates(position, xform.MapID);
        Sawmill.Info($"Picked location {spaceSpawn.Coords} for {ToPrettyString(ent.Owner):rule}");
    }

    private void OnSelectLocation(Entity<SpaceSpawnRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords is { } coords)
            args.Coordinates.Add(coords);
    }
}
