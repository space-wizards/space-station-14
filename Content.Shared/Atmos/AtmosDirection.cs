using System;
using System.Runtime.CompilerServices;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos
{
    /// <summary>
    ///     The reason we use this over <see cref="Direction"/> is that we are going to do some heavy bitflag usage.
    /// </summary>
    [Flags, Serializable]
    [FlagsFor(typeof(AtmosDirectionFlags))]
    public enum AtmosDirection
    {
        Invalid = 0,                        // 0
        North   = 1 << 0,                   // 1
        South   = 1 << 1,                   // 2
        East    = 1 << 2,                   // 4
        West    = 1 << 3,                   // 8

        NorthEast = North | East,           // 5
        SouthEast = South | East,           // 6
        NorthWest = North | West,           // 9
        SouthWest = South | West,           // 10

        All = North | South | East | West,  // 15
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

        /// <summary>
        /// Converts a direction to an angle, where angle is -PI to +PI.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static Angle ToAngle(this AtmosDirection direction)
        {
            return direction switch
            {
                AtmosDirection.East => Angle.FromDegrees(0),
                AtmosDirection.North => Angle.FromDegrees(90),
                AtmosDirection.West => Angle.FromDegrees(180),
                AtmosDirection.South => Angle.FromDegrees(270),

                AtmosDirection.NorthEast => Angle.FromDegrees(45),
                AtmosDirection.NorthWest => Angle.FromDegrees(135),
                AtmosDirection.SouthWest => Angle.FromDegrees(225),
                AtmosDirection.SouthEast => Angle.FromDegrees(315),

                _ => throw new ArgumentOutOfRangeException(nameof(direction), $"It was {direction}."),
            };
        }

        /// <summary>
        /// Converts an angle to a cardinal AtmosDirection
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static AtmosDirection ToAtmosDirectionCardinal(this Angle angle)
        {
            return angle.GetCardinalDir().ToAtmosDirection();
        }

        /// <summary>
        /// Converts an angle to an AtmosDirection
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static AtmosDirection ToAtmosDirection(this Angle angle)
        {
            return angle.GetDir().ToAtmosDirection();
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

        public static AtmosDirection WithoutFlag(this AtmosDirection direction, AtmosDirection other)
        {
            return direction & ~other;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFlagSet(this AtmosDirection direction, AtmosDirection other)
        {
            return (direction & other) != 0;
        }
    }

    public sealed class AtmosDirectionFlags { }
}
