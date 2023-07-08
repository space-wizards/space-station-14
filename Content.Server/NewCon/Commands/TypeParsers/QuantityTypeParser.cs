using System.Diagnostics.CodeAnalysis;

namespace Content.Server.NewCon.Commands.TypeParsers;

public sealed class QuantityParser : TypeParser<Quantity>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result)
    {
        var word = parser.GetWord();

        if (word?.TrimEnd('%') is not { } maybeParseable || !float.TryParse(word, out var v))
        {
            result = null;
            return false;
        }

        if (word.EndsWith('%'))
        {
            result = new Quantity(null, (v / 100.0f));
            return true;
        }

        result = new Quantity(v, null);
        return true;
    }
}

public readonly record struct Quantity(float? Amount, float? Percentage);
