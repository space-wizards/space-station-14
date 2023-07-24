namespace Content.Server.NewCon.Commands.Debug;

[ConsoleCommand]
public sealed class FuckCommand : ConsoleCommand
{
    [CommandImplementation]
    public object? Fuck([PipedArgument] object? value)
    {
        throw new Exception("fuck!");
    }
}
