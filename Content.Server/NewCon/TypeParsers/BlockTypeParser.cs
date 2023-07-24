using System.Diagnostics.CodeAnalysis;
using Content.Server.NewCon.Errors;
using Content.Server.NewCon.Syntax;

namespace Content.Server.NewCon.TypeParsers;

public sealed class BlockTypeParser<T> : TypeParser<Block<T>>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var r = Block<T>.TryParse(parser, null, out var block, out error);
        result = block;
        return r;
    }
}

public sealed class BlockTypeParser<TIn, TOut> : TypeParser<Block<TIn, TOut>>
{
    public override bool TryParse(ForwardParser parser, [NotNullWhen(true)] out object? result, out IConError? error)
    {
        var r = Block<TIn, TOut>.TryParse(parser, null, out var block, out error);
        result = block;
        return r;
    }
}
