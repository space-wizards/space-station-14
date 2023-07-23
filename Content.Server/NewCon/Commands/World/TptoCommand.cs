namespace Content.Server.NewCon.Commands.World;

[ConsoleCommand]
public sealed class TptoCommand : ConsoleCommand
{
    [CommandImplementation]
    public void TpTo([PipedArgument] IEnumerable<EntityUid> input, [CommandArgument] EntityUid target)
    {

    }
}
