using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonLayers;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="Shared.Procedural.DungeonLayers.FillGridDunGen"/>
    /// </summary>
    private async Task GenerateFillDunGen(FillGridDunGen fill, List<Dungeon> dungeons, HashSet<Vector2i> reservedTiles)
    {
        foreach (var dungeon in dungeons)
        {
            foreach (var tile in dungeon.AllTiles)
            {
                if (reservedTiles.Contains(tile))
                    continue;

                if (!_maps.TryGetTileDef(_grid, tile, out var tileDef))
                    continue;

                if (fill.AllowedTiles != null && !fill.AllowedTiles.Contains(tileDef.ID))
                    continue;

                if (!_anchorable.TileFree(_grid, tile, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                    continue;

                var gridPos = _maps.GridTileToLocal(_gridUid, _grid, tile);
                _entManager.SpawnEntity(fill.Entity, gridPos);

                await SuspendDungeon();
                if (!ValidateResume())
                    break;
            }
        }
    }
}
