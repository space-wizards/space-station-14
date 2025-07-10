using System.Linq;
using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Decals;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Maps;

/// <summary>
///     Handles server-side tile manipulation like prying/deconstructing tiles.
/// </summary>
public sealed class TileSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly SharedDecalSystem _decal = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private int _tileStackingHistorySize;

    //Tile history for deconstruction.
    private readonly Dictionary<(EntityUid Grid, Vector2i Indices), Stack<string>> _tileBaseTurfHistory = new();

    public override void Initialize()
    {
        _cfg.OnValueChanged(CCVars.TileStackingHistorySize, value => _tileStackingHistorySize = value, true);
    }

    /// <summary>
    ///     Returns a weighted pick of a tile variant.
    /// </summary>
    public byte PickVariant(ContentTileDefinition tile)
    {
        return PickVariant(tile, _robustRandom.GetRandom());
    }

    /// <summary>
    ///     Returns a weighted pick of a tile variant.
    /// </summary>
    public byte PickVariant(ContentTileDefinition tile, int seed)
    {
        var rand = new System.Random(seed);
        return PickVariant(tile, rand);
    }

    /// <summary>
    ///     Returns a weighted pick of a tile variant.
    /// </summary>
    public byte PickVariant(ContentTileDefinition tile, System.Random random)
    {
        var variants = tile.PlacementVariants;

        var sum = variants.Sum();
        var accumulated = 0f;
        var rand = random.NextFloat() * sum;

        for (byte i = 0; i < variants.Length; ++i)
        {
            accumulated += variants[i];

            if (accumulated >= rand)
                return i;
        }

        // Shouldn't happen
        throw new InvalidOperationException($"Invalid weighted variantize tile pick for {tile.ID}!");
    }

    /// <summary>
    ///     Returns a tile with a weighted random variant.
    /// </summary>
    public Tile GetVariantTile(ContentTileDefinition tile, System.Random random)
    {
        return new Tile(tile.TileId, variant: PickVariant(tile, random));
    }

    /// <summary>
    ///     Returns a tile with a weighted random variant.
    /// </summary>
    public Tile GetVariantTile(ContentTileDefinition tile, int seed)
    {
        var rand = new System.Random(seed);
        return new Tile(tile.TileId, variant: PickVariant(tile, rand));
    }

    public bool PryTile(Vector2i indices, EntityUid gridId)
    {
        var grid = Comp<MapGridComponent>(gridId);
        var tileRef = _maps.GetTileRef(gridId, grid, indices);
        return PryTile(tileRef);
    }

    public bool PryTile(TileRef tileRef)
    {
        return PryTile(tileRef, false);
    }

    public bool PryTile(TileRef tileRef, bool pryPlating)
    {
        var tile = tileRef.Tile;

        if (tile.IsEmpty)
            return false;

        var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.TypeId];

        if (!tileDef.CanCrowbar)
            return false;

        return DeconstructTile(tileRef);
    }

    public bool ReplaceTile(TileRef tileref, ContentTileDefinition replacementTile)
    {
        if (!TryComp<MapGridComponent>(tileref.GridUid, out var grid))
            return false;
        return ReplaceTile(tileref, replacementTile, tileref.GridUid, grid);
    }

    public bool ReplaceTile(TileRef tileref, ContentTileDefinition replacementTile, EntityUid grid, MapGridComponent? component = null)
    {
        DebugTools.Assert(tileref.GridUid == grid);

        if (!Resolve(grid, ref component))
            return false;

        var key = (grid, tileref.GridIndices);

        //Create stack if there is none
        if (!_tileBaseTurfHistory.TryGetValue(key, out var stack))
        {
            stack = new Stack<string>();
            _tileBaseTurfHistory[key] = stack;
        }

        //Push current tile to the stack, if not empty
        if (!tileref.Tile.IsEmpty)
        {
            var currentTileDef = (ContentTileDefinition)_tileDefinitionManager[tileref.Tile.TypeId];
            stack.Push(currentTileDef.ID);

            //Enforce stack limit (0 = infinite)
            if (_tileStackingHistorySize > 0 && stack.Count > _tileStackingHistorySize)
            {
                //Trim the bottom-most (oldest) entry
                var newStack = new Stack<string>(stack.Reverse().Take(_tileStackingHistorySize));
                _tileBaseTurfHistory[key] = newStack;
            }
        }

        var variant = PickVariant(replacementTile);
        var decals = _decal.GetDecalsInRange(tileref.GridUid, _turf.GetTileCenter(tileref).Position, 0.5f);
        foreach (var (id, _) in decals)
        {
            _decal.RemoveDecal(tileref.GridUid, id);
        }

        _maps.SetTile(grid, component, tileref.GridIndices, new Tile(replacementTile.TileId, 0, variant));
        return true;
    }


    public bool DeconstructTile(TileRef tileRef)
    {
        if (tileRef.Tile.IsEmpty)
            return false;

        var tileDef = (ContentTileDefinition)_tileDefinitionManager[tileRef.Tile.TypeId];

        //Can't deconstruct anything that doesn't have a base turf.
        if (tileDef.BaseTurf is not { Count: > 0 })
            return false;

        var gridUid = tileRef.GridUid;
        var mapGrid = Comp<MapGridComponent>(gridUid);

        const float margin = 0.1f;
        var bounds = mapGrid.TileSize - margin * 2;
        var indices = tileRef.GridIndices;
        var coordinates = _maps.GridTileToLocal(gridUid, mapGrid, indices)
            .Offset(new Vector2(
                (_robustRandom.NextFloat() - 0.5f) * bounds,
                (_robustRandom.NextFloat() - 0.5f) * bounds));

        var keyHistory = (gridUid, indices);
        string previousTileId;

        //Pop from stack if we have history
        if (_tileBaseTurfHistory.TryGetValue(keyHistory, out var stack) && stack.Count > 0)
        {
            previousTileId = stack.Pop();

            //Clean up empty stacks to avoid memory buildup
            if (stack.Count == 0)
            {
                _tileBaseTurfHistory.Remove(keyHistory);
            }
        }
        else
        {
            //No stack? Assume BaseTurf[0] was the layer below
            previousTileId = tileDef.BaseTurf[0];
        }

        //Actually spawn the relevant tile item at the right position and give it some random offset.  
        var tileItem = Spawn(tileDef.ItemDropPrototypeName, coordinates);
        Transform(tileItem).LocalRotation = _robustRandom.NextDouble() * Math.Tau;

        //Destroy any decals on the tile  
        var decals = _decal.GetDecalsInRange(gridUid, coordinates.SnapToGrid(EntityManager, _mapManager).Position, 0.5f);
        foreach (var (id, _) in decals)
        {
            _decal.RemoveDecal(tileRef.GridUid, id);
        }

        //Replace tile with the one it was placed on
        var previousDef = (ContentTileDefinition)_tileDefinitionManager[previousTileId];
        _maps.SetTile(gridUid, mapGrid, indices, new Tile(previousDef.TileId));

        return true;
    }
}
