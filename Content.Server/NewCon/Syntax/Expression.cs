using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.NewCon.Errors;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.Syntax;

public sealed class Expression
{
    public List<(ParsedCommand, Vector2i)> Commands;
    private string _originalExpr;

    public static bool TryParse(ForwardParser parser, Type? pipedType, Type? targetOutput, bool once, [NotNullWhen(true)] out Expression? expr, out IConError? error)
    {
        error = null;
        var cmds = new List<(ParsedCommand, Vector2i)>();
        var start = parser.Index;
        var noCommand = false;
        while (ParsedCommand.TryParse(parser, pipedType, out var cmd, out error, out noCommand))
        {
            var end = parser.Index;
            if (cmds.Count != 1 && once)
            {
                expr = null;
                return false;
            }

            pipedType = cmd.ReturnType;
            cmds.Add((cmd, (start, end)));
            if (cmd.ReturnType == targetOutput)
                goto done;
            start = parser.Index;
        }

        if (error is not null and not OutOfInputError || error is not null && noCommand && cmds.Count == 0 || cmds.Count == 0 || parser.Index < parser.MaxIndex)
        {
            expr = null;
            return false;
        }

        if (cmds.Last().Item1.ReturnType != targetOutput && targetOutput is not null)
        {
            Logger.Debug("Bailing due to wrong return type");
            expr = null;
            return false;
        }

        done:
        expr = new Expression(cmds, parser.Input);
        return true;
    }

    public object? Invoke(object? input, IInvocationContext ctx, bool reportErrors = true)
    {
        var ret = input;
        foreach (var (cmd, span) in Commands)
        {
            ret = cmd.Invoke(ret, ctx);
            if (ctx.GetErrors().Any())
            {
                // Got an error, we need to report it and break out.
                foreach (var err in ctx.GetErrors())
                {
                    err.Contextualize(_originalExpr, span);
                    ctx.WriteLine(err.Describe());
                }

                return null;
            }
        }

        return ret;
    }


    private Expression(List<(ParsedCommand, Vector2i)> commands, string originalExpr)
    {
        Commands = commands;
        _originalExpr = originalExpr;
    }
}


public sealed class Expression<TRes>
{
    public Expression InnerExpression;

    public static bool TryParse(ForwardParser parser, Type? pipedType, bool once,
        [NotNullWhen(true)] out Expression<TRes>? expr, out IConError? error)
    {
        if (!Expression.TryParse(parser, pipedType, typeof(TRes), once, out var innerExpr, out error))
        {
            expr = null;
            return false;
        }

        expr = new Expression<TRes>(innerExpr);
        return true;
    }

    public TRes? Invoke(object? input, IInvocationContext ctx)
    {
        var res = InnerExpression.Invoke(input, ctx);
        if (res is null)
            return default;
        return (TRes?) res;
    }

    private Expression(Expression expression)
    {
        InnerExpression = expression;
    }
}

public record struct ExpressionOfWrongType(Type Expected, Type Got) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup(
            $"Expected an expression of type {Expected.PrettyName()}, but got {Got.PrettyName()}");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
}
