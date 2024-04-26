using Content.Server.Antag;
using Content.Server.GameTicking.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Spawners.Components;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed class LoadMapRuleSystem : GameRuleSystem<LoadMapRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

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

        _map.CreateMap(out var mapId);
        comp.Map = mapId;

        if (comp.GameMap != null)
        {
            var gameMap = _prototypeManager.Index(comp.GameMap.Value);
            comp.MapGrids.AddRange(GameTicker.LoadGameMap(gameMap, comp.Map.Value, new MapLoadOptions()));
        }
        else if (comp.MapPath != null)
        {
            if (_mapLoader.TryLoad(comp.Map.Value, comp.MapPath.Value.ToString(), out var roots, new MapLoadOptions { LoadMap = true }))
                comp.MapGrids.AddRange(roots);
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
