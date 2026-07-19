using System.Numerics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos;

/// <summary>
/// Bitflag for representing directions in atmospherics.
/// Used for indicating if airflow is allowed/blocked, etc.
/// Used over <see cref="Direction"/> as it allows us to represent multiple valid directions at once,
/// plus gives us easy comparisons using bitflags.
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
    // If more directions are added, note that AtmosDirectionHelpers.ToOppositeIndex() expects opposite directions
    // to come in pairs

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

    /// <summary>
    /// This returns the index that corresponds to the opposite direction of some other direction index.
    /// I.e., <c>1&lt;&lt;OppositeIndex(i) == (1&lt;&lt;i).GetOpposite()</c>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToOppositeIndex(this int index)
    {
        return index ^ 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AtmosDirection ToOppositeDir(this int index)
    {
        return (AtmosDirection) (1 << (index ^ 1));
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
            AtmosDirection.South => Angle.Zero,
            AtmosDirection.East => new Angle(MathHelper.PiOver2),
            AtmosDirection.North => new Angle(Math.PI),
            AtmosDirection.West => new Angle(-MathHelper.PiOver2),
            AtmosDirection.NorthEast => new Angle(Math.PI*3/4),
            AtmosDirection.NorthWest => new Angle(-Math.PI*3/4),
            AtmosDirection.SouthWest => new Angle(-MathHelper.PiOver4),
            AtmosDirection.SouthEast => new Angle(MathHelper.PiOver4),

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToIndex(this AtmosDirection direction)
    {
        // This will throw if you pass an invalid direction. Not this method's fault, but yours!
        return BitOperations.Log2((uint)direction);
    }

    /// <summary>
    /// Converts an index to an <see cref="AtmosDirection"/>.
    /// This is the same as doing <c>(AtmosDirection)(1 &lt;&lt; index)</c>, but reduces RSI from writing loops.
    /// </summary>
    /// <param name="index">The 0-based index of the direction.
    /// Should be 0 for North, 1 for South, 2 for East, and 3 for West.</param>
    /// <returns>The <see cref="AtmosDirection"/> corresponding to the given index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [PublicAPI]
    public static AtmosDirection ToAtmosDirection(this int index)
    {
        return (AtmosDirection)(1 << index);
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
        return (direction & other) == other;
    }

    /// <summary>
    /// Converts a cardinal direction to a <see cref="Vector2i"/>, where the value is the offset in that direction.
    /// </summary>
    /// <param name="dir">The direction to convert.
    /// Should be a cardinal direction, but will work with diagonals too.</param>
    /// <returns>A <see cref="Vector2i"/> where the value is the offset in that direction.
    /// E.g., North would be (0, -1), South would be (0, 1), etc.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [PublicAPI]
    public static Vector2i CardinalToIntVec(this AtmosDirection dir)
    {
        // extract individual bits and compute delta = (positive side) - (negative side).
        // a bit faster, works on my machine award
        var b = (byte)dir;
        var dx = ((b >> 2) & 1) - ((b >> 3) & 1);
        var dy = ((b >> 0) & 1) - ((b >> 1) & 1);

        return new Vector2i(dx, dy);
    }

    /// <summary>
    /// Offsets the given <see cref="Vector2i"/> in the direction of the given <see cref="AtmosDirection"/>.
    /// </summary>
    /// <param name="pos">The origin position.</param>
    /// <param name="dir">The direction to offset in.
    /// Should be a cardinal direction, but will work with diagonals too.</param>
    /// <returns>>The offset position. E.g., if the direction is North, this will return (pos.X, pos.Y + 1).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [PublicAPI]
    public static Vector2i Offset(this Vector2i pos, AtmosDirection dir)
    {
        return pos + dir.CardinalToIntVec();
    }
}

public sealed class AtmosDirectionFlags;
