using Content.Server.EUI;
using Content.Server.Fax.AdminUI;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;

namespace Content.Server.Administration.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class FaxUiCommand : ToolshedCommand
{
    [Dependency] private readonly EuiManager _euiManager = default!;

    [CommandImplementation]
    public void FaxUi(IInvocationContext ctx)
    {
        if (ctx.Session is null)
        {
            ctx.ReportError(new NotForServerConsoleError());
            return;
        }

        _euiManager.OpenEui(new AdminFaxEui(), ctx.Session);
    }
}
