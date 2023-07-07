namespace Content.Server.NewCon.Commands;

[ConsoleCommand]
public sealed class EntitiesCommand : ConsoleCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;

    public override bool TryGetReturnType(Type? pipedType, out Type? type)
    {
        type = typeof(IEnumerable<EntityUid>);
        return true;
    }

    [CommandImplementation]
    public IEnumerable<EntityUid> Entities()
    {
        return _entity.GetEntities();
    }
}
