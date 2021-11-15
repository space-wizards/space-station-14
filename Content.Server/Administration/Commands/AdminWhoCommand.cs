using Content.Server.Administration.Managers;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Commands;

[AnyCommand]
public sealed class AdminWhoCommand : IConsoleCommand
{
    public string Command => "adminwho";
    public string Description => "Returns a list of all admins on the server";
    public string Help => "Usage: adminwho";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var adminMgr = IoCManager.Resolve<IAdminManager>();
        foreach (var admin in adminMgr.ActiveAdmins)
        {
            var adminData = adminMgr.GetAdminData(admin)!;
            DebugTools.AssertNotNull(adminData);

            shell.WriteLine(adminData.Title != null ? $"{admin.Name}: {adminData.Title}" : admin.Name);
        }
    }
}
