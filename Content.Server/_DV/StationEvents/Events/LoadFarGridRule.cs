using Content.Server._DV.StationEvents.Components;
using Content.Shared.GameTicking;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._DV.StationEvents.Events;

public sealed partial class LoadFarGridRule : StationEventSystem<LoadFarGridRuleComponent>
{
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private TransformSystem _transform = default!;

    protected override void Added(EntityUid uid, LoadFarGridRuleComponent comp, GameRuleComponent rule, GameRuleAddedEvent args)
    {
        base.Added(uid, comp, rule, args);

        if (!Station.TryGetRandomStation(out var station) || !TryComp<StationDataComponent>(station, out var data))
        {
            Log.Error($"{ToPrettyString(uid):rule} failed to find a station!");
            ForceEndSelf(uid, rule);
            return;
        }

        if (data.Grids.Count < 1)
        {
            Sawmill.Warning($"{ToPrettyString(uid):rule} picked station {station} which had no grids!"); // Harmony change- warn instead of error
            ForceEndSelf(uid, rule);
            return;
        }

        // get an AABB that contains all the station's grids
        var aabb = new Box2();
        var map = MapId.Nullspace;
        foreach (var gridId in data.Grids)
        {
            // use the first grid's map id
            if (map == MapId.Nullspace)
                map = Transform(gridId).MapID;

            var grid = Comp<MapGridComponent>(gridId);
            var gridAabb = _transform.GetWorldMatrix(gridId).TransformBox(grid.LocalAABB);
            aabb = aabb.Union(gridAabb);
        }

        var scale = comp.Sousk / aabb.Width;
        var modifier = comp.DistanceModifier * scale;
        var dist = MathF.Max(aabb.Height / 2f, aabb.Width / 2f) * modifier;
        var offset = RobustRandom.NextVector2(dist, dist * 2.5f);
        var options = new MapLoadOptions
        {
            Offset = aabb.Center + offset,
        };

        var path = comp.Path;
        Log.Debug($"Loading far grid {path} at {options.Offset}");
        if (!_mapLoader.TryLoadGrid(map, path, out var grident, null, options.Offset))
        {
            Log.Error($"{ToPrettyString(uid):rule} failed to load grid {path}!");
            ForceEndSelf(uid, rule);
            return;
        }

        // let other systems do stuff
        var grids = new List<EntityUid> { grident.Value.Owner };
        var ev = new RuleLoadedGridsEvent(map, grids);
        RaiseLocalEvent(uid, ref ev);
    }
}
