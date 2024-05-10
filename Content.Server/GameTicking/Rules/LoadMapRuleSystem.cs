using Content.Server.Antag;
using Content.Server.GameTicking.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GridPreloader;
using Content.Server.Spawners.Components;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed class LoadMapRuleSystem : GameRuleSystem<LoadMapRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly GridPreloaderSystem _gridPreloader = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LoadMapRuleComponent, AntagSelectLocationEvent>(OnSelectLocation);
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);
    }

    private void OnGridSplit(ref GridSplitEvent args)
    {
        var rule = QueryActiveRules();
        while (rule.MoveNext(out _, out var mapComp, out _))
        {
            if (!mapComp.MapGrids.Contains(args.Grid))
                continue;

            mapComp.MapGrids.AddRange(args.NewGrids);
            break;
        }
    }

    protected override void Added(EntityUid uid, LoadMapRuleComponent comp, GameRuleComponent rule, GameRuleAddedEvent args)
    {
        if (comp.Map != null)
            return;

        // grid preloading needs map to init after moving it
        var mapUid = comp.PreloadedGrid != null ? _map.CreateMap(out var mapId, false) : _map.CreateMap(out mapId);
        _metaData.SetEntityName(mapUid, $"LoadMapRule destination for rule {ToPrettyString(uid)}");
        comp.Map = mapId;

        if (comp.GameMap != null)
        {
            var gameMap = _prototypeManager.Index(comp.GameMap.Value);
            comp.MapGrids.AddRange(GameTicker.LoadGameMap(gameMap, comp.Map.Value, new MapLoadOptions()));
        }
        else if (comp.MapPath != null)
        {
            if (!_mapLoader.TryLoad(comp.Map.Value,
                    comp.MapPath.Value.ToString(),
                    out var roots,
                    new MapLoadOptions { LoadMap = true }))
            {
                _mapManager.DeleteMap(mapId);
                return;
            }

            comp.MapGrids.AddRange(roots);
        }
        else if (comp.PreloadedGrid != null)
        {
            // TODO: If there are no preloaded grids left, any rule announcements will still go off!
            if (!_gridPreloader.TryGetPreloadedGrid(comp.PreloadedGrid.Value, out var loadedShuttle))
            {
                _mapManager.DeleteMap(mapId);
                return;
            }

            _transform.SetParent(loadedShuttle.Value, mapUid);
            comp.MapGrids.Add(loadedShuttle.Value);
            _map.InitializeMap(mapId);
        }
        else
        {
            Log.Error($"No valid map prototype or map path associated with the rule {ToPrettyString(uid)}");
        }
    }

    private void OnSelectLocation(Entity<LoadMapRuleComponent> ent, ref AntagSelectLocationEvent args)
    {
        var query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID != ent.Comp.Map)
                continue;

            if (xform.GridUid == null || !ent.Comp.MapGrids.Contains(xform.GridUid.Value))
                continue;

            if (ent.Comp.SpawnerWhitelist != null && !ent.Comp.SpawnerWhitelist.IsValid(uid, EntityManager))
                continue;

            args.Coordinates.Add(_transform.GetMapCoordinates(xform));
        }
    }
}
