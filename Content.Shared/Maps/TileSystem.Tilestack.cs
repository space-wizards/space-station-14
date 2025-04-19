using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.Maps;

/// <summary>
///     Handles tilestacks content-side.
/// </summary>
public sealed partial class TileSystem : EntitySystem
{
    /// <summary>
    ///     Creates a tilestack of the given tile based on BaseTurfs.
    /// </summary>
    public void CreateTilestack(TileRef tileRef)
    {
        if (!TryComp<TilestackMapGridComponent>(tileRef.GridUid, out var comp))
            return;
        var tilestack = new List<Tile>();
        var curTile = tileRef.GetContentTileDefinition().BaseTurf;
        while (!string.IsNullOrEmpty(curTile))
        {
            tilestack.Insert(0, new Tile(_tileDefinitionManager[curTile].TileId));
            curTile = ((ContentTileDefinition) _tileDefinitionManager[curTile]).BaseTurf;
        }
        comp.Data.Add(tileRef.GridIndices, tilestack);
    }

    /// <summary>
    ///     Same as AddLayer, but creates the tilestack if it doesn't exist.
    /// </summary>
    public void EnsureAddLayer(Vector2i gridIndices, EntityUid gridUid, MapGridComponent grid, Tile newTile)
    {
        if (!_maps.TryTilestack(gridIndices, gridUid, out _))
            CreateTilestack(_maps.GetTileRef(gridUid, grid, gridIndices));
        _maps.AddLayer(gridIndices, gridUid, grid, newTile);
    }

    /// <summary>
    ///     Same as RemoveLayer, but uses BaseTurf if there is no tilestack.
    /// </summary>
    public void EnsureRemoveLayer(Vector2i gridIndices, EntityUid gridUid, MapGridComponent grid)
    {
        if (!_maps.TryTilestack(gridIndices, gridUid, out _))
        {
            var tileDef = _maps.GetTileRef(gridUid, grid, gridIndices).GetContentTileDefinition();
            var plating = _tileDefinitionManager[tileDef.BaseTurf];
            _maps.SetTile(gridUid, grid, gridIndices, new Tile(plating.TileId));
            return;
        }
        _maps.RemoveLayer(gridIndices, gridUid, grid);
    }
}
