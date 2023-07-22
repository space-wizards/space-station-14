using System.Linq;
using Content.Server.NewCon.TypeParsers;

namespace Content.Server.NewCon.Commands.Entities;

[ConsoleCommand]
public sealed class CompCommand : ConsoleCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;

    public override Type[] TypeParameterParsers => new[] {typeof(ComponentType)};

    [CommandImplementation]
    public IEnumerable<T> With<T>([PipedArgument] IEnumerable<EntityUid> input)
        where T: IComponent
    {
        return input.Where(x => _entity.HasComponent<T>(x)).Select(x => _entity.GetComponent<T>(x));
    }
}
