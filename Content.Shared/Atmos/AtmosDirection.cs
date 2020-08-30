using System;
using Robust.Shared.Maths;

namespace Content.Shared.Atmos
{
    /// <summary>
    ///     The reason we use this over <see cref="Direction"/> is that we are going to do some heavy bitflag usage.
    /// </summary>
    [Flags]
    public enum AtmosDirection : byte
    {
        Invalid = 0,
        North   = 1 << 0,
        South   = 1 << 1,
        East    = 1 << 2,
        West    = 1 << 3,

        NorthEast = North | East,
        NorthWest = North | West,
        SouthEast = South | East,
        SouthWest = South | West,

        All = North | South | East | West,
    }

    public static class AtmosDirectionHelpers
    {
        public static AtmosDirection GetOpposite(this AtmosDirection direction)
        {
            return direction switch
            {
                AtmosDirection.North => AtmosDirection.South,
                AtmosDirection.South => AtmosDirection.North,
                AtmosDirection.East => AtmosDirection.West,
                AtmosDirection.West => AtmosDirection.East,
                AtmosDirection.NorthEast => AtmosDirection.SouthWest,
                AtmosDirection.NorthWest => AtmosDirection.SouthEast,
                AtmosDirection.SouthEast => AtmosDirection.NorthWest,
                AtmosDirection.SouthWest => AtmosDirection.NorthEast,
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
        }

        public static Direction ToDirection(this AtmosDirection direction)
        {
            return direction switch
            {
                AtmosDirection.North => Direction.North,
                AtmosDirection.South => Direction.South,
                AtmosDirection.East => Direction.East,
                AtmosDirection.West => Direction.West,
                AtmosDirection.NorthEast => Direction.NorthEast,
                AtmosDirection.NorthWest => Direction.NorthWest,
                AtmosDirection.SouthEast => Direction.SouthEast,
                AtmosDirection.SouthWest => Direction.SouthWest,
                AtmosDirection.Invalid => Direction.Invalid,
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
        }

        public static AtmosDirection ToAtmosDirection(this Direction direction)
        {
            return direction switch
            {
                Direction.North => AtmosDirection.North,
                Direction.South => AtmosDirection.South,
                Direction.East => AtmosDirection.East,
                Direction.West => AtmosDirection.West,
                Direction.NorthEast => AtmosDirection.NorthEast,
                Direction.NorthWest => AtmosDirection.NorthWest,
                Direction.SouthEast => AtmosDirection.SouthEast,
                Direction.SouthWest => AtmosDirection.SouthWest,
                Direction.Invalid => AtmosDirection.Invalid,
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
        }

        public static int ToIndex(this AtmosDirection direction)
        {
            // This will throw if you pass an invalid direction. Not this method's fault, but yours!
            return (int) Math.Log2((int) direction);
        }

        public static AtmosDirection WithFlag(this AtmosDirection direction, AtmosDirection other)
        {
            return direction | other;
        }
    }
}
