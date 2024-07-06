using System.Globalization;
using System.Linq;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.FixedPoint
{
    /// <summary>
    ///     Represents a quantity of something, to a precision of 0.0001, in an unsigned manner.
    ///     To enforce this level of precision, floats are shifted by 4 decimal points, rounded, and converted to an UNSIGNED int.
    /// </summary>
    [Serializable, CopyByRef]
    public struct FixedPoint4U : ISelfSerialize, IComparable<FixedPoint4U>, IEquatable<FixedPoint4U>, IFormattable
    {
        public uint Value { get; private set; }
        private const uint Shift = 4;
        private const uint ShiftConstant = 10000; // Must be equal to pow(10, Shift)

        public static FixedPoint4U MaxValue { get; } = new(uint.MaxValue);
        public static FixedPoint4U Epsilon { get; } = new(1);
        public static FixedPoint4U Zero { get; } = new(0);

        // This value isn't picked by any proper testing, don't @ me.
        private const float FloatEpsilon = 0.00001f;

#if DEBUG
        static FixedPoint4U()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            DebugTools.Assert(Math.Pow(10, Shift) == ShiftConstant, "ShiftConstant must be equal to pow(10, Shift)");
        }
#endif

        private readonly double ShiftDown()
        {
            return Value / (double) ShiftConstant;
        }

        private FixedPoint4U(uint value)
        {
            Value = value;
        }

        public static FixedPoint4U New(uint value)
        {
            return new(value * ShiftConstant);
        }

        public static FixedPoint4U FromTenThousandths(uint value) => new(value);

        public static FixedPoint4U New(float value)
        {
            return new((uint) ApplyFloatEpsilon(value * ShiftConstant));
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
        /// Create the closest <see cref="FixedPoint4U"/> for a float value, always rounding up.
        /// </summary>
        public static FixedPoint4U NewCeiling(float value)
        {
            return new((uint) MathF.Ceiling(value * ShiftConstant));
        }

        public static FixedPoint4U New(double value)
        {
            return new((uint) ApplyFloatEpsilon(value * ShiftConstant));
        }

        public static FixedPoint4U New(string value)
        {
            return New(Parse.Double(value));
        }

        public static FixedPoint4U operator +(FixedPoint4U a) => a;

        public static FixedPoint4U operator +(FixedPoint4U a, FixedPoint4U b)
            => new(a.Value + b.Value);

        public static FixedPoint4U operator -(FixedPoint4U a, FixedPoint4U b)
            => new(a.Value - b.Value);

        public static FixedPoint4U operator *(FixedPoint4U a, FixedPoint4U b)
        {
            return new(b.Value * a.Value / ShiftConstant);
        }

        public static FixedPoint4U operator *(FixedPoint4U a, float b)
        {
            return new((uint) ApplyFloatEpsilon(a.Value * b));
        }

        public static FixedPoint4U operator *(FixedPoint4U a, double b)
        {
            return new((uint) ApplyFloatEpsilon(a.Value * b));
        }

        public static FixedPoint4U operator *(FixedPoint4U a, uint b)
        {
            return new(a.Value * b);
        }

        public static FixedPoint4U operator /(FixedPoint4U a, FixedPoint4U b)
        {
            return new((uint) (ShiftConstant * (long) a.Value / b.Value));
        }

        public static FixedPoint4U operator /(FixedPoint4U a, float b)
        {
            return new((uint) ApplyFloatEpsilon(a.Value / b));
        }

        public static bool operator <=(FixedPoint4U a, uint b)
        {
            return a <= New(b);
        }

        public static bool operator >=(FixedPoint4U a, uint b)
        {
            return a >= New(b);
        }

        public static bool operator <(FixedPoint4U a, uint b)
        {
            return a < New(b);
        }

        public static bool operator >(FixedPoint4U a, uint b)
        {
            return a > New(b);
        }

        public static bool operator ==(FixedPoint4U a, uint b)
        {
            return a == New(b);
        }

        public static bool operator !=(FixedPoint4U a, uint b)
        {
            return a != New(b);
        }

        public static bool operator ==(FixedPoint4U a, FixedPoint4U b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(FixedPoint4U a, FixedPoint4U b)
        {
            return !a.Equals(b);
        }

        public static bool operator <=(FixedPoint4U a, FixedPoint4U b)
        {
            return a.Value <= b.Value;
        }

        public static bool operator >=(FixedPoint4U a, FixedPoint4U b)
        {
            return a.Value >= b.Value;
        }

        public static bool operator <(FixedPoint4U a, FixedPoint4U b)
        {
            return a.Value < b.Value;
        }

        public static bool operator >(FixedPoint4U a, FixedPoint4U b)
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

        public readonly uint Int()
        {
            return Value / ShiftConstant;
        }

        // Implicit operators ftw
        public static implicit operator FixedPoint4U(float n) => FixedPoint4U.New(n);
        public static implicit operator FixedPoint4U(double n) => FixedPoint4U.New(n);
        public static implicit operator FixedPoint4U(uint n) => FixedPoint4U.New(n);

        public static explicit operator float(FixedPoint4U n) => n.Float();
        public static explicit operator double(FixedPoint4U n) => n.Double();
        public static explicit operator uint(FixedPoint4U n) => n.Int();

        public static FixedPoint4U Min(params FixedPoint4U[] fixedPoints)
        {
            return fixedPoints.Min();
        }

        public static FixedPoint4U Min(FixedPoint4U a, FixedPoint4U b)
        {
            return a < b ? a : b;
        }

        public static FixedPoint4U Max(FixedPoint4U a, FixedPoint4U b)
        {
            return a > b ? a : b;
        }

        public static uint Sign(FixedPoint4U value)
        {
            if (value > Zero)
            {
                return 1;
            }

            return 0;
        }

        public static FixedPoint4U Abs(FixedPoint4U a)
        {
            return FromTenThousandths((uint) Math.Abs(a.Value));
        }

        public static FixedPoint4U Dist(FixedPoint4U a, FixedPoint4U b)
        {
            return FixedPoint4U.Abs(a - b);
        }

        public static FixedPoint4U Clamp(FixedPoint4U number, FixedPoint4U min, FixedPoint4U max)
        {
            if (min > max)
            {
                throw new ArgumentException($"{nameof(min)} {min} cannot be larger than {nameof(max)} {max}");
            }

            return number < min ? min : number > max ? max : number;
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is FixedPoint4U unit &&
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

        public readonly bool Equals(FixedPoint4U other)
        {
            return Value == other.Value;
        }

        public readonly int CompareTo(FixedPoint4U other)
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

    public static class FixedPoint4UEnumerableExt
    {
        public static FixedPoint4U Sum(this IEnumerable<FixedPoint4U> source)
        {
            var acc = FixedPoint4U.Zero;

            foreach (var n in source)
            {
                acc += n;
            }

            return acc;
        }
    }
}
