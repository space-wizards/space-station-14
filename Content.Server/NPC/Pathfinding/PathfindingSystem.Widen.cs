using System.Numerics;
using Robust.Shared.Random;

namespace Content.Server.NPC.Pathfinding;

public sealed partial class PathfindingSystem
{
    /// <summary>
    /// Widens the path by the specified amount.
    /// </summary>
    public HashSet<Vector2i> GetWiden(WidenArgs args, Random random)
    {
        var tiles = new HashSet<Vector2i>(args.Path.Count * 2);
        var variance = (args.MaxWiden - args.MinWiden) / 2f + args.MinWiden;
        var counter = 0;

        foreach (var tile in args.Path)
        {
            counter++;

            if (counter != args.TileSkip)
                continue;

            counter = 0;

            var center = new Vector2(tile.X + 0.5f, tile.Y + 0.5f);

            if (args.Square)
            {
                for (var x = -variance; x <= variance; x++)
                {
                    for (var y = -variance; y <= variance; y++)
                    {
                        var neighbor = center + new Vector2(x, y);

                        tiles.Add(neighbor.Floored());
                    }
                }
            }
            else
            {
                for (var x = -variance; x <= variance; x++)
                {
                    for (var y = -variance; y <= variance; y++)
                    {
                        var offset = new Vector2(x, y);

                        if (offset.Length() > variance)
                            continue;

                        var neighbor = center + offset;

                        tiles.Add(neighbor.Floored());
                    }
                }
            }

            variance += random.NextFloat(-args.Variance * args.TileSkip, args.Variance * args.TileSkip);
            variance = Math.Clamp(variance, args.MinWiden, args.MaxWiden);
        }

        return tiles;
    }

    public record struct WidenArgs()
    {
        public bool Square = false;

        /// <summary>
        /// How many tiles to skip between iterations., 1-in-n
        /// </summary>
        public int TileSkip = 3;

        /// <summary>
        /// Maximum amount to vary per tile.
        /// </summary>
        public float Variance = 0.25f;

        /// <summary>
        /// Minimum width.
        /// </summary>
        public float MinWiden = 2f;


        public float MaxWiden = 7f;

        public List<Vector2i> Path;
    }
}
