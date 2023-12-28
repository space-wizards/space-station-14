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
        var tiles = new List<(Vector2i, Tile)>();

        foreach (var layer in dungen.Layers)
        {
            layer.Noise.SetSeed(seed);
        }

        // First we have to find a seed tile, then floodfill from there until we get to noise
        // at which point we floodfill the entire noise.
        var iterations = dungen.Iterations;
        var area = new Box2i();
        var frontier = new Queue<Vector2i>();
        var rooms = new List<DungeonRoom>();

        while (iterations > 0)
        {
            var roomTiles = new HashSet<Vector2i>();
            iterations--;

            // Get a random exterior tile to start floodfilling from.
            var edge = rand.Next(4);
            Vector2i seedTile;

            switch (edge)
            {
                case 0:
                    seedTile = new Vector2i(rand.Next(area.Left, area.Right), area.Bottom - 1);
                    break;
                case 1:
                    seedTile = new Vector2i(area.Right, rand.Next(area.Bottom - 1, area.Top));
                    break;
                case 2:
                    seedTile = new Vector2i(rand.Next(area.Left, area.Right), area.Top);
                    break;
                case 3:
                    seedTile = new Vector2i(area.Left - 1, rand.Next(area.Bottom - 1, area.Top));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var noiseFill = false;
            frontier.Clear();
            frontier.Enqueue(seedTile);
            Box2i roomArea = new Box2i(seedTile, seedTile + Vector2i.One);

            // Time to floodfill again
            while (frontier.TryDequeue(out var node))
            {
                var foundNoise = false;

                foreach (var layer in dungen.Layers)
                {
                    var value = layer.Noise.GetNoise(node.X, node.Y);

                    if (value < layer.Threshold)
                        continue;

                    roomArea = roomArea.Union(node);
                    foundNoise = true;
                    noiseFill = true;
                    var tileDef = _tileDefManager[layer.Tile.Id];
                    var variant = rand.NextByte(tileDef.Variants);

                    tiles.Add((node, new Tile(tileDef.TileId, variant: variant)));
                    roomTiles.Add(node);
                    break;
                }

                // Don't get neighbors if they don't have noise.
                // only if we've already found any noise.
                if (noiseFill && !foundNoise)
                    continue;

                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        var neighbor = new Vector2i(node.X + x, node.Y + y);

                        frontier.Enqueue(neighbor);
                    }
                }

                await SuspendIfOutOfTime();
                ValidateResume();
            }

            area = area.Union(roomArea);
            var center = Vector2.Zero;

            foreach (var tile in roomTiles)
            {
                center += tile + grid.TileSizeHalfVector;
            }

            center /= roomTiles.Count;
            rooms.Add(new DungeonRoom(roomTiles, center, roomArea, new HashSet<Vector2i>()));
            await SuspendIfOutOfTime();
            ValidateResume();
        }

        grid.SetTiles(tiles);

        var dungeon = new Dungeon(rooms);
        return dungeon;
    }
}
