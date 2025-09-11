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
    public struct FixedPoint4 : ISelfSerialize, IComparable<FixedPoint4>, IEquatable<FixedPoint4>, IFormattable
    {
        public long Value { get; private set; }
        private const long Shift = 4;
        private const long ShiftConstant = 10000; // Must be equal to pow(10, Shift)

        public static FixedPoint4 MaxValue { get; } = new(long.MaxValue);
        public static FixedPoint4 Epsilon { get; } = new(1);
        public static FixedPoint4 Zero { get; } = new(0);

        // This value isn't picked by any proper testing, don't @ me.
        private const float FloatEpsilon = 0.00001f;

#if DEBUG
        static FixedPoint4()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            DebugTools.Assert(Math.Pow(10, Shift) == ShiftConstant, "ShiftConstant must be equal to pow(10, Shift)");
        }
#endif

        private readonly double ShiftDown()
        {
            return Value / (double) ShiftConstant;
        }

        private FixedPoint4(long value)
        {
            Value = value;
        }

        public static FixedPoint4 New(long value)
        {
            return new(value * ShiftConstant);
        }
        public static FixedPoint4 FromTenThousandths(long value) => new(value);

        public static FixedPoint4 New(float value)
        {
            return new((long) ApplyFloatEpsilon(value * ShiftConstant));
        }

        private static float ApplyFloatEpsilon(float value)
        {
            return value + FloatEpsilon * Math.Sign(value);
        }

        private static double ApplyFloatEpsilon(double value)
        {
            return value + FloatEpsilon * Math.Sign(value);
        }

        /// <summary>
        /// Create the closest <see cref="FixedPoint4"/> for a float value, always rounding up.
        /// </summary>
        public static FixedPoint4 NewCeiling(float value)
        {
            return new((long) MathF.Ceiling(value * ShiftConstant));
        }

        public static FixedPoint4 New(double value)
        {
            return new((long) ApplyFloatEpsilon(value * ShiftConstant));
        }

        public static FixedPoint4 New(string value)
        {
            return New(Parse.Float(value));
        }

        public static FixedPoint4 operator +(FixedPoint4 a) => a;

        public static FixedPoint4 operator -(FixedPoint4 a) => new(-a.Value);

        public static FixedPoint4 operator +(FixedPoint4 a, FixedPoint4 b)
            => new(a.Value + b.Value);

        public static FixedPoint4 operator -(FixedPoint4 a, FixedPoint4 b)
            => new(a.Value - b.Value);

        public static FixedPoint4 operator *(FixedPoint4 a, FixedPoint4 b)
        {
            return new(b.Value * a.Value / ShiftConstant);
        }

        public static FixedPoint4 operator *(FixedPoint4 a, float b)
        {
            return new((long) ApplyFloatEpsilon(a.Value * b));
        }

        public static FixedPoint4 operator *(FixedPoint4 a, double b)
        {
            return new((long) ApplyFloatEpsilon(a.Value * b));
        }

        public static FixedPoint4 operator *(FixedPoint4 a, long b)
        {
            return new(a.Value * b);
        }

        public static FixedPoint4 operator /(FixedPoint4 a, FixedPoint4 b)
        {
            return new((long) (ShiftConstant * (long) a.Value / b.Value));
        }

        public static FixedPoint4 operator /(FixedPoint4 a, float b)
        {
            return new((long) ApplyFloatEpsilon(a.Value / b));
        }

        public static bool operator <=(FixedPoint4 a, long b)
        {
            return a <= New(b);
        }

        public static bool operator >=(FixedPoint4 a, long b)
        {
            return a >= New(b);
        }

        public static bool operator <(FixedPoint4 a, long b)
        {
            return a < New(b);
        }

        public static bool operator >(FixedPoint4 a, long b)
        {
            return a > New(b);
        }

        public static bool operator ==(FixedPoint4 a, long b)
        {
            return a == New(b);
        }

        public static bool operator !=(FixedPoint4 a, long b)
        {
            return a != New(b);
        }

        public static bool operator ==(FixedPoint4 a, FixedPoint4 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(FixedPoint4 a, FixedPoint4 b)
        {
            return !a.Equals(b);
        }

        public static bool operator <=(FixedPoint4 a, FixedPoint4 b)
        {
            return a.Value <= b.Value;
        }

        public static bool operator >=(FixedPoint4 a, FixedPoint4 b)
        {
            return a.Value >= b.Value;
        }

        public static bool operator <(FixedPoint4 a, FixedPoint4 b)
        {
            return a.Value < b.Value;
        }

        public static bool operator >(FixedPoint4 a, FixedPoint4 b)
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

        public readonly long Long()
        {
            return Value / ShiftConstant;
        }

        public readonly int Int()
        {
            return (int)Long();
        }

        // Implicit operators ftw
        public static implicit operator FixedPoint4(FixedPoint2 n) => New(n.Int());
        public static implicit operator FixedPoint4(float n) => New(n);
        public static implicit operator FixedPoint4(double n) => New(n);
        public static implicit operator FixedPoint4(int n) => New(n);
        public static implicit operator FixedPoint4(long n) => New(n);

        public static explicit operator FixedPoint2(FixedPoint4 n) => n.Int();
        public static explicit operator float(FixedPoint4 n) => n.Float();
        public static explicit operator double(FixedPoint4 n) => n.Double();
        public static explicit operator int(FixedPoint4 n) => n.Int();
        public static explicit operator long(FixedPoint4 n) => n.Long();

        public static FixedPoint4 Min(params FixedPoint4[] fixedPoints)
        {
            return fixedPoints.Min();
        }

        public static FixedPoint4 Min(FixedPoint4 a, FixedPoint4 b)
        {
            return a < b ? a : b;
        }

        public static FixedPoint4 Max(FixedPoint4 a, FixedPoint4 b)
        {
            return a > b ? a : b;
        }

        public static long Sign(FixedPoint4 value)
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

        public static FixedPoint4 Abs(FixedPoint4 a)
        {
            return FromTenThousandths(Math.Abs(a.Value));
        }

        public static FixedPoint4 Dist(FixedPoint4 a, FixedPoint4 b)
        {
            return FixedPoint4.Abs(a - b);
        }

        public static FixedPoint4 Clamp(FixedPoint4 number, FixedPoint4 min, FixedPoint4 max)
        {
            if (min > max)
            {
                throw new ArgumentException($"{nameof(min)} {min} cannot be larger than {nameof(max)} {max}");
            }

            return number < min ? min : number > max ? max : number;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is FixedPoint4 unit &&
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
                this = New(Parse.Double(value));
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

        public readonly bool Equals(FixedPoint4 other)
        {
            return Value == other.Value;
        }

        public readonly int CompareTo(FixedPoint4 other)
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

    public static class FixedPoint4EnumerableExt
    {
        public static FixedPoint4 Sum(this IEnumerable<FixedPoint4> source)
        {
            var acc = FixedPoint4.Zero;

            foreach (var n in source)
            {
                acc += n;
            }

            return acc;
        }
    }
}
