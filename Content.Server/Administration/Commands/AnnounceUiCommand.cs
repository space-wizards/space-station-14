using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;

namespace Content.Server.Administration.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Moderator)]
public sealed class AnnounceUiCommand : ToolshedCommand
{
    [Dependency] private readonly EuiManager _euiManager = default!;

    [CommandImplementation]
    public void AnnounceUi(IInvocationContext ctx)
    {
        if (ctx.Session is null)
        {
            ctx.ReportError(new NotForServerConsoleError());
            return;
        }

        _euiManager.OpenEui(new AdminAnnounceEui(), ctx.Session);
    }
}
