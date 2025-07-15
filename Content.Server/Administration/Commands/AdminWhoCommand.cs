using System.Text;
using Content.Server.Administration.Managers;
using Content.Server.Afk;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.AdminWho)]
public sealed class AdminWhoCommand : LocalizedCommands
{
    [Dependency] private readonly IAfkManager _afkManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;

    public override string Command => "adminwho";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var seeStealth = true;

        // If null it (hopefully) means it is being called from the console.
        if (shell.Player != null)
        {
            var playerData = _adminManager.GetAdminData(shell.Player);

            seeStealth = playerData != null && playerData.CanStealth();
        }

        var sb = new StringBuilder();
        var first = true;
        foreach (var admin in _adminManager.ActiveAdmins)
        {
            var adminData = _adminManager.GetAdminData(admin)!;
            DebugTools.AssertNotNull(adminData);

            if (adminData.Stealth && !seeStealth)
                continue;

            if (!first)
                sb.Append('\n');
            first = false;

            sb.Append(admin.Name);
            if (adminData.Title is { } title)
                sb.Append($": [{title}]");

            if (adminData.Stealth)
                sb.Append(" (S)");

            if (shell.Player is { } player && _adminManager.HasAdminFlag(player, AdminFlags.Admin))
            {
                if (_afkManager.IsAfk(admin))
                    sb.Append(" [AFK]");
            }
        }

        shell.WriteLine(sb.ToString());
    }
}
