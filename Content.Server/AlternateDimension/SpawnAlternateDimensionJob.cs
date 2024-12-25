using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.AlternateDimension;
using Content.Shared.Tag;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.AlternateDimension;

public sealed class SpawnAlternateDimensionJob : Job<bool>
{
    private readonly IEntityManager _entManager;
    private readonly IMapManager _mapManager;
    private readonly IPrototypeManager _prototypeManager;
    private readonly SharedMapSystem _mapSystem;
    private readonly ITileDefinitionManager _tileDefManager;
    private readonly TileSystem _tileSystem;
    private readonly EntityLookupSystem _lookup;
    private readonly TagSystem _tag;

    private readonly MapId _alternateMapId;

    private readonly EntityUid _alternateGrid;
    private readonly EntityUid _originalGrid;
    private readonly AlternateDimensionParams _alternateParams;

    public SpawnAlternateDimensionJob(
        double maxTime,
        IEntityManager entManager,
        IMapManager mapManager,
        IPrototypeManager protoManager,
        SharedMapSystem map,
        ITileDefinitionManager tileDefManager,
        TileSystem tileSystem,
        EntityLookupSystem lookup,
        TagSystem tagSystem,
        MapId alternateMapId,
        EntityUid alternateGrid,
        EntityUid originalGrid,
        AlternateDimensionParams alternateParams,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _entManager = entManager;
        _mapManager = mapManager;
        _prototypeManager = protoManager;
        _mapSystem = map;
        _tileDefManager = tileDefManager;
        _tileSystem = tileSystem;
        _lookup = lookup;
        _tag = tagSystem;
        _originalGrid = originalGrid;
        _alternateGrid = alternateGrid;
        _alternateMapId = alternateMapId;
        _alternateParams = alternateParams;
    }

    protected override async Task<bool> Process()
    {
        if (!_entManager.TryGetComponent<MapGridComponent>(_originalGrid, out var stationGridComp))
            return false;
        if (!_entManager.TryGetComponent<MapGridComponent>(_alternateGrid, out var alternateGridComp))
            return false;

        if (!_prototypeManager.TryIndex(_alternateParams.Dimension, out var indexedDimension))
            return false;

        var random = new Random(_alternateParams.Seed);

        //Add map components
        if (indexedDimension.MapComponents is not null)
            _entManager.AddComponents(_mapSystem.GetMap(_alternateMapId), indexedDimension.MapComponents);

        //silhouette tiles
        var stationTiles = _mapSystem.GetAllTilesEnumerator(_originalGrid, stationGridComp);
        var alternateTiles = new List<(Vector2i Index, Tile Tile)>();
        var tileDef = _tileDefManager[indexedDimension.DefaultTile];
        while (stationTiles.MoveNext(out var tileRef))
        {
            alternateTiles.Add((tileRef.Value.GridIndices, new Tile(tileDef.TileId, variant: _tileSystem.PickVariant((ContentTileDefinition) tileDef, random))));
        }
        _mapSystem.SetTiles((_alternateGrid, alternateGridComp), alternateTiles);

        //Add grid components
        if (indexedDimension.GridComponents is not null)
            _entManager.AddComponents(_alternateGrid, indexedDimension.GridComponents);

        //Set alternate dimension entities
        HashSet<Entity<TagComponent, TransformComponent>> taggedEntities = new();
        _lookup.GetChildEntities(_originalGrid, taggedEntities);

        foreach (var tagged in taggedEntities)
        {
            foreach (var replacement in indexedDimension.Replacements)
            {
                if (!_tag.HasTag(tagged.Owner, replacement.Key))
                    continue;
                var coord = new EntityCoordinates(_mapSystem.GetMap(_alternateMapId), tagged.Comp2.Coordinates.Position);
                _entManager.SpawnEntity(replacement.Value, coord);
                break;
            }
        }

        //Final
        _mapManager.DoMapInitialize(_alternateMapId);
        _mapManager.SetMapPaused(_alternateMapId, false);

        return true;
    }
}
