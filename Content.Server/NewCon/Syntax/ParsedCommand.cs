using System.Diagnostics.CodeAnalysis;
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

    public static bool TryParse(ForwardParser parser, Type? pipedArgumentType, [NotNullWhen(true)] out ParsedCommand? result)
    {
        var checkpoint = parser.Save();
        // todo: pretty errors
        var bundle = new CommandArgumentBundle()
            {Arguments = new(), Inverted = false, PipedArgumentType = pipedArgumentType, TypeArguments = Array.Empty<Type>()};

        if (!TryDigestModifiers(parser, bundle)
            || !TryParseCommand(parser, bundle, out var invocable, out var command)
            || !command.TryGetReturnType(pipedArgumentType, bundle.TypeArguments, out var retType)
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

    private static bool TryDigestModifiers(ForwardParser parser, CommandArgumentBundle bundle)
    {
        if (parser.PeekWord() == "not")
        {
            parser.GetWord(); //yum
            bundle.Inverted = true;
        }

        return true;
    }

    private static bool TryParseCommand(ForwardParser parser, CommandArgumentBundle bundle, [NotNullWhen(true)] out Invocable? invocable, [NotNullWhen(true)] out ConsoleCommand? command)
    {
        var cmd = parser.GetWord();
        invocable = null;
        command = null;
        if (cmd is null)
        {
            return false;
        }

        if (!parser.NewCon.TryGetCommand(cmd, out var cmdImpl))
        {
            return false;
        }

        string? subCommand = null;
        if (cmdImpl.HasSubCommands)
        {
            if (parser.GetWord() is not { } subcmd)
            {
                return false;
            }

            subCommand = subcmd;
        }

        parser.Consume(char.IsWhiteSpace);

        if (!cmdImpl.TryParseArguments(parser, out var args, out var types))
        {
            return false;
        }

        bundle.TypeArguments = types;

        if (!cmdImpl.TryGetImplementation(bundle.PipedArgumentType, subCommand, types, out var impl))
        {
            return false;
        }

        bundle.Arguments = args;
        invocable = impl;
        command = cmdImpl;

        return true;
    }

    public object? Invoke(object? pipedIn)
    {
        return Invocable.Invoke(new CommandInvocationArguments() {Bundle = Bundle, PipedArgument = pipedIn});
    }
}
