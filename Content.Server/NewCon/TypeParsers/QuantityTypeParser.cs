using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Content.Server.NewCon.Errors;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.TypeParsers;

public sealed class QuantityParser : TypeParser<Quantity>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var word = parser.GetWord();
        error = null;

        if (word?.TrimEnd('%') is not { } maybeParseable || !float.TryParse(maybeParseable, out var v))
        {
            if (word is not null)
                error = new InvalidQuantity(word);
            else
                error = new OutOfInputError();

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

public record struct InvalidQuantity(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup(
            $"The value {Value} is not a valid quantity. Please input some decimal number, optionally with a % to indicate that it's a percentage.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
