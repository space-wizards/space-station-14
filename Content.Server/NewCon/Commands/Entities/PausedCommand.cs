using System.Linq;

namespace Content.Server.NewCon.Commands.Entities;

[ConsoleCommand]
public sealed class PausedCommand : ConsoleCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> Paused([PipedArgument] IEnumerable<EntityUid> entities, [CommandInverted] bool inverted)
    {
        return entities.Where(x => Comp<MetaDataComponent>(x).EntityPaused ^ inverted);
    }
}
