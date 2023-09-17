namespace Content.Shared.Power;

public sealed class PowerLabel
{
    public string GetPrefixedPowerValue(double value, string? unit = "W", string? format = "0.00 ")
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return value.ToString() + unit;

        if (value == 0)
            return value.ToString(format) + unit;

        char[] incPrefixes = new[] { 'k', 'M', 'G', 'T', 'P', 'E', 'Z', 'Y' };
        char[] decPrefixes = new[] { 'm', 'u', 'n', 'p', 'f', 'a', 'z', 'y' };

        int degree = Math.Min((int) Math.Floor(Math.Log10(Math.Abs(value)) / 3), 8);
        double scaled = value * Math.Pow(1000, -degree);

        char? prefix = null;
        switch (Math.Sign(degree))
        {
            case 1: prefix = incPrefixes[degree - 1]; break;
            case -1: prefix = decPrefixes[-degree - 1]; break;
        }

        return scaled.ToString(format) + prefix + unit;
    }
}
