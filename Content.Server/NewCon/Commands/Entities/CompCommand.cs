using System.Linq;
using Content.Server.NewCon.TypeParsers;

namespace Content.Server.NewCon.Commands.Entities;

[ConsoleCommand]
public sealed class CompCommand : ConsoleCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(ComponentType)};

    [CommandImplementation]
    public IEnumerable<T> CompEnumerable<T>([PipedArgument] IEnumerable<EntityUid> input)
        where T: IComponent
    {
        return input.Where(HasComp<T>).Select(Comp<T>);
    }

    /*[CommandImplementation]
    public T? CompDirect<T>([PipedArgument] EntityUid input)
        where T : Component
    {
        _entity.TryGetComponent(input, out T? res);
        return res;
    }*/
}
