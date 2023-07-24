using System.Linq;
using Content.Server.NewCon.Syntax;
using Robust.Server.GameObjects;

namespace Content.Server.NewCon.Commands.World;

[ConsoleCommand]
public sealed class TptoCommand : ConsoleCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;
    private TransformSystem? _xform;

    [CommandImplementation]
    public IEnumerable<EntityUid> TpTo(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] Expression<EntityUid> target
        )
    {
        _xform ??= GetSys<TransformSystem>();

        var targetId = target.Invoke(null, ctx);

        if (ctx.GetErrors().Any())
            yield break;


        foreach (var i in input)
        {

        }
    }
}
