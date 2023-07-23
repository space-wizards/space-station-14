namespace Content.Server.NewCon.Commands.Entities;

[ConsoleCommand]
public sealed class EntCommand : ConsoleCommand
{
    [CommandImplementation]
    public EntityUid Ent([CommandArgument] EntityUid ent) => ent;
}

