using Content.Server.Coordinates.Helpers;
using Content.Server.Decals;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Maps;

/// <summary>
///     Handles server-side tile manipulation like prying/deconstructing tiles.
/// </summary>
public sealed class TileSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly DecalSystem _decal = default!;

    public bool PryTile(Vector2i indices, EntityUid gridId)
    {
        var grid = _mapManager.GetGrid(gridId);
        var tileRef = grid.GetTileRef(indices);
        return PryTile(tileRef);
    }

    public bool PryTile(TileRef tileRef)
    {
        var tile = tileRef.Tile;

        if (tile.IsEmpty)
            return false;

        var tileDef = (ContentTileDefinition) _tileDefinitionManager[tile.TypeId];

        if (!tileDef.CanCrowbar)
            return false;

        return DeconstructTile(tileRef);
    }

    public bool CutTile(TileRef tileRef)
    {
        var tile = tileRef.Tile;

        if (tile.IsEmpty)
            return false;

        var tileDef = (ContentTileDefinition) _tileDefinitionManager[tile.TypeId];

        if (!tileDef.CanWirecutter)
            return false;

        return DeconstructTile(tileRef);
    }

    private bool DeconstructTile(TileRef tileRef)
    {
        var indices = tileRef.GridIndices;

        var tileDef = (ContentTileDefinition) _tileDefinitionManager[tileRef.Tile.TypeId];
        var mapGrid = _mapManager.GetGrid(tileRef.GridUid);

        const float margin = 0.1f;
        var bounds = mapGrid.TileSize - margin * 2;
        var coordinates = mapGrid.GridTileToLocal(indices)
            .Offset(new Vector2(
                (_robustRandom.NextFloat() - 0.5f) * bounds,
                (_robustRandom.NextFloat() - 0.5f) * bounds));

        //Actually spawn the relevant tile item at the right position and give it some random offset.
        var tileItem = Spawn(tileDef.ItemDropPrototypeName, coordinates);
        Transform(tileItem).LocalRotation = _robustRandom.NextDouble() * Math.Tau;

        // Destroy any decals on the tile
        var decals = _decal.GetDecalsInRange(tileRef.GridUid, coordinates.SnapToGrid(EntityManager, _mapManager).Position, 0.5f);
        foreach (var (id, _) in decals)
        {
            _decal.RemoveDecal(tileRef.GridUid, id);
        }

        var plating = _tileDefinitionManager[tileDef.BaseTurfs[^1]];

        mapGrid.SetTile(tileRef.GridIndices, new Tile(plating.TileId));

        return true;
    }
}
