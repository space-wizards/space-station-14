using System.Numerics;
using Content.Server.Decals;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
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
    [Dependency] private readonly TurfSystem _turf = default!;

    public bool PryTile(Vector2i indices, EntityUid gridId)
    {
        var grid = _mapManager.GetGrid(gridId);
        var tileRef = grid.GetTileRef(indices);
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

        var tileDef = (ContentTileDefinition) _tileDefinitionManager[tile.TypeId];

        if (!tileDef.CanCrowbar && !(pryPlating && tileDef.CanAxe))
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

    public bool ReplaceTile(TileRef tileref, ContentTileDefinition replacementTile)
    {
        if (!TryComp<MapGridComponent>(tileref.GridUid, out var grid))
            return false;
        return ReplaceTile(tileref, replacementTile, tileref.GridUid, grid);
    }

    public bool ReplaceTile(TileRef tileref, ContentTileDefinition replacementTile, EntityUid grid, MapGridComponent? component = null)
    {
        if (!Resolve(grid, ref component))
            return false;

        var variant = _robustRandom.Pick(replacementTile.PlacementVariants);
        var decals = _decal.GetDecalsInRange(tileref.GridUid, _turf.GetTileCenter(tileref).Position, 0.5f);
        foreach (var (id, _) in decals)
        {
            _decal.RemoveDecal(tileref.GridUid, id);
        }
        component.SetTile(tileref.GridIndices, new Tile(replacementTile.TileId, 0, variant));
        return true;
    }

    private bool DeconstructTile(TileRef tileRef)
    {
        if (tileRef.Tile.IsEmpty)
            return false;

        var tileDef = (ContentTileDefinition) _tileDefinitionManager[tileRef.Tile.TypeId];

        if (string.IsNullOrEmpty(tileDef.BaseTurf))
            return false;

        var mapGrid = _mapManager.GetGrid(tileRef.GridUid);

        const float margin = 0.1f;
        var bounds = mapGrid.TileSize - margin * 2;
        var indices = tileRef.GridIndices;
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

        var plating = _tileDefinitionManager[tileDef.BaseTurf];

        mapGrid.SetTile(tileRef.GridIndices, new Tile(plating.TileId));

        return true;
    }
}
