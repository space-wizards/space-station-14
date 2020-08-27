using System;
using Robust.Shared.Maths;

namespace Content.Server.Atmos
{
    /// <summary>
    ///     The reason we use this over <see cref="Direction"/> is that we are going to do some heavy bitflag usage.
    /// </summary>
    [Flags]
    public enum AtmosDirection
    {
        Invalid = 0,
        North   = 1 << 1,
        South   = 1 << 2,
        East    = 1 << 3,
        West    = 1 << 4,

        NorthEast = North | East,
        NorthWest = North | West,
        SouthEast = South | East,
        SouthWest = South | West,
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
                _ => AtmosDirection.Invalid
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
                _ => Direction.Invalid
            };
        }

        public static AtmosDirection WithFlag(this AtmosDirection direction, AtmosDirection other)
        {
            return direction | other;
        }
    }
}
