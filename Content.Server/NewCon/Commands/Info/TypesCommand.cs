namespace Content.Server.NewCon.Commands.Info;

[ConsoleCommand]
public sealed class TypesCommand : ConsoleCommand
{
    [Dependency] private readonly NewConManager _newCon = default!;

    [CommandImplementation("consumers")]
    public void Consumers([CommandInvocationContext] IInvocationContext ctx, [PipedArgument] object? input)
    {
        var t = input is Type ? (Type)input : input!.GetType();

        ctx.WriteLine($"Valid intakers for {t.PrettyName()}:");

        foreach (var (command, subCommand) in _newCon.CommandsTakingType(t))
        {
            if (subCommand is null)
                ctx.WriteLine($"{command.Name}");
            else
                ctx.WriteLine($"{command.Name}:{subCommand}");
        }
    }

    [CommandImplementation("tree")]
    public IEnumerable<Type> Tree([CommandInvocationContext] IInvocationContext ctx, [PipedArgument] object? input)
    {
        var t = input is Type ? (Type)input : input!.GetType();
        return _newCon.AllSteppedTypes(t);
    }

    [CommandImplementation("gettype")]
    public Type GetType([PipedArgument] object? input)
    {
        return input?.GetType() ?? typeof(void);
    }
}
