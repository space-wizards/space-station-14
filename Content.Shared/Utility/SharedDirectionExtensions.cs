#nullable enable
using System.Collections.Generic;
using Content.Shared.Maps;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Shared.Utility
{
    public static class SharedDirectionExtensions
    {
        /// <summary>
        ///     Gets random directions until none are left
        /// </summary>
        /// <returns>An enumerable of the directions.</returns>
        public static IEnumerable<Direction> RandomDirections()
        {
            var directions = new[]
            {
                Direction.East,
                Direction.SouthEast,
                Direction.South,
                Direction.SouthWest,
                Direction.West,
                Direction.NorthWest,
                Direction.North,
                Direction.NorthEast,
            };

            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            var n = directions.Length;

            while (n > 1)
            {
                n--;
                var k = robustRandom.Next(n + 1);
                var value = directions[k];
                directions[k] = directions[n];
                directions[n] = value;
            }

            foreach (var direction in directions)
            {
                yield return direction;
            }
        }

        /// <summary>
        ///     Gets tiles in random directions from the given one.
        /// </summary>
        /// <returns>An enumerable of the adjacent tiles.</returns>
        public static IEnumerable<TileRef> AdjacentTilesRandom(this TileRef tile, bool ignoreSpace = false)
        {
            return tile.GridPosition().AdjacentTilesRandom(ignoreSpace);
        }

        /// <summary>
        ///     Gets tiles in random directions from the given one.
        /// </summary>
        /// <returns>An enumerable of the adjacent tiles.</returns>
        public static IEnumerable<TileRef> AdjacentTilesRandom(this EntityCoordinates coordinates, bool ignoreSpace = false)
        {
            foreach (var direction in RandomDirections())
            {
                var adjacent = coordinates.Offset(direction).GetTileRef();

                if (adjacent == null)
                {
                    continue;
                }

                if (ignoreSpace && adjacent.Value.Tile.IsEmpty)
                {
                    continue;
                }

                yield return adjacent.Value;
            }
        }

        public static EntityCoordinates Offset(this EntityCoordinates coordinates, Direction direction)
        {
            return coordinates.Offset(direction.ToVec());
        }
    }
}
