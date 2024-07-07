using System.Globalization;
using System.Numerics;

namespace Content.Shared.FixedPoint;

public interface IFixedPoint<TSelf, TWrapped, TPrecision> : IMinMaxValue<TSelf>, INumber<TSelf>
    where TSelf : struct, IFixedPoint<TSelf, TWrapped, TPrecision>, IAdditionOperators<TSelf, TSelf,TSelf>
    where TWrapped : INumber<TWrapped>, IBinaryInteger<TWrapped>, IMinMaxValue<TWrapped>
    where TPrecision: IFloatingPoint<TPrecision>

{
    public TWrapped Value { get; protected init; }
    public abstract static int Shift { get; }
    public virtual static int ShiftConstant => (int) Math.Pow(10, TSelf.Shift);

    // This value isn't picked by any proper testing, don't @ me.
    public const float FloatEpsilon = 0.00001f;

    public static TSelf Epsilon => 1;
    static TSelf IMinMaxValue<TSelf>.MinValue => 0;
    static TSelf IMinMaxValue<TSelf>.MaxValue => (TSelf)(TWrapped.MaxValue / (TWrapped)(object)TSelf.ShiftConstant);
    static TSelf INumberBase<TSelf>.Zero => 0;
    static TSelf INumberBase<TSelf>.One => 1;
    static int INumberBase<TSelf>.Radix => TWrapped.Radix;
    static TSelf IAdditiveIdentity<TSelf, TSelf>.AdditiveIdentity => 0;
    static TSelf IMultiplicativeIdentity<TSelf, TSelf>.MultiplicativeIdentity => 1;
    public static TSelf Create(TWrapped n)
    {
        var imp = new TSelf
        {
            Value = n * (TWrapped)(object)TSelf.ShiftConstant
        };
        return imp;
    }

    public static TWrapped ToWrappedType(TSelf fixedPoint)
    {
        return fixedPoint.Value / (TWrapped)(object)TSelf.ShiftConstant;
    }

    public static TSelf Create(TPrecision value)
    {
        var imp = new TSelf
        {
            Value = TWrapped.CreateChecked(TPrecision.Round(ShiftDown(value), TSelf.Shift, MidpointRounding.ToNegativeInfinity))
        };
        return imp;
    }

    public static TPrecision ToFloatType(TSelf fixedPoint)
    {
        return ShiftDown(TPrecision.CreateChecked(fixedPoint.Value));
    }

    public static TPrecision ShiftUp(TPrecision value) => value * TPrecision.CreateChecked(TSelf.ShiftConstant);

    public static TPrecision ShiftDown(TPrecision value) => value / TPrecision.CreateChecked(TSelf.ShiftConstant);

    // Implicit operators
    public virtual static explicit operator TSelf(TWrapped n) => Create(n);
    public virtual static explicit operator TWrapped(TSelf fp) => ToWrappedType(fp);

    public virtual static implicit operator TSelf(float n) => Create(TPrecision.CreateChecked(n));
    public virtual static implicit operator TSelf(double n) => Create(TPrecision.CreateChecked(n));
    public virtual static implicit operator TSelf(decimal n) => Create(TPrecision.CreateChecked(n));
    public virtual static implicit operator TSelf(int n) => Create(TWrapped.CreateChecked(n));

    public virtual static explicit operator float(TSelf fp) => float.CreateChecked(ToFloatType(fp));
    public virtual static explicit operator double(TSelf fp) => double.CreateChecked(ToFloatType(fp));
    public virtual static explicit operator decimal(TSelf fp) => decimal.CreateChecked(ToFloatType(fp));
    public virtual static explicit operator int(TSelf fp) => int.CreateChecked(ToWrappedType(fp));

    #region Math Operators

    static TSelf IAdditionOperators<TSelf, TSelf, TSelf>.operator +(TSelf left, TSelf right)
    {
        return Create(left.Value + right.Value);
    }

    static TSelf ISubtractionOperators<TSelf, TSelf, TSelf>.operator -(TSelf left, TSelf right)
    {
        return Create(left.Value - right.Value);
    }

    static TSelf IIncrementOperators<TSelf>.operator ++(TSelf value)
    {
        return value + TSelf.One;
    }

    static TSelf IDecrementOperators<TSelf>.operator --(TSelf value)
    {
        return value - TSelf.One;
    }

    static TSelf IMultiplyOperators<TSelf, TSelf, TSelf>.operator *(TSelf left, TSelf right)
    {
        return left * right;
    }

    static TSelf IDivisionOperators<TSelf, TSelf, TSelf>.operator /(TSelf left, TSelf right)
    {
        return Create(TPrecision.Round(
            TPrecision.CreateChecked(left.Value) / TPrecision.CreateChecked(right.Value),
            MidpointRounding.ToNegativeInfinity));
    }

    static TSelf IModulusOperators<TSelf, TSelf, TSelf>.operator %(TSelf left, TSelf right)
    {
        return Create(left.Value % right.Value);
    }

    static TSelf IUnaryPlusOperators<TSelf, TSelf>.operator +(TSelf value)
    {
        return +value;
    }

    static TSelf IUnaryNegationOperators<TSelf, TSelf>.operator -(TSelf value)
    {
        return -value;
    }

    #endregion

    #region Conversions

    static bool INumberBase<TSelf>.TryConvertFromChecked<TOther>(TOther value, out TSelf result)
    {
        if (TPrecision.TryConvertFromChecked(value, out var pData))
        {
            result = Create(pData);
            return true;
        }

        if (TWrapped.TryConvertFromChecked(value, out var wData))
        {
            result = Create(wData);
            return true;
        }

        if (typeof(TOther) == typeof(TSelf))
        {
            result = (TSelf)(object)value;
            return true;
        }

        result = default;
        return false;
    }

    static bool INumberBase<TSelf>.TryConvertFromSaturating<TOther>(TOther value, out TSelf result)
    {
        if (TPrecision.TryConvertFromSaturating(value, out var pData))
        {
            result = Create(pData);
            return true;
        }

        if (TWrapped.TryConvertFromSaturating(value, out var wData))
        {
            result = Create(wData);
            return true;
        }

        if (typeof(TOther) == typeof(TSelf))
        {
            result = (TSelf)(object)value;
            return true;
        }

        result = default;
        return false;
    }

    static bool INumberBase<TSelf>.TryConvertFromTruncating<TOther>(TOther value, out TSelf result)
    {
        if (TPrecision.TryConvertFromTruncating(value, out var pData))
        {
            result = Create(pData);
            return true;
        }

        if (TWrapped.TryConvertFromTruncating(value, out var wData))
        {
            result = Create(wData);
            return true;
        }

        if (typeof(TOther) == typeof(TSelf))
        {
            result = (TSelf)(object)value;
            return true;
        }

        result = default;
        return false;
    }

    static bool INumberBase<TSelf>.TryConvertToChecked<TOther>(TSelf value, out TOther result)
    {
        if (TPrecision.TryConvertToChecked(ToFloatType(value), out TOther? pData) && pData != null)
        {
            result = pData;
            return true;
        }
        if (TWrapped.TryConvertToChecked(ToWrappedType(value), out TOther? wData) && wData != null)
        {
            result = wData;
            return true;
        }
        if (typeof(TOther) == typeof(TSelf))
        {
            result = (TOther)(object)value;
            return true;
        }
        //I have to do this, it's being dumb
        result = default!;
        return false;
    }

    static bool INumberBase<TSelf>.TryConvertToSaturating<TOther>(TSelf value, out TOther result)
    {
        if (TPrecision.TryConvertToSaturating(ToFloatType(value), out TOther? pData) && pData != null)
        {
            result = pData;
            return true;
        }
        if (TWrapped.TryConvertToSaturating(ToWrappedType(value), out TOther? wData) && wData != null)
        {
            result = wData;
            return true;
        }
        if (typeof(TOther) == typeof(TSelf))
        {
            result = (TOther)(object)value;
            return true;
        }
        //I have to do this, it's being dumb
        result = default!;
        return false;
    }

    static bool INumberBase<TSelf>.TryConvertToTruncating<TOther>(TSelf value, out TOther result)
    {
        if (TPrecision.TryConvertToTruncating(ToFloatType(value), out TOther? pData) && pData != null)
        {
            result = pData;
            return true;
        }
        if (TWrapped.TryConvertToTruncating(ToWrappedType(value), out TOther? wData) && wData != null)
        {
            result = wData;
            return true;
        }
        if (typeof(TOther) == typeof(TSelf))
        {
            result = (TOther)(object)value;
            return true;
        }
        //I have to do this, it's being dumb
        result = default!;
        return false;
    }

    #endregion

    #region Equality/Comparison

    static bool IEqualityOperators<TSelf,TSelf, bool>.operator ==(TSelf left, TSelf right) => left.Value == right.Value;

    static bool IEqualityOperators<TSelf,TSelf, bool>.operator !=(TSelf left, TSelf right) => left.Value != right.Value;

    static bool IComparisonOperators<TSelf, TSelf, bool>.operator >(TSelf left, TSelf right) => left.Value > right.Value;

    static bool IComparisonOperators<TSelf, TSelf, bool>.operator >=(TSelf left, TSelf right) => left.Value >= right.Value;

    static bool IComparisonOperators<TSelf, TSelf, bool>.operator <(TSelf left, TSelf right) => left.Value < right.Value;

    static bool IComparisonOperators<TSelf, TSelf, bool>.operator <=(TSelf left, TSelf right) => left.Value <= right.Value;

    int IComparable.CompareTo(object? obj)
    {
        if (obj == null)
        {
            return 1;
        }
        if (obj is TSelf fp)
        {
            if (fp.Value > Value)
            {
                return -1;
            }
            if (fp.Value < Value)
            {
                return 1;
            }
            return 0;
        }
        throw new ArgumentException($"Other type must be {typeof(TSelf)}");
    }

    int IComparable<TSelf>.CompareTo(TSelf fp)
    {
        if (fp.Value > Value)
        {
            return -1;
        }
        if (fp.Value < Value)
        {
            return 1;
        }
        return 0;
    }

    bool IEquatable<TSelf>.Equals(TSelf fp) => Value == fp.Value;

    #endregion

    #region Integer Checks

    static bool INumberBase<TSelf>.IsInteger(TSelf fixedPoint) =>
        decimal.CreateChecked(fixedPoint.Value) % 1 < (decimal) FloatEpsilon;

    static bool INumberBase<TSelf>.IsEvenInteger(TSelf value) => TSelf.IsInteger(value) && (TSelf.Abs(value % 2) == 0);

    static bool INumberBase<TSelf>.IsOddInteger(TSelf value) => TSelf.IsInteger(value) && (TSelf.Abs(value % 2) == 1);

    #endregion

    #region Strings/Formating/Parsing

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => $"{ToFloatType((TSelf) this).ToString()}";

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return ShiftDown(TPrecision.CreateChecked(Value)).TryFormat(destination, out charsWritten, format, provider);
    }

    static TSelf INumberBase<TSelf>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) =>
        Create(TPrecision.Parse(s, style, provider));

    static TSelf INumberBase<TSelf>.Parse(string s, NumberStyles style, IFormatProvider? provider) =>
        Create(TPrecision.Parse(s, style, provider));

    static TSelf IParsable<TSelf>.Parse(string s, IFormatProvider? provider)
    {
        return Create(TPrecision.Parse(s, provider));
    }

    static TSelf ISpanParsable<TSelf>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return Create(TPrecision.Parse(s, provider));
    }

    static bool INumberBase<TSelf>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out TSelf result)
    {
        result = default;
        if (!TPrecision.TryParse(s, style, provider, out var floatVal))
            return false;
        result = Create(floatVal);
        return true;
    }

    static bool INumberBase<TSelf>.TryParse(string? s, NumberStyles style, IFormatProvider? provider, out TSelf result)
    {
        result = default;
        if (!TPrecision.TryParse(s, style, provider, out var floatVal))
            return false;
        result = Create(floatVal);
        return true;
    }

    static bool IParsable<TSelf>.TryParse(string? s, IFormatProvider? provider, out TSelf result)
    {
        result = default;
        if (!TPrecision.TryParse(s, provider, out var floatVal))
            return false;
        result = Create(floatVal);
        return true;
    }

    static bool ISpanParsable<TSelf>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TSelf result)
    {
        result = default;
        if (!TPrecision.TryParse(s, provider, out var floatVal))
            return false;
        result = Create(floatVal);
        return true;
    }

    #endregion

    #region Passthrough To WrappedType

    static TSelf INumberBase<TSelf>.Abs(TSelf fixedPoint) => (TSelf)TWrapped.Abs(fixedPoint.Value);

    static bool INumberBase<TSelf>.IsNaN(TSelf fixedPoint) => TWrapped.IsNaN(fixedPoint.Value);

    static bool INumberBase<TSelf>.IsInfinity(TSelf fixedPoint) => TWrapped.IsInfinity(fixedPoint.Value);

    static bool INumberBase<TSelf>.IsNegative(TSelf fixedPoint) => TWrapped.IsNegative(fixedPoint.Value);

    static bool INumberBase<TSelf>.IsNegativeInfinity(TSelf fixedPoint) => TWrapped.IsNegativeInfinity(fixedPoint.Value);

    static bool INumberBase<TSelf>.IsNormal(TSelf fixedPoint) => TWrapped.IsNormal(fixedPoint.Value);

    static bool INumberBase<TSelf>.IsCanonical(TSelf fixedPoint) => TWrapped.IsCanonical(fixedPoint.Value);

    static bool INumberBase<TSelf>.IsComplexNumber(TSelf fixedPoint) => TWrapped.IsComplexNumber(fixedPoint.Value);

    static bool INumberBase<TSelf>.IsFinite(TSelf fixedPoint) => TWrapped.IsFinite(fixedPoint.Value);

    static bool INumberBase<TSelf>.IsImaginaryNumber(TSelf fixedPoint) => TWrapped.IsImaginaryNumber(fixedPoint.Value);

    static bool INumberBase<TSelf>.IsRealNumber(TSelf fixedPoint) => TWrapped.IsRealNumber(fixedPoint.Value);

    static bool INumberBase<TSelf>.IsSubnormal(TSelf fixedPoint) => TWrapped.IsSubnormal(fixedPoint.Value);

    static bool INumberBase<TSelf>.IsPositive(TSelf fixedPoint) => TWrapped.IsPositive(fixedPoint.Value);

    static bool INumberBase<TSelf>.IsPositiveInfinity(TSelf fixedPoint) => TWrapped.IsPositiveInfinity(fixedPoint.Value);

    static bool INumberBase<TSelf>.IsZero(TSelf fixedPoint) => TWrapped.IsZero(fixedPoint.Value);

    static TSelf INumberBase<TSelf>.MaxMagnitude(TSelf fixedPointX, TSelf fixedPointY) =>
        (TSelf)TWrapped.MaxMagnitude(fixedPointX.Value, fixedPointY.Value);

    static TSelf INumberBase<TSelf>.MaxMagnitudeNumber(TSelf fixedPointX, TSelf fixedPointY) =>
        (TSelf)TWrapped.MaxMagnitudeNumber(fixedPointX.Value, fixedPointY.Value);

    static TSelf INumberBase<TSelf>.MinMagnitude(TSelf fixedPointX, TSelf fixedPointY) =>
        (TSelf)TWrapped.MinMagnitude(fixedPointX.Value, fixedPointY.Value);

    static TSelf INumberBase<TSelf>.MinMagnitudeNumber(TSelf fixedPointX, TSelf fixedPointY) =>
        (TSelf)TWrapped.MinMagnitudeNumber(fixedPointX.Value, fixedPointY.Value);

    #endregion


}

