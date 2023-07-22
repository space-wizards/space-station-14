using System.Linq;

namespace Content.Server.NewCon.Commands.Entities;

[ConsoleCommand]
public sealed class PausedCommand : ConsoleCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;

    [CommandImplementation]
    public IEnumerable<EntityUid> Paused([PipedArgument] IEnumerable<EntityUid> entities, [CommandInverted] bool inverted)
    {
        Logger.Debug($"{inverted}");
        return entities.Where(x => _entity.GetComponent<MetaDataComponent>(x).EntityPaused ^ inverted);
    }
}
