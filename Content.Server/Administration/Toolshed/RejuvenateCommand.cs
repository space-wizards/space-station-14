using Content.Server.Administration.Systems;
using Robust.Shared.Toolshed;

namespace Content.Server.Administration.Toolshed;

[ToolshedCommand]
public sealed class RejuvenateCommand : ToolshedCommand
{
    private RejuvenateSystem? _rejuvenate;
    [CommandImplementation]
    public IEnumerable<EntityUid> Rejuvenate([PipedArgument] IEnumerable<EntityUid> input)
    {
        _rejuvenate ??= GetSys<RejuvenateSystem>();

        foreach (var i in input)
        {
            _rejuvenate.PerformRejuvenate(i);
            yield return i;
        }
    }
}
