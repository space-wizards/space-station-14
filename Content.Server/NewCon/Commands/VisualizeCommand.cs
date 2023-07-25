using System.Linq;
using Content.Server.EUI;
using Content.Shared.Bql;
using Content.Shared.Eui;
using Robust.Server.Player;
using Robust.Shared.RTShell;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;

namespace Content.Server.NewCon.Commands;

[RtShellCommand]
public sealed class VisualizeCommand : ToolshedCommand
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [CommandImplementation]
    public void VisualizeEntities(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<EntityUid> input
        )
    {
        if (ctx.Session is null)
        {
            ctx.ReportError(new NotForServerConsoleError());
            return;
        }

        var ui = new RtShellVisualizeEui(
            input.Select(e => (EntName(e), e)).ToArray()
        );
        _euiManager.OpenEui(ui, (IPlayerSession) ctx.Session);
        _euiManager.QueueStateUpdate(ui);
    }
}
internal sealed class RtShellVisualizeEui : BaseEui
{
    private readonly (string name, EntityUid entity)[] _entities;

    public RtShellVisualizeEui((string name, EntityUid entity)[] entities)
    {
        _entities = entities;
    }

    public override EuiStateBase GetNewState()
    {
        return new RtShellVisualizeEuiState(_entities);
    }
}

