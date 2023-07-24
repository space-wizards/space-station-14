using System.Collections;
using System.Linq;
using Content.Server.NewCon.Syntax;

namespace Content.Server.NewCon.Commands.Generic;

[ConsoleCommand]
public sealed class MapCommand : ConsoleCommand
{
    public override Type[] TypeParameterParsers => new[] {typeof(Type)};

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public IEnumerable<TOut>? Map<TOut, TIn>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<TIn> value,
        [CommandArgument] Block<TIn, TOut> block)
    {
        return value.Select(x => block.Invoke(x, ctx)).Where(x => x != null).Cast<TOut>();
    }
}
