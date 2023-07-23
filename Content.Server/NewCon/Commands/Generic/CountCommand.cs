using System.Collections;
using System.Linq;

namespace Content.Server.NewCon.Commands.Generic;

[ConsoleCommand]
public sealed class CountCommand : ConsoleCommand
{
    [CommandImplementation, TakesPipedTypeAsGeneric]
    public int Count<T>([PipedArgument] IEnumerable<T> enumerable)
    {
        return enumerable.Count();
    }
}
