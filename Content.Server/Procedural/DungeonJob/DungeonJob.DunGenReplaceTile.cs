using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Map;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="BiomeMarkerLayerPostGen"/>
    /// </summary>
    private async Task<Dungeon> GenerateTileReplacementDungeon(ReplaceTileDunGen gen, DungeonData data, HashSet<Vector2i> reservedTiles, Random random)
    {
        var tiles = _maps.GetAllTilesEnumerator(_gridUid, _grid);
        var replacements = new List<(Vector2i Index, Tile Tile)>();
        var reserved = new HashSet<Vector2i>();

        while (tiles.MoveNext(out var tileRef))
        {
            var tile = tileRef.Value.GridIndices;

            if (reservedTiles.Contains(tile))
                continue;

            foreach (var layer in gen.Layers)
            {
                var value = layer.Noise.GetNoise(tile.X, tile.Y);

                if (value < layer.Threshold)
                    continue;

                replacements.Add((tile, _tileDefManager.GetVariantTile(_prototype.Index(layer.Tile), random)));
                reserved.Add(tile);
                break;
            }

            await SuspendDungeon();
        }

        _maps.SetTiles(_gridUid, _grid, replacements);
        return new Dungeon(new List<DungeonRoom>()
        {
            new DungeonRoom(reserved, _position, Box2i.Empty, new HashSet<Vector2i>()),
        });
    }
}
