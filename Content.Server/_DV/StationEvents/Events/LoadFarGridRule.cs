using Content.Server.GameTicking.Rules;
using Content.Server.Station.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class LoadFarGridRule : StationEventSystem<LoadFarGridRuleComponent>
{
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    protected override void Added(EntityUid uid, LoadFarGridRuleComponent comp, GameRuleComponent rule, GameRuleAddedEvent args)
    {
        base.Added(uid, comp, rule, args);

        if (!TryGetRandomStation(out var station) || !TryComp<StationDataComponent>(station, out var data))
        {
            Log.Error($"{ToPrettyString(uid):rule} failed to find a station!");
            ForceEndSelf(uid, rule);
            return;
        }

        if (data.Grids.Count < 1)
        {
            Log.Error($"{ToPrettyString(uid):rule} picked station {station} which had no grids!");
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
            var gridAabb = Transform(gridId).WorldMatrix.TransformBox(grid.LocalAABB);
            aabb = aabb.Union(gridAabb);
        }

        var scale = comp.Sousk / aabb.Width;
        var modifier = comp.DistanceModifier * scale;
        var dist = MathF.Max(aabb.Height / 2f, aabb.Width / 2f) * modifier;
        var offset = RobustRandom.NextVector2(dist, dist * 2.5f);
        var options = new MapLoadOptions
        {
            Offset = aabb.Center + offset,
            LoadMap = false
        };

        var path = comp.Path.ToString();
        Log.Debug($"Loading far grid {path} at {options.Offset}");
        if (!_mapLoader.TryLoad(map, path, out var grids, options))
        {
            Log.Error($"{ToPrettyString(uid):rule} failed to load grid {path}!");
            ForceEndSelf(uid, rule);
            return;
        }

        // let other systems do stuff
        var ev = new RuleLoadedGridsEvent(map, grids);
        RaiseLocalEvent(uid, ref ev);
    }
}
