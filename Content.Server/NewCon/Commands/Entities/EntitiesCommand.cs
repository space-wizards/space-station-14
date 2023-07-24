namespace Content.Server.NewCon.Commands.Entities;

[ConsoleCommand]
public sealed class EntitiesCommand : ConsoleCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> Entities()
    {
        return EntityManager.GetEntities();
    }
}
