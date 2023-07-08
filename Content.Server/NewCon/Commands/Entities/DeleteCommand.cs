namespace Content.Server.NewCon.Commands.Entities;

[ConsoleCommand]
public sealed class DeleteCommand : ConsoleCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;

    [CommandImplementation]
    public void Delete([PipedArgument] IEnumerable<EntityUid> entities)
    {
        foreach (var ent in entities)
        {
            _entity.DeleteEntity(ent);
        }
    }
}
