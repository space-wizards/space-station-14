using System.Numerics;
using System.Threading.Tasks;
using Content.Shared.Maps;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Distance;
using Content.Shared.Procedural.DungeonGenerators;
using Robust.Shared.Map;

namespace Content.Server.Procedural.DungeonJob;

public sealed partial class DungeonJob
{
    /*
     * See https://www.redblobgames.com/maps/terrain-from-noise/#islands
     * Really it's just blending from the original noise (which may occupy the entire area)
     * with some other shape to confine it into a bounds more naturally.
     * https://old.reddit.com/r/proceduralgeneration/comments/kaen7h/new_video_on_procedural_island_noise_generation/gfjmgen/ also has more variations
     */

    /// <summary>
    /// <see cref="NoiseDistanceDunGen"/>
    /// </summary>
    private async Task<Dungeon> GenerateNoiseDistanceDunGen(
        Vector2i position,
        NoiseDistanceDunGen dungen,
        HashSet<Vector2i> reservedTiles,
        int seed,
        Random random)
    {
        var tiles = new List<(Vector2i, Tile)>();
        var matrix = Matrix3Helpers.CreateTranslation(position);

        foreach (var layer in dungen.Layers)
        {
            layer.Noise.SetSeed(seed);
        }

        // First we have to find a seed tile, then floodfill from there until we get to noise
        // at which point we floodfill the entire noise.
        var area = Box2i.FromDimensions(-dungen.Size / 2, dungen.Size);
        var roomTiles = new HashSet<Vector2i>();
        var width = (float) area.Width;
        var height = (float) area.Height;

        for (var x = area.Left; x <= area.Right; x++)
        {
            for (var y = area.Bottom; y <= area.Top; y++)
            {
                var node = new Vector2i(x, y);

                foreach (var layer in dungen.Layers)
                {
                    var value = layer.Noise.GetNoise(node.X, node.Y);

                    if (dungen.DistanceConfig != null)
                    {
                        // Need to get dx - dx in a range from -1 -> 1
                        var dx = 2 * x / width;
                        var dy = 2 * y / height;

                        var distance = GetDistance(dx, dy, dungen.DistanceConfig);

                        value = MathHelper.Lerp(value, 1f - distance, dungen.DistanceConfig.BlendWeight);
                    }

                    if (value < layer.Threshold)
                        continue;

                    var tileDef = _tileDefManager[layer.Tile];
                    var variant = _tile.PickVariant((ContentTileDefinition) tileDef, random);
                    var adjusted = Vector2.Transform(node + _grid.TileSizeHalfVector, matrix).Floored();

                    // Do this down here because noise has a much higher chance of failing than reserved tiles.
                    if (reservedTiles.Contains(adjusted))
                    {
                        break;
                    }

                    tiles.Add((adjusted, new Tile(tileDef.TileId, variant: variant)));
                    roomTiles.Add(adjusted);
                    break;
                }
            }

            await SuspendDungeon();
        }

        var room = new DungeonRoom(roomTiles, area.Center, area, new HashSet<Vector2i>());

        _maps.SetTiles(_gridUid, _grid, tiles);
        var dungeon = new Dungeon(new List<DungeonRoom>()
        {
            room,
        });

        await SuspendDungeon();
        return dungeon;
    }

    private float GetDistance(float dx, float dy, IDunGenDistance distance)
    {
        switch (distance)
        {
            case DunGenEuclideanSquaredDistance:
                return MathF.Min(1f, (dx * dx + dy * dy) / MathF.Sqrt(2));
            case DunGenSquareBump:
                return 1f - (1f - dx * dx) * (1f - dy * dy);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
