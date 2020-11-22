using System;
using System.Globalization;
using System.Linq;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Chemistry
{
    [Serializable]
    public struct ReagentUnit : ISelfSerialize, IComparable<ReagentUnit>, IEquatable<ReagentUnit>
    {
        private uint _value;
        private static readonly int Shift = 2;

        public static ReagentUnit MaxValue { get; } = new ReagentUnit(uint.MaxValue);
        public static ReagentUnit Epsilon { get; } = new ReagentUnit(1);
        public static ReagentUnit Zero { get; } = new ReagentUnit(0);

        private double ShiftDown()
        {
            return _value / Math.Pow(10, Shift);
        }

        private ReagentUnit(uint value)
        {
            _value = value;
        }

        public static ReagentUnit New(uint value)
        {
            return new ReagentUnit(value * (uint) Math.Pow(10, Shift));
        }

        public static ReagentUnit New(float value)
        {
            return new ReagentUnit(FromFloat(value));
        }

        private static uint FromFloat(float value)
        {
            float v = MathF.Round(value * MathF.Pow(10, Shift), MidpointRounding.AwayFromZero);
            if (v < 0)
            {
                return 0;
            }
            return (uint) v;
        }

        public static ReagentUnit New(double value)
        {
            return new ReagentUnit(FromDouble(value));
        }

        private static uint FromDouble(double value)
        {
            double v = Math.Round(value * Math.Pow(10, Shift), MidpointRounding.AwayFromZero);
            if (v < 0)
            {
                return 0;
            }
            return (uint) v;
        }

        public static ReagentUnit New(string value)
        {
            return New(FloatFromString(value));
        }

        private static float FloatFromString(string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        public static ReagentUnit operator +(ReagentUnit a) => a;

        public static ReagentUnit operator +(ReagentUnit a, ReagentUnit b)
            => new ReagentUnit(a._value + b._value);

        public static ReagentUnit operator -(ReagentUnit a, ReagentUnit b)
        {
            if (b._value > a._value)
            {
                // underflow
                return ReagentUnit.Zero;
            }
            return new ReagentUnit(a._value - b._value);
        }

        public static ReagentUnit operator *(ReagentUnit a, ReagentUnit b)
        {
            var aD = a.ShiftDown();
            var bD = b.ShiftDown();
            return New(aD * bD);
        }

        public static ReagentUnit operator *(ReagentUnit a, float b)
        {
            var aD = (float) a.ShiftDown();
            return New(aD * b);
        }

        public static ReagentUnit operator *(ReagentUnit a, double b)
        {
            var aD = a.ShiftDown();
            return New(aD * b);
        }

        public static ReagentUnit operator *(ReagentUnit a, uint b)
        {
            return new ReagentUnit(a._value * b);
        }

        public static ReagentUnit operator /(ReagentUnit a, ReagentUnit b)
        {
            if (b._value == 0)
            {
                throw new DivideByZeroException();
            }
            var aD = a.ShiftDown();
            var bD = b.ShiftDown();
            return New(aD / bD);
        }

        public static bool operator <=(ReagentUnit a, uint b)
        {
            return a.ShiftDown() <= b;
        }

        public static bool operator >=(ReagentUnit a, uint b)
        {
            return a.ShiftDown() >= b;
        }

        public static bool operator <(ReagentUnit a, uint b)
        {
            return a.ShiftDown() < b;
        }

        public static bool operator >(ReagentUnit a, uint b)
        {
            return a.ShiftDown() > b;
        }

        public static bool operator ==(ReagentUnit a, uint b)
        {
            return a.UInt() == b;
        }

        public static bool operator !=(ReagentUnit a, uint b)
        {
            return a.UInt() != b;
        }

        public static bool operator ==(ReagentUnit a, ReagentUnit b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ReagentUnit a, ReagentUnit b)
        {
            return !a.Equals(b);
        }

        public static bool operator <=(ReagentUnit a, ReagentUnit b)
        {
            return a._value <= b._value;
        }

        public static bool operator >=(ReagentUnit a, ReagentUnit b)
        {
            return a._value >= b._value;
        }

        public static bool operator <(ReagentUnit a, ReagentUnit b)
        {
            return a._value < b._value;
        }

        public static bool operator >(ReagentUnit a, ReagentUnit b)
        {
            return a._value > b._value;
        }

        public float Float()
        {
            return (float) ShiftDown();
        }

        public double Double()
        {
            return ShiftDown();
        }

        public uint UInt()
        {
            return (uint) ShiftDown();
        }

        public static ReagentUnit Min(params ReagentUnit[] reagentUnits)
        {
            return reagentUnits.Min();
        }

        public static ReagentUnit Min(ReagentUnit a, ReagentUnit b)
        {
            return a < b ? a : b;
        }

        public static ReagentUnit Max(ReagentUnit a, ReagentUnit b)
        {
            return a > b ? a : b;
        }

        public static ReagentUnit Clamp(ReagentUnit reagent, ReagentUnit min, ReagentUnit max)
        {
            if (min > max)
            {
                throw new ArgumentException($"{nameof(min)} {min} cannot be larger than {nameof(max)} {max}");
            }

            return reagent < min ? min : reagent > max ? max : reagent;
        }

        public override bool Equals(object obj)
        {
            return obj is ReagentUnit unit &&
                   _value == unit._value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_value);
        }

        public void Deserialize(string value)
        {
            _value = FromFloat(FloatFromString(value));
        }

        public override string ToString() => $"{ShiftDown().ToString(CultureInfo.InvariantCulture)}";

        public string Serialize()
        {
            return ToString();
        }

        public bool Equals(ReagentUnit other)
        {
            return _value == other._value;
        }

        public int CompareTo(ReagentUnit other)
        {
            if(other._value > _value)
            {
                return -1;
            }
            if(other._value < _value)
            {
                return 1;
            }
            return 0;
        }
    }
}
