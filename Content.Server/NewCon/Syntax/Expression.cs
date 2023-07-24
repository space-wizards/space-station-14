using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.NewCon.Errors;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.Syntax;

/// <summary>
/// A "run" of commands. Not a true expression.
/// </summary>
public sealed class CommandRun
{
    public List<(ParsedCommand, Vector2i)> Commands;
    private string _originalExpr;

    public static bool TryParse(ForwardParser parser, Type? pipedType, Type? targetOutput, bool once, [NotNullWhen(true)] out CommandRun? expr, out IConError? error)
    {
        error = null;
        var cmds = new List<(ParsedCommand, Vector2i)>();
        var start = parser.Index;
        var noCommand = false;

        while ((!once || cmds.Count < 1) && ParsedCommand.TryParse(parser, pipedType, out var cmd, out error, out noCommand, targetOutput))
        {
            var end = parser.Index;
            pipedType = cmd.ReturnType;
            cmds.Add((cmd, (start, end)));
            if (cmd.ReturnType == targetOutput)
                goto done;
            start = parser.Index;
        }

        if (error is not null and not OutOfInputError || error is not null && noCommand && cmds.Count == 0 || cmds.Count == 0)
        {
            expr = null;
            return false;
        }

        if (cmds.Last().Item1.ReturnType != targetOutput && targetOutput is not null)
        {
            error = new ExpressionOfWrongType(targetOutput, cmds.Last().Item1.ReturnType!, once);
            expr = null;
            return false;
        }

        done:
        expr = new CommandRun(cmds, parser.Input);
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


    private CommandRun(List<(ParsedCommand, Vector2i)> commands, string originalExpr)
    {
        Commands = commands;
        _originalExpr = originalExpr;
    }
}

public sealed class CommandRun<TIn, TOut>
{
    public CommandRun InnerCommandRun;

    public static bool TryParse(ForwardParser parser, bool once,
        [NotNullWhen(true)] out CommandRun<TIn, TOut>? expr, out IConError? error)
    {
        if (!CommandRun.TryParse(parser, typeof(TIn), typeof(TOut), once, out var innerExpr, out error))
        {
            expr = null;
            return false;
        }

        expr = new CommandRun<TIn, TOut>(innerExpr);
        return true;
    }

    public TOut? Invoke(object? input, IInvocationContext ctx)
    {
        var res = InnerCommandRun.Invoke(input, ctx);
        if (res is null)
            return default;
        return (TOut?) res;
    }

    private CommandRun(CommandRun commandRun)
    {
        InnerCommandRun = commandRun;
    }
}

public sealed class CommandRun<TRes>
{
    public CommandRun InnerCommandRun;

    public static bool TryParse(ForwardParser parser, Type? pipedType, bool once,
        [NotNullWhen(true)] out CommandRun<TRes>? expr, out IConError? error)
    {
        if (!CommandRun.TryParse(parser, pipedType, typeof(TRes), once, out var innerExpr, out error))
        {
            expr = null;
            return false;
        }

        expr = new CommandRun<TRes>(innerExpr);
        return true;
    }

    public TRes? Invoke(object? input, IInvocationContext ctx)
    {
        var res = InnerCommandRun.Invoke(input, ctx);
        if (res is null)
            return default;
        return (TRes?) res;
    }

    private CommandRun(CommandRun commandRun)
    {
        InnerCommandRun = commandRun;
    }
}

public record struct ExpressionOfWrongType(Type Expected, Type Got, bool Once) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = FormattedMessage.FromMarkup(
            $"Expected an expression of type {Expected.PrettyName()}, but got {Got.PrettyName()}");

        if (Once)
        {
            msg.PushNewline();
            msg.AddText("Note: A single command is expected here, if you were trying to chain commands please surround the run with { } to form a block.");
        }

        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
