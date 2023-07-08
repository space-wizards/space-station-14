namespace Content.Server.NewCon.Commands.Entities;

[ConsoleCommand]
public sealed class EntitiesCommand : ConsoleCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;

    [CommandImplementation]
    public IEnumerable<EntityUid> Entities()
    {
        return _entity.GetEntities();
    }
}
