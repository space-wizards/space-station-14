using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Content.Server.NewCon.TypeParsers;

public sealed class StringTypeParser : TypeParser<string>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result)
    {
        if (parser.PeekChar() is not '"')
        {
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
