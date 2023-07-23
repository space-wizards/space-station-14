using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.NewCon.Errors;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.Syntax;

using Invocable = Func<CommandInvocationArguments, object?>;

public sealed class ParsedCommand
{
    public ConsoleCommand Command { get; }
    public Type? ReturnType { get; }

    public Type? PipedType => Bundle.PipedArgumentType;
    public Invocable Invocable { get; }
    public CommandArgumentBundle Bundle { get; }

    public static bool TryParse(ForwardParser parser, Type? pipedArgumentType, [NotNullWhen(true)] out ParsedCommand? result, out IConError? error, out bool noCommand)
    {
        noCommand = false;
        error = null;
        var checkpoint = parser.Save();
        var bundle = new CommandArgumentBundle()
            {Arguments = new(), Inverted = false, PipedArgumentType = pipedArgumentType, TypeArguments = Array.Empty<Type>()};

        if (!TryDigestModifiers(parser, bundle, out error)
            || !TryParseCommand(parser, bundle, out var subCommand, out var invocable, out var command, out error, out noCommand)
            || !command.TryGetReturnType(subCommand, pipedArgumentType, bundle.TypeArguments, out var retType)
            )
        {
            result = null;
            parser.Restore(checkpoint);
            return false;
        }


        result = new(bundle, invocable, command, retType);
        return true;
    }

    private ParsedCommand(CommandArgumentBundle bundle, Invocable invocable, ConsoleCommand command, Type? returnType)
    {
        Invocable = invocable;
        Bundle = bundle;
        Command = command;
        ReturnType = returnType;
    }

    private static bool TryDigestModifiers(ForwardParser parser, CommandArgumentBundle bundle, out IConError? error)
    {
        error = null;
        if (parser.PeekWord() == "not")
        {
            parser.GetWord(); //yum
            bundle.Inverted = true;
        }

        return true;
    }

    private static bool TryParseCommand(ForwardParser parser, CommandArgumentBundle bundle, out string? subCommand, [NotNullWhen(true)] out Invocable? invocable, [NotNullWhen(true)] out ConsoleCommand? command, out IConError? error, out bool noCommand)
    {
        noCommand = false;
        error = null;
        var start = parser.Index;
        var cmd = parser.GetWord(char.IsAsciiLetterOrDigit);
        subCommand = null;
        invocable = null;
        command = null;
        if (cmd is null)
        {
            noCommand = true;
            error = new OutOfInputError();
            error.Contextualize(parser.Input, (parser.Index, parser.Index));
            return false;
        }

        if (!parser.NewCon.TryGetCommand(cmd, out var cmdImpl))
        {
            error = new UnknownCommandError(cmd);
            error.Contextualize(parser.Input, (start, parser.Index));
            return false;
        }

        if (cmdImpl.HasSubCommands)
        {
            if (parser.GetChar() is not ':')
                return false;

            var subCmdStart = parser.Index;

            if (parser.GetWord() is not { } subcmd)
            {
                noCommand = true;
                error = new OutOfInputError();
                error.Contextualize(parser.Input, (parser.Index, parser.Index));
                return false;
            }

            if (!cmdImpl.Subcommands.Contains(subcmd))
            {
                noCommand = true;
                error = new UnknownSubcommandError(cmd, subcmd, cmdImpl);
                error.Contextualize(parser.Input, (subCmdStart, parser.Index));
                return false;
            }

            subCommand = subcmd;
        }

        parser.Consume(char.IsWhiteSpace);

        var argsStart = parser.Index;

        if (!cmdImpl.TryParseArguments(parser, out var args, out var types, out error))
        {
            noCommand = true;
            error?.Contextualize(parser.Input, (argsStart, parser.Index));
            return false;
        }

        bundle.TypeArguments = types;

        if (!cmdImpl.TryGetImplementation(bundle.PipedArgumentType, subCommand, types, out var impl))
        {
            noCommand = true;
            error = new NoImplementationError(cmd, types, subCommand, bundle.PipedArgumentType);
            error.Contextualize(parser.Input, (start, parser.Index));
            return false;
        }

        bundle.Arguments = args;
        invocable = impl;
        command = cmdImpl;

        return true;
    }

    public object? Invoke(object? pipedIn, IInvocationContext ctx)
    {
        try
        {
            return Invocable.Invoke(new CommandInvocationArguments()
                {Bundle = Bundle, PipedArgument = pipedIn, Context = ctx});
        }
        catch (Exception e)
        {
            ctx.ReportError(new UnhandledExceptionError(e));
            return null;
        }
    }
}

public record struct UnknownCommandError(string Cmd) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup($"Got unknown command {Cmd}.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
}

public record struct NoImplementationError(string Cmd, Type[] Types, string? SubCommand, Type? PipedType) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = FormattedMessage.FromMarkup($"Could not find an implementation for {Cmd} given the constraints.");
        msg.PushNewline();

        var typeArgs = "";

        if (Types.Length != 0)
        {
            typeArgs = "<" + string.Join(",", Types.Select(ReflectionExtensions.PrettyName)) + ">";
        }

        msg.AddText($"Signature: {Cmd}{(SubCommand is not null ? $":{SubCommand}" : "")}{typeArgs} {PipedType?.PrettyName() ?? "void"} -> ???");
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
}

public record struct UnknownSubcommandError(string Cmd, string SubCmd, ConsoleCommand Command) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = new FormattedMessage();
        msg.AddText($"The command group {Cmd} doesn't have command {SubCmd}.");
        msg.PushNewline();
        msg.AddText($"The valid commands are: {string.Join(", ", Command.Subcommands)}.");
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
}
