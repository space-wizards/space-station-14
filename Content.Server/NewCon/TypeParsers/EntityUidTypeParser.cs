using System.Diagnostics.CodeAnalysis;
using Content.Server.NewCon.Errors;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.TypeParsers;

public sealed class EntityUidTypeParser : TypeParser<EntityUid>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var start = parser.Index;
        var word = parser.GetWord();
        error = null;

        if (!EntityUid.TryParse(word, out var ent))
        {
            result = null;

            if (word is not null)
                error = new InvalidEntityUid(word);
            else
                error = new OutOfInputError();

            error.Contextualize(parser.Input, (start, parser.Index));
            return false;
        }

        result = ent;
        return true;
    }
}

public record struct InvalidEntityUid(string Value) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup($"Couldn't parse {Value} as an entity ID. Entity IDs are numeric, optionally starting with a c to indicate client-sided-ness.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
}
