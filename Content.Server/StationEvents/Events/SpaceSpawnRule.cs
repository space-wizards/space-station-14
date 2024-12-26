using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Station event component for spawning this rules antags in space around a station.
/// </summary>
public sealed class SpaceSpawnRule : StationEventSystem<SpaceSpawnRuleComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceSpawnRuleComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    protected override void Added(EntityUid uid, SpaceSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, comp, gameRule, args);

        if (!TryGetRandomStation(out var station))
        {
            ForceEndSelf(uid, gameRule);
            return;
        }

        var stationData = Comp<StationDataComponent>(station.Value);

        // find a station grid
        var gridUid = StationSystem.GetLargestGrid(stationData);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            Sawmill.Warning("Chosen station has no grids, cannot pick location for {ToPrettyString(uid):rule}");
            ForceEndSelf(uid, gameRule);
            return;
        }

        // figure out its AABB size and use that as a guide to how far the spawner should be
        var size = grid.LocalAABB.Size.Length() / 2;
        var distance = size + comp.SpawnDistance;
        var angle = RobustRandom.NextAngle();
        // position relative to station center
        var location = angle.ToVec() * distance;

        // create the spawner!
        var xform = Transform(gridUid.Value);
        var position = _transform.GetWorldPosition(xform) + location;
        comp.Coords = new MapCoordinates(position, xform.MapID);
        Sawmill.Info($"Picked location {comp.Coords} for {ToPrettyString(uid):rule}");
    }

    private void OnSelectLocation(Entity<SpaceSpawnRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords is {} coords)
            args.Coordinates.Add(coords);
    }
}
