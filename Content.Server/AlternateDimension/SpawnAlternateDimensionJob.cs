using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.AlternateDimension;
using Content.Shared.Tag;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.AlternateDimension;

public sealed class SpawnAlternateDimensionJob : Job<bool>
{
    private readonly IEntityManager _entManager;
    private readonly IMapManager _mapManager;
    private readonly IPrototypeManager _prototypeManager;
    private readonly MetaDataSystem _metaData;
    private readonly StationSystem _stationSystem;
    private readonly SharedMapSystem _map;
    private readonly ITileDefinitionManager _tileDefManager;
    private readonly TileSystem _tileSystem;
    private readonly EntityLookupSystem _lookup;
    private readonly TagSystem _tag;

    public readonly EntityUid Station;
    private readonly AlternateDimensionParams _alternateParams;

    private readonly ISawmill _sawmill;

    public SpawnAlternateDimensionJob(
        double maxTime,
        IEntityManager entManager,
        ILogManager logManager,
        IMapManager mapManager,
        IPrototypeManager protoManager,
        MetaDataSystem metaData,
        StationSystem stationSystem,
        SharedMapSystem map,
        ITileDefinitionManager tileDefManager,
        TileSystem tileSystem,
        EntityLookupSystem lookup,
        TagSystem tagSystem,
        EntityUid station,
        AlternateDimensionParams alternateParams,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _entManager = entManager;
        _mapManager = mapManager;
        _prototypeManager = protoManager;
        _metaData = metaData;
        _stationSystem = stationSystem;
        _map = map;
        _tileDefManager = tileDefManager;
        _tileSystem = tileSystem;
        _lookup = lookup;
        _tag = tagSystem;
        Station = station;
        _alternateParams = alternateParams;
        _sawmill = logManager.GetSawmill("shadow_dimension_job");
    }

    protected override async Task<bool> Process()
    {
        if (!_entManager.TryGetComponent<StationDataComponent>(Station, out var stationData))
            return false;

        var stationGrid = _stationSystem.GetLargestGrid(stationData);

        if (!_entManager.TryGetComponent<MapGridComponent>(stationGrid, out var stationGridComp))
            return false;

        if (!_prototypeManager.TryIndex(_alternateParams.Dimension, out var indexedDimension))
            return false;

        //Create new map and set name
        var shadowMapUid = _map.CreateMap(out var shadowMapId, runMapInit: false);
        var stationMetaData = _entManager.EnsureComponent<MetaDataComponent>(Station);
        _metaData.SetEntityName(
            shadowMapUid,
            $"Shadow side of {stationMetaData.EntityName}"); //TODO: Localize it

        _sawmill.Debug("shadow_dimension", $"Spawning station {stationMetaData.EntityName} shadow side with seed {_alternateParams.Seed}");
        var random = new Random(_alternateParams.Seed);

        //Add map components
        if (indexedDimension.MapComponents is not null)
            _entManager.AddComponents(shadowMapUid, indexedDimension.MapComponents);

        //Set Station grid silhouette tiles
        var shadowGrid = _mapManager.CreateGridEntity(shadowMapId);
        var stationTiles = _map.GetAllTilesEnumerator(stationGrid.Value, stationGridComp);
        var shadowTiles = new List<(Vector2i Index, Tile Tile)>();
        var tileDef = _tileDefManager[indexedDimension.DefaultTile];
        while (stationTiles.MoveNext(out var tileRef))
        {
            shadowTiles.Add((tileRef.Value.GridIndices, new Tile(tileDef.TileId, variant: _tileSystem.PickVariant((ContentTileDefinition) tileDef, random))));
        }
        _map.SetTiles(shadowGrid, shadowTiles);

        //Add grid components
        if (indexedDimension.GridComponents is not null)
            _entManager.AddComponents(shadowGrid, indexedDimension.GridComponents);

        //Set shadow dimension entities
        HashSet<Entity<TagComponent, TransformComponent>> taggedEntities = new();
        _lookup.GetChildEntities(stationGrid.Value, taggedEntities);

        foreach (var tagged in taggedEntities)
        {
            foreach (var replacement in indexedDimension.Replacements)
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
