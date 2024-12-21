using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Gravity;
using Content.Shared.Maps;
using Content.Shared.Parallax;
using Content.Shared.ShadowDimension;
using Content.Shared.Tag;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ShadowDimension;

public sealed class SpawnShadowDimensionJob : Job<bool>
{
    private readonly IEntityManager _entManager;
    private readonly IGameTiming _timing;
    private readonly IMapManager _mapManager;
    private readonly IPrototypeManager _prototypeManager;
    private readonly AnchorableSystem _anchorable;
    private readonly MetaDataSystem _metaData;
    private readonly SharedTransformSystem _xforms;
    private readonly StationSystem _stationSystem;
    private readonly SharedMapSystem _map;
    private readonly ITileDefinitionManager _tileDefManager;
    private readonly TileSystem _tileSystem;
    private readonly EntityLookupSystem _lookup;
    private readonly TagSystem _tag;

    public readonly EntityUid Station;
    private readonly ShadowDimensionParams _shadowParams;

    private readonly ISawmill _sawmill;

    public SpawnShadowDimensionJob(
        double maxTime,
        IEntityManager entManager,
        IGameTiming timing,
        ILogManager logManager,
        IMapManager mapManager,
        IPrototypeManager protoManager,
        AnchorableSystem anchorable,
        MetaDataSystem metaData,
        SharedTransformSystem xform,
        StationSystem stationSystem,
        SharedMapSystem map,
        ITileDefinitionManager tileDefManager,
        TileSystem tileSystem,
        EntityLookupSystem lookup,
        TagSystem tagSystem,
        EntityUid station,
        ShadowDimensionParams shadowParams,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _entManager = entManager;
        _timing = timing;
        _mapManager = mapManager;
        _prototypeManager = protoManager;
        _anchorable = anchorable;
        _metaData = metaData;
        _xforms = xform;
        _stationSystem = stationSystem;
        _map = map;
        _tileDefManager = tileDefManager;
        _tileSystem = tileSystem;
        _lookup = lookup;
        _tag = tagSystem;
        Station = station;
        _shadowParams = shadowParams;
        _sawmill = logManager.GetSawmill("shadow_dimension_job");
    }

    protected override async Task<bool> Process()
    {
        if (!_entManager.TryGetComponent<StationDataComponent>(Station, out var stationData))
            return false;

        var stationGrid = _stationSystem.GetLargestGrid(stationData);

        if (!_entManager.TryGetComponent<MapGridComponent>(stationGrid, out var stationGridComp))
            return false;

        //Create new map and set name
        var shadowMapUid = _map.CreateMap(out var shadowMapId, runMapInit: false);
        var stationMetaData = _entManager.EnsureComponent<MetaDataComponent>(Station);
        _metaData.SetEntityName(
            shadowMapUid,
            $"Shadow side of {stationMetaData.EntityName}"); //TODO: Localize it

        _sawmill.Debug("shadow_dimension", $"Spawning station {stationMetaData.EntityName} shadow side with seed {_shadowParams.Seed}");
        var random = new Random(_shadowParams.Seed);
        var shadowGrid = _mapManager.CreateGridEntity(shadowMapId);

        //Gravity
        _entManager.EnsureComponent<GravityComponent>(shadowGrid, out var gravityComp);
        gravityComp.Enabled = true;

        //Parallax
        _entManager.EnsureComponent<ParallaxComponent>(shadowMapUid, out var parallaxComp);
        parallaxComp.Parallax = "Darkness";

        //Set Station silhouette tiles
        var stationTiles = _map.GetAllTilesEnumerator(stationGrid.Value, stationGridComp);
        var shadowTiles = new List<(Vector2i Index, Tile Tile)>();
        var tileDef = _tileDefManager[_shadowParams.DefaultTile];
        while (stationTiles.MoveNext(out var tileRef))
        {
            shadowTiles.Add((tileRef.Value.GridIndices, new Tile(tileDef.TileId, variant: _tileSystem.PickVariant((ContentTileDefinition) tileDef, random))));
        }
        _map.SetTiles(shadowGrid, shadowTiles);

        //Set shadow dimension entities
        HashSet<Entity<TagComponent, TransformComponent>> taggedEntities = new();
        _lookup.GetChildEntities(stationGrid.Value, taggedEntities);

        foreach (var tagged in taggedEntities)
        {
            foreach (var replacement in _shadowParams.Replacements)
            {
                if (!_tag.HasTag(tagged.Owner, replacement.Key))
                    continue;
                var coord = new EntityCoordinates(shadowMapUid, tagged.Comp2.Coordinates.Position);
                _entManager.SpawnEntity(replacement.Value, coord);
                break;
            }
        }

        //Final
        _mapManager.DoMapInitialize(shadowMapId);
        _mapManager.SetMapPaused(shadowMapId, false);

        return true;
    }
}
