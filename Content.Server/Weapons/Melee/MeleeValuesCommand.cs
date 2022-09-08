using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Weapons.Melee;

[AdminCommand(AdminFlags.Debug)]
public sealed class MeleeValuesCommand : IConsoleCommand
{
    public string Command => "showmeleevalues";
    public string Description => "Dumps all melee stats into a table.";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not IPlayerSession pSession)
        {
            shell.WriteError($"{Command} can't be run on server!");
            return;
        }

        var euiManager = IoCManager.Resolve<EuiManager>();
        var eui = new MeleeWeaponEui();
        euiManager.OpenEui(eui, pSession);
        eui.StateDirty();
    }
}
