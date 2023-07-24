using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Content.Server.NewCon.Errors;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.TypeParsers;

public sealed class TypeTypeParser : TypeParser<Type>
{
    [Dependency] private readonly IModLoader _modLoader = default!;
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var res = parser.GetWord();
        // TODO: Do this right.
        result = Type.GetType(res ?? "", false, true);
        /* TODO
        if (!_modLoader.IsContentTypeAccessAllowed((Type?) result ?? typeof(void)))
        {
            error = new TypeIsSandboxViolation((Type?) result ?? typeof(void));
            result = null;
            return false;
        }
        */
        error = null;
        return result is not null;
    }
}

internal record struct TypeIsSandboxViolation(Type T) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = new FormattedMessage();
        msg.AddText($"The type {T.PrettyName()} is not permitted under sandbox rules.");
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
