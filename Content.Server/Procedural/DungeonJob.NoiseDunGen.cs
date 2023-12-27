using System.Numerics;
using System.Threading.Tasks;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Procedural;

public sealed partial class DungeonJob
{
    private async Task<Dungeon> GenerateNoiseDungeon(NoiseDunGen dungen, EntityUid gridUid, MapGridComponent grid,
        int seed)
    {
        var rand = new Random(seed);
        var radSquared = dungen.Radius * dungen.Radius;
        var tiles = new List<(Vector2i, Tile)>();

        foreach (var layer in dungen.Layers)
        {
            layer.Noise.SetSeed(seed);
        }

        for (var x = -dungen.Radius; x <= dungen.Radius; x++)
        {
            for (var y = -dungen.Radius; y <= dungen.Radius; y++)
            {
                var point = new Vector2(x, y);

                if (point.LengthSquared() > radSquared)
                {
                    continue;
                }

                foreach (var layer in dungen.Layers)
                {
                    var value = layer.Noise.GetNoise(x, y);

                    if (value < layer.Threshold)
                        continue;

                    var tileDef = _tileDefManager[layer.Tile.Id];
                    var variant = rand.NextByte(tileDef.Variants);

                    tiles.Add((point.Floored(), new Tile(tileDef.TileId, variant: variant)));
                    break;
                }
            }

            // Just check per row.
            await SuspendIfOutOfTime();
            ValidateResume();
        }

        grid.SetTiles(tiles);

        var dungeon = new Dungeon()
        {
            Rooms =
            {
                new DungeonRoom()
            }
        };

        return dungeon;
    }
}
