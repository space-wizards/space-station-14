using System.Numerics;
using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

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
        var tileCount = 0;
        var tileCap = rand.NextGaussian(dungen.TileCap, dungen.CapStd);
        var visited = new HashSet<Vector2i>();

        while (iterations > 0 && tileCount < tileCap)
        {
            var roomTiles = new HashSet<Vector2i>();
            iterations--;

            // Get a random exterior tile to start floodfilling from.
            var edge = rand.Next(4);
            Vector2i seedTile;

            switch (edge)
            {
                case 0:
                    seedTile = new Vector2i(rand.Next(area.Left - 2, area.Right + 1), area.Bottom - 2);
                    break;
                case 1:
                    seedTile = new Vector2i(area.Right + 1, rand.Next(area.Bottom - 2, area.Top + 1));
                    break;
                case 2:
                    seedTile = new Vector2i(rand.Next(area.Left - 2, area.Right + 1), area.Top + 1);
                    break;
                case 3:
                    seedTile = new Vector2i(area.Left - 2, rand.Next(area.Bottom - 2, area.Top + 1));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            DebugTools.Assert(!visited.Contains(seedTile));
            var noiseFill = false;
            frontier.Clear();
            visited.Add(seedTile);
            frontier.Enqueue(seedTile);
            area = area.UnionTile(seedTile);
            var roomArea = new Box2i(seedTile, seedTile + Vector2i.One);

            // Time to floodfill again
            while (frontier.TryDequeue(out var node) && tileCount < tileCap)
            {
                var foundNoise = false;

                foreach (var layer in dungen.Layers)
                {
                    var value = layer.Noise.GetNoise(node.X, node.Y);

                    if (value < layer.Threshold)
                        continue;

                    roomArea = roomArea.UnionTile(node);
                    foundNoise = true;
                    noiseFill = true;
                    var tileDef = _tileDefManager[layer.Tile];
                    var variant = rand.NextByte(tileDef.Variants);

                    tiles.Add((node, new Tile(tileDef.TileId, variant: variant)));
                    roomTiles.Add(node);
                    tileCount++;
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
                        // Cardinals only
                        if (x != 0 && y != 0)
                            continue;

                        var neighbor = new Vector2i(node.X + x, node.Y + y);

                        if (!visited.Add(neighbor))
                            continue;

                        area = area.UnionTile(neighbor);
                        frontier.Enqueue(neighbor);
                    }
                }

                await SuspendIfOutOfTime();
                ValidateResume();
            }

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

        foreach (var tile in tiles)
        {
            dungeon.RoomTiles.Add(tile.Item1);
        }

        return dungeon;
    }
}
