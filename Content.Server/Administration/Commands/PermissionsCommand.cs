using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;

namespace Content.Server.Administration.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Permissions)]
public sealed class PermissionsCommand : ToolshedCommand
{
    [Dependency] private readonly EuiManager _euiManager = default!;

    [CommandImplementation]
    public void Permissions(IInvocationContext ctx)
    {
        if (ctx.Session is null)
        {
            ctx.ReportError(new NotForServerConsoleError());
            return;
        }

        _euiManager.OpenEui(new PermissionsEui(), ctx.Session);
    }
}
