using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Content.Shared.Procedural.PostGeneration;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /// <summary>
    /// <see cref="ReplaceTileDunGen"/>
    /// </summary>
    private async Task<Dungeon> GenerateTileReplacementDunGen(ReplaceTileDunGen gen, DungeonData data, HashSet<Vector2i> reservedTiles, Random random)
    {
        var tiles = _maps.GetAllTilesEnumerator(_gridUid, _grid);
        var replacements = new List<(Vector2i Index, Tile Tile)>();
        var reserved = new HashSet<Vector2i>();

        while (tiles.MoveNext(out var tileRef))
        {
            var node = tileRef.Value.GridIndices;

            if (reservedTiles.Contains(node))
                continue;

            foreach (var layer in gen.Layers)
            {
                var value = layer.Noise.GetNoise(node.X, node.Y);

                if (value < layer.Threshold)
                    continue;

                Tile tile;

                if (random.Prob(gen.VariantWeight))
                {
                    tile = _tileDefManager.GetVariantTile(_prototype.Index(layer.Tile), random);
                }
                else
                {
                    tile = new Tile(_prototype.Index(layer.Tile).TileId);
                }

                replacements.Add((node, tile));
                reserved.Add(node);
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
