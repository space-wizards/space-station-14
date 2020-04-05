using System;
using System.Linq;

namespace Content.Shared.Chemistry
{
    [Serializable]
    public struct ReagentUnit
    {
        private int _value;
        private static readonly int Shift = 2;

        public static ReagentUnit MaxValue => new ReagentUnit(int.MaxValue);

        private decimal ShiftDown()
        {
            return _value / (decimal)Math.Pow(10, Shift);
        }

        private decimal ShiftUp()
        {
            return _value * (decimal)Math.Pow(10, Shift);
        }

        private ReagentUnit(int value)
        {
            _value = value;
        }

        public static ReagentUnit New(int value)
        {
            return new ReagentUnit(value * (int) Math.Pow(10, Shift));
        }

        public static ReagentUnit New(decimal value)
        {
            return new ReagentUnit((int) Math.Round(value * (decimal) Math.Pow(10, Shift)));
        }

        public static ReagentUnit New(float value)
        {
            return new ReagentUnit((int) Math.Round(value * (float) Math.Pow(10, Shift)));
        }

        public static ReagentUnit New(double value)
        {
            return new ReagentUnit((int) Math.Round(value * Math.Pow(10, Shift)));
        }

        public static ReagentUnit operator +(ReagentUnit a) => a;

        public static ReagentUnit operator -(ReagentUnit a) => new ReagentUnit(-a._value);

        public static ReagentUnit operator +(ReagentUnit a, ReagentUnit b)
            => new ReagentUnit(a._value + b._value);

        public static ReagentUnit operator -(ReagentUnit a, ReagentUnit b)
            => a + -b;

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

        public static ReagentUnit operator *(ReagentUnit a, decimal b)
        {
            var aD = a.ShiftDown();
            return New(aD * b);
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

        public static bool operator <=(ReagentUnit a, int b)
        {
            return a.ShiftDown() <= b;
        }

        public static bool operator >=(ReagentUnit a, int b)
        {
            return a.ShiftDown() >= b;
        }

        public static bool operator ==(ReagentUnit a, int b)
        {
            return a.ShiftDown() == b;
        }

        public static bool operator !=(ReagentUnit a, int b)
        {
            return a.ShiftDown() != b;
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

        public override string ToString() => $"{ShiftDown()}";

        public float Float()
        {
            return (float) ShiftDown();
        }

        public decimal Decimal()
        {
            return (decimal) ShiftDown();
        }

        public int Int()
        {
            return (int) ShiftDown();
        }

        public static ReagentUnit Min(params ReagentUnit[] reagentUnits)
        {
            return reagentUnits.OrderBy(x => x._value).First();
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
    }
}
