using Content.Server.Antag;
using Content.Server.Atmos.EntitySystems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Maps;
using Content.Shared.Random.Helpers;
using Content.Shared.SprayPainter.Components;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Station event component for spawning antags at random externals access.
/// </summary>
public sealed class OutsideStationAntagSpawnRule : StationEventSystem<OutsideStationAntagSpawnComponent>
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OutsideStationAntagSpawnComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    protected override void Added(EntityUid uid, OutsideStationAntagSpawnComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
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
        var externalList = new List<EntityUid>();
        var validTiles = new List<MapCoordinates>();
        var externalsQuery = AllEntityQuery<PaintableAirlockComponent>();
        while (externalsQuery.MoveNext(out var extUid, out var extComp))
        {
            if (extComp.Group.Id == "ExternalGlass" || extComp.Group.Id == "External")
                externalList.Add(extUid);
        }
        foreach (var airlock in externalList)
        {
            foreach (var tile in _mapSystem.GetTilesIntersecting(airlock, grid, new Circle(_transform.GetWorldPosition(airlock), 1), false))
            {
                if (!tile.IsSpace(_tileDef))
                    validTiles.Add(_transform.ToMapCoordinates(Transform(tile.GridUid).Coordinates));
            }
        }
        var spawn = _rand.Pick(validTiles);
        comp.Coords = spawn;
        Sawmill.Info($"Picked location {comp.Coords} for {ToPrettyString(uid):rule}");
    }

    private void OnSelectLocation(Entity<OutsideStationAntagSpawnComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords is {} coords)
            args.Coordinates.Add(coords);
    }
}
