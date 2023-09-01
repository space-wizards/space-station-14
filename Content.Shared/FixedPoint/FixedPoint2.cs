using System.Globalization;
using System.Linq;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.FixedPoint
{
    /// <summary>
    ///     Represents a quantity of something, to a precision of 0.01.
    ///     To enforce this level of precision, floats are shifted by 2 decimal points, rounded, and converted to an int.
    /// </summary>
    [Serializable, CopyByRef]
    public struct FixedPoint2 : ISelfSerialize, IComparable<FixedPoint2>, IEquatable<FixedPoint2>, IFormattable
    {
        public int Value { get; private set; }
        private const int Shift = 2;
        private const int ShiftConstant = 100; // Must be equal to pow(10, Shift)

        public static FixedPoint2 MaxValue { get; } = new(int.MaxValue);
        public static FixedPoint2 Epsilon { get; } = new(1);
        public static FixedPoint2 Zero { get; } = new(0);

#if DEBUG
        static FixedPoint2()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            DebugTools.Assert(Math.Pow(10, Shift) == ShiftConstant, "ShiftConstant must be equal to pow(10, Shift)");
        }
#endif

        private readonly double ShiftDown()
        {
            return Value / (double) ShiftConstant;
        }

        private FixedPoint2(int value)
        {
            Value = value;
        }

        public static FixedPoint2 New(int value)
        {
            return new(value * ShiftConstant);
        }

        public static FixedPoint2 FromCents(int value) => new(value);

        public static FixedPoint2 New(float value)
        {
            return new((int) MathF.Round(value * ShiftConstant, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// Create the closest <see cref="FixedPoint2"/> for a float value, always rounding up.
        /// </summary>
        public static FixedPoint2 NewCeiling(float value)
        {
            return new((int) MathF.Ceiling(value * ShiftConstant));
        }

        public static FixedPoint2 New(double value)
        {
            return new((int) Math.Round(value * ShiftConstant, MidpointRounding.AwayFromZero));
        }

        public static FixedPoint2 New(string value)
        {
            return New(FloatFromString(value));
        }

        private static float FloatFromString(string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        public static FixedPoint2 operator +(FixedPoint2 a) => a;

        public static FixedPoint2 operator -(FixedPoint2 a) => new(-a.Value);

        public static FixedPoint2 operator +(FixedPoint2 a, FixedPoint2 b)
            => new(a.Value + b.Value);

        public static FixedPoint2 operator -(FixedPoint2 a, FixedPoint2 b)
            => new(a.Value - b.Value);

        public static FixedPoint2 operator *(FixedPoint2 a, FixedPoint2 b)
        {
            return new((int) MathF.Round(b.Value * a.Value / (float) ShiftConstant, MidpointRounding.AwayFromZero));
        }

        public static FixedPoint2 operator *(FixedPoint2 a, float b)
        {
            return new((int) MathF.Round(a.Value * b, MidpointRounding.AwayFromZero));
        }

        public static FixedPoint2 operator *(FixedPoint2 a, double b)
        {
            return new((int) Math.Round(a.Value * b, MidpointRounding.AwayFromZero));
        }

        public static FixedPoint2 operator *(FixedPoint2 a, int b)
        {
            return new(a.Value * b);
        }

        public static FixedPoint2 operator /(FixedPoint2 a, FixedPoint2 b)
        {
            return new((int) MathF.Round((ShiftConstant * a.Value) / (float) b.Value, MidpointRounding.AwayFromZero));
        }

        public static FixedPoint2 operator /(FixedPoint2 a, float b)
        {
            return new((int) MathF.Round(a.Value / b, MidpointRounding.AwayFromZero));
        }

        public static bool operator <=(FixedPoint2 a, int b)
        {
            return a <= New(b);
        }

        public static bool operator >=(FixedPoint2 a, int b)
        {
            return a >= New(b);
        }

        public static bool operator <(FixedPoint2 a, int b)
        {
            return a < New(b);
        }

        public static bool operator >(FixedPoint2 a, int b)
        {
            return a > New(b);
        }

        public static bool operator ==(FixedPoint2 a, int b)
        {
            return a == New(b);
        }

        public static bool operator !=(FixedPoint2 a, int b)
        {
            return a != New(b);
        }

        public static bool operator ==(FixedPoint2 a, FixedPoint2 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(FixedPoint2 a, FixedPoint2 b)
        {
            return !a.Equals(b);
        }

        public static bool operator <=(FixedPoint2 a, FixedPoint2 b)
        {
            return a.Value <= b.Value;
        }

        public static bool operator >=(FixedPoint2 a, FixedPoint2 b)
        {
            return a.Value >= b.Value;
        }

        public static bool operator <(FixedPoint2 a, FixedPoint2 b)
        {
            return a.Value < b.Value;
        }

        public static bool operator >(FixedPoint2 a, FixedPoint2 b)
        {
            return a.Value > b.Value;
        }

        public readonly float Float()
        {
            return (float) ShiftDown();
        }

        public readonly double Double()
        {
            return ShiftDown();
        }

        public readonly int Int()
        {
            return (int) ShiftDown();
        }

        // Implicit operators ftw
        public static implicit operator FixedPoint2(float n) => FixedPoint2.New(n);
        public static implicit operator FixedPoint2(double n) => FixedPoint2.New(n);
        public static implicit operator FixedPoint2(int n) => FixedPoint2.New(n);

        public static explicit operator float(FixedPoint2 n) => n.Float();
        public static explicit operator double(FixedPoint2 n) => n.Double();
        public static explicit operator int(FixedPoint2 n) => n.Int();

        public static FixedPoint2 Min(params FixedPoint2[] fixedPoints)
        {
            return fixedPoints.Min();
        }

        public static FixedPoint2 Min(FixedPoint2 a, FixedPoint2 b)
        {
            return a < b ? a : b;
        }

        public static FixedPoint2 Max(FixedPoint2 a, FixedPoint2 b)
        {
            return a > b ? a : b;
        }

        public static int Sign(FixedPoint2 value)
        {
            if (value < Zero)
            {
                return -1;
            }

            if (value > Zero)
            {
                return 1;
            }

            return 0;
        }

        public static FixedPoint2 Abs(FixedPoint2 a)
        {
            return FixedPoint2.New(Math.Abs(a.Value));
        }

        public static FixedPoint2 Dist(FixedPoint2 a, FixedPoint2 b)
        {
            return FixedPoint2.Abs(a - b);
        }

        public static FixedPoint2 Clamp(FixedPoint2 reagent, FixedPoint2 min, FixedPoint2 max)
        {
            if (min > max)
            {
                throw new ArgumentException($"{nameof(min)} {min} cannot be larger than {nameof(max)} {max}");
            }

            return reagent < min ? min : reagent > max ? max : reagent;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is FixedPoint2 unit &&
                   Value == unit.Value;
        }

        public override readonly int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return HashCode.Combine(Value);
        }

        public void Deserialize(string value)
        {
            // TODO implement "lossless" serializer.
            // I.e., dont use floats.
            if (value == "MaxValue")
                Value = int.MaxValue;
            else
                this = New(FloatFromString(value));
        }

        public override readonly string ToString() => $"{ShiftDown().ToString(CultureInfo.InvariantCulture)}";

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString();
        }

        public readonly string Serialize()
        {
            // TODO implement "lossless" serializer.
            // I.e., dont use floats.
            if (Value == int.MaxValue)
                return "MaxValue";

            return ToString();
        }

        public readonly bool Equals(FixedPoint2 other)
        {
            return Value == other.Value;
        }

        public readonly int CompareTo(FixedPoint2 other)
        {
            if (other.Value > Value)
            {
                return -1;
            }
            if (other.Value < Value)
            {
                return 1;
            }
            return 0;
        }

    }

    public static class FixedPointEnumerableExt
    {
        public static FixedPoint2 Sum(this IEnumerable<FixedPoint2> source)
        {
            var acc = FixedPoint2.Zero;

            foreach (var n in source)
            {
                acc += n;
            }

            return acc;
        }
    }
}
