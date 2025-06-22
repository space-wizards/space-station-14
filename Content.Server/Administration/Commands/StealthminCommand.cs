using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Stealth)]
public sealed class StealthminCommand : LocalizedCommands
{
    [Dependency] private readonly IAdminManager _adminManager = default!;

    public override string Command => "stealthmin";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var adminData = _adminManager.GetAdminData(player);

        DebugTools.AssertNotNull(adminData);

        if (!adminData!.Stealth)
            _adminManager.Stealth(player);
        else
            _adminManager.UnStealth(player);
    }
}
