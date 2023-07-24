using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Content.Server.NewCon.Errors;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.Syntax;

/// <summary>
/// Something more akin to actual expressions.
/// </summary>
public sealed class Block<T>
{
    public CommandRun<T> CommandRun { get; set; }

    public static bool TryParse(ForwardParser parser, Type? pipedType,
        [NotNullWhen(true)] out Block<T>? block, out IConError? error)
    {
        parser.Consume(char.IsWhiteSpace);

        var enclosed = parser.EatMatch('{');

        CommandRun<T>.TryParse(parser, pipedType, !enclosed, out var expr, out error);

        if (expr is null)
        {
            block = null;
            return false;
        }

        if (enclosed && !parser.EatMatch('}'))
        {
            error = new MissingClosingBrace();
            block = null;
            return false;
        }

        block = new Block<T>(expr);
        return true;
    }

    public Block(CommandRun<T> expr)
    {
        CommandRun = expr;
    }

    public T? Invoke(object? input, IInvocationContext ctx)
    {
        return CommandRun.Invoke(input, ctx);
    }
}

public sealed class Block<TIn, TOut>
{
    public CommandRun<TIn, TOut> CommandRun { get; set; }

    public static bool TryParse(ForwardParser parser, Type? pipedType,
        [NotNullWhen(true)] out Block<TIn, TOut>? block, out IConError? error)
    {
        parser.Consume(char.IsWhiteSpace);

        var enclosed = parser.EatMatch('{');

        CommandRun<TIn, TOut>.TryParse(parser, !enclosed, out var expr, out error);

        if (expr is null)
        {
            block = null;
            return false;
        }

        if (enclosed && !parser.EatMatch('}'))
        {
            error = new MissingClosingBrace();
            block = null;
            return false;
        }

        block = new Block<TIn, TOut>(expr);
        return true;
    }

    public Block(CommandRun<TIn, TOut> expr)
    {
        CommandRun = expr;
    }

    public TOut? Invoke(object? input, IInvocationContext ctx)
    {
        return CommandRun.Invoke(input, ctx);
    }
}


public record struct MissingClosingBrace() : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup("Expected a closing brace.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
