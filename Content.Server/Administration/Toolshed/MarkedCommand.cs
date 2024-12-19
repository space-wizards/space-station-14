using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server.Administration.Toolshed;

[ToolshedCommand, AnyCommand]
public sealed class MarkedCommand : ToolshedCommand
{
    [CommandImplementation]
    public IEnumerable<EntityUid> Marked(IInvocationContext ctx)
    {
        var res = (IEnumerable<EntityUid>?)ctx.ReadVar("marked");
        res ??= Array.Empty<EntityUid>();
        return res;
    }
}
