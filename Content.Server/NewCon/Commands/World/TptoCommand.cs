using System.Linq;
using Robust.Server.GameObjects;
using Robust.Shared.RTShell;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Server.NewCon.Commands.World;

[RtShellCommand]
public sealed class TptoCommand : ToolshedCommand
{
    [Dependency] private readonly IEntityManager _entity = default!;
    private TransformSystem? _xform;

    [CommandImplementation]
    public IEnumerable<EntityUid> TpTo(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] CommandRun<EntityUid> target
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
