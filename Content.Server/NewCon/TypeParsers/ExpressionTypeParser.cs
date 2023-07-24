using System.Diagnostics.CodeAnalysis;
using Content.Server.NewCon.Errors;
using Content.Server.NewCon.Syntax;

namespace Content.Server.NewCon.TypeParsers;

public sealed class ExpressionTypeParser : TypeParser<CommandRun>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var res = CommandRun.TryParse(parser, null, null, false, out var r, out error);
        result = r;
        return res;
    }
}

public sealed class ExpressionTypeParser<T> : TypeParser<CommandRun<T>>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var res = CommandRun<T>.TryParse(parser, null, false, out var r, out error);
        result = r;
        return res;
    }
}
