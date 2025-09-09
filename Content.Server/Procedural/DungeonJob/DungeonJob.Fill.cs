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

                await SuspendDungeon();
                if (!ValidateResume())
                    return;

                if (!_maps.TryGetTileDef(_grid, tile, out var tileDef))
                    continue;

                if (fill.AllowedTiles != null && !fill.AllowedTiles.Contains(tileDef.ID))
                    continue;

                // If noise then check it matches.
                if (fill.ReservedNoise != null)
                {
                    var value = fill.ReservedNoise.GetNoise(tile.X, tile.Y);

                    if (fill.DistanceConfig != null)
                    {
                        // Need to get dx - dx in a range from -1 -> 1
                        var dx = 2 * tile.X / fill.Size.X;
                        var dy = 2 * tile.Y / fill.Size.Y;

                        var distance = GetDistance(dx, dy, fill.DistanceConfig);

                        value = MathHelper.Lerp(value, 1f - distance, fill.DistanceConfig.BlendWeight);
                    }

                    value *= (fill.Invert ? -1 : 1);

                    if (value < fill.Threshold)
                        continue;
                }

                if (!_anchorable.TileFree((_gridUid, _grid), tile, DungeonSystem.CollisionLayer, DungeonSystem.CollisionMask))
                    continue;

                var gridPos = _maps.GridTileToLocal(_gridUid, _grid, tile);
                _entManager.SpawnEntity(fill.Entity, gridPos);
            }
        }
    }
}
