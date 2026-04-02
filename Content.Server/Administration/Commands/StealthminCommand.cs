using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Stealth)]
public sealed class StealthminCommand : ToolshedCommand
{
    [Dependency] private readonly IAdminManager _adminManager = default!;

    [CommandImplementation]
    public void Stealthmin(IInvocationContext ctx)
    {
        if (ctx.Session is not { } admin)
        {
            ctx.ReportError(new NotForServerConsoleError());
            return;
        }

        var adminData = _adminManager.GetAdminData(admin);

        DebugTools.AssertNotNull(adminData);

        if (!adminData!.Stealth)
            _adminManager.Stealth(admin);
        else
            _adminManager.UnStealth(admin);
    }
}
