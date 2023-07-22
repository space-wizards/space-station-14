using System.Linq;
using Content.Server.NewCon.TypeParsers;

namespace Content.Server.NewCon.Commands.Entities;

[ConsoleCommand]
public sealed class WithCommand : ConsoleCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;

    [CommandImplementation]
    public IEnumerable<EntityUid> With([PipedArgument] IEnumerable<EntityUid> input, [CommandArgument] ComponentType ty)
    {
        return input.Where(x => _entity.HasComponent(x, ty.Ty));
    }
}
