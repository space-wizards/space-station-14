using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Server.NewCon.Errors;
using Microsoft.Extensions.Primitives;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.TypeParsers;

public sealed class StringTypeParser : TypeParser<string>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        error = null;
        parser.Consume(char.IsWhiteSpace);
        if (parser.PeekChar() is not '"')
        {
            if (parser.PeekChar() is null)
            {
                error = new OutOfInputError();
                result = null;
                return false;
            }

            error = new StringMustStartWithQuote();
            error.Contextualize(parser.Input, (parser.Index, parser.Index + 1));
            result = null;
            return false;
        }

        parser.GetChar();

        var output = new StringBuilder();

        while (true)
        {
            while (parser.PeekChar() is not '"' and not '\\') { output.Append(parser.GetChar()); }

            if (parser.PeekChar() is '"' or null)
            {
                parser.GetChar();
                break;
            }

            parser.GetChar(); // okay it's \

            switch (parser.GetChar())
            {
                case '"':
                    output.Append('"');
                    continue;
                case 'n':
                    output.Append('\n');
                    continue;
                case '\\':
                    output.Append('\\');
                    continue;
                default:
                    result = null;
                    // todo: error
                    return false;
            }
        }

        parser.Consume(char.IsWhiteSpace);

        result = output.ToString();
        return true;
    }
}

public record struct StringMustStartWithQuote : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup("A string must start with a quote.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
}
