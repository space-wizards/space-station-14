namespace Content.Server.NewCon.Commands.Generic;

[ConsoleCommand]
public sealed class AsCommand : ConsoleCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(Type)};

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public TOut? As<TOut, TIn>([PipedArgument] TIn value)
        => (TOut?)(object?)value;
}
