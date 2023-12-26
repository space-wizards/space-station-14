using System.Globalization;
using System.Linq;

namespace Content.Shared.Localizations;

/// <summary>
/// Helpers for user input parsing.
/// </summary>
/// <remarks>
/// A wrapper around the <see cref="System.Single.TryParse(string, out float)"/> API,
/// with the goal of trying more options to make the user input parsing less restrictive.
/// For culture-invariant parsing use <see cref="Robust.Shared.Utility.Parse"/>.
/// </remarks>
public static class UserInputParser
{
    private static readonly IEqualityComparer<CultureInfo> NumberDecimalSeparatorEqualityComparer =
        EqualityComparer<CultureInfo>.Create((a, b) =>
            string.Equals(a?.NumberFormat.NumberDecimalSeparator, b?.NumberFormat.NumberDecimalSeparator,
                StringComparison.Ordinal), (c) => c.NumberFormat.NumberDecimalSeparator.GetHashCode());

    private static readonly CultureInfo InvariantCultureWithDecimalDot = new CultureInfo("")
    {
        NumberFormat =
        {
            NumberDecimalSeparator = "."
        }
    };

    private static readonly CultureInfo InvariantCultureWithDecimalComma = new CultureInfo("")
    {
        NumberFormat =
        {
            NumberDecimalSeparator = ","
        }
    };

    private static IEnumerable<CultureInfo> GetCulturesForDecimalNumberParsing(ILocalizationManager loc)
    {
        var culturesToTry = new[] { loc.DefaultCulture }.Concat(loc.FallbackCultures)
            .Concat(new[] { InvariantCultureWithDecimalDot, InvariantCultureWithDecimalComma });

        return culturesToTry.OfType<CultureInfo>().Distinct(NumberDecimalSeparatorEqualityComparer);
    }

    public static bool TryFloat(ReadOnlySpan<char> text, ILocalizationManager loc, out float result)
    {
        foreach (var culture in GetCulturesForDecimalNumberParsing(loc))
        {
            if (float.TryParse(text, NumberStyles.Float, culture, out result))
            {
                return true;
            }
        }

        result = 0f;
        return false;
    }

    public static bool TryDouble(ReadOnlySpan<char> text, ILocalizationManager loc, out double result)
    {
        foreach (var culture in GetCulturesForDecimalNumberParsing(loc))
        {
            if (double.TryParse(text, NumberStyles.Float, culture, out result))
            {
                return true;
            }
        }

        result = 0d;
        return false;
    }
}
