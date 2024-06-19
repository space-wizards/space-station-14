using Content.Server.GameTicking.Rules.Components;
using Content.Server.GridPreloader;
using Content.Shared.GameTicking.Components;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed class LoadMapRuleSystem : GameRuleSystem<LoadMapRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly GridPreloaderSystem _gridPreloader = default!;

    protected override void Added(EntityUid uid, LoadMapRuleComponent comp, GameRuleComponent rule, GameRuleAddedEvent args)
    {
        if (comp.PreloadedGrid != null && !_gridPreloader.PreloadingEnabled)
        {
            // Preloading will never work if it's disabled, duh
            Log.Debug($"Immediately ending {ToPrettyString(uid):rule} as preloading grids is disabled by cvar.");
            ForceEndSelf(uid, rule);
            return;
        }

        // grid preloading needs map to init after moving it
        var mapUid = _map.CreateMap(out var mapId, runMapInit: comp.PreloadedGrid == null);

        Log.Info($"Created map {mapId} for {ToPrettyString(uid):rule}");

        IReadOnlyList<EntityUid> grids;
        if (comp.GameMap != null)
        {
            var gameMap = _prototypeManager.Index(comp.GameMap.Value);
            grids = GameTicker.LoadGameMap(gameMap, mapId, new MapLoadOptions());
        }
        else if (comp.MapPath is {} path)
        {
            var options = new MapLoadOptions { LoadMap = true };
            if (!_mapLoader.TryLoad(mapId, path.ToString(), out var roots, options))
            {
                Log.Error($"Failed to load map from {path}!");
                Del(mapUid);
                ForceEndSelf(uid, rule);
                return;
            }

            grids = roots;
        }
        else if (comp.PreloadedGrid is {} preloaded)
        {
            // TODO: If there are no preloaded grids left, any rule announcements will still go off!
            if (!_gridPreloader.TryGetPreloadedGrid(preloaded, out var loadedShuttle))
            {
                Log.Error($"Failed to get a preloaded grid with {preloaded}!");
                Del(mapUid);
                ForceEndSelf(uid, rule);
                return;
            }

            _transform.SetParent(loadedShuttle.Value, mapUid);
            grids = new List<EntityUid>() { loadedShuttle.Value };
            _map.InitializeMap(mapUid);
        }
        else
        {
            Log.Error($"No valid map prototype or map path associated with the rule {ToPrettyString(uid)}");
            Del(mapUid);
            ForceEndSelf(uid, rule);
            return;
        }

        var ev = new RuleLoadedGridsEvent(mapId, grids);
        RaiseLocalEvent(uid, ref ev);
    }
}
