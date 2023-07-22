using System.Diagnostics.CodeAnalysis;
using Content.Server.NewCon.Syntax;

namespace Content.Server.NewCon.TypeParsers;

public sealed class ExpressionTypeParser : TypeParser<Expression>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result)
    {
        var res = Expression.TryParse(parser, null, null, false, out var r);
        result = r;
        return res;
    }
}

public sealed class ExpressionTypeParser<T> : TypeParser<Expression<T>>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result)
    {
        var res = Expression<T>.TryParse(parser, null, false, out var r);
        result = r;
        return res;
    }
}
