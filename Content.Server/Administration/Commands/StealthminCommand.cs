using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Commands;

[UsedImplicitly]
[AdminCommand(AdminFlags.Stealth)]
public sealed class StealthminCommand : IConsoleCommand
{
    public string Command => "stealthmin";
    public string Description => "Toggle whether others can see you in adminwho";
    public string Help => "Usage: stealthmin\nUse stealthmin to toggle whether you appear in the output of the adminwho command.";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine("You cannot use this command from the server console.");
                return;
            }

            var mgr = IoCManager.Resolve<IAdminManager>();

            var adminData = mgr.GetAdminData(player);

            DebugTools.AssertNotNull(adminData);

            if (!adminData!.Stealth)
            {
                mgr.Stealth(player);
            }
            else
            {
                mgr.UnStealth(player);
            }
    }
}
