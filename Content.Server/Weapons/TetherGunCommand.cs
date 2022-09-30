using Content.Server.Administration;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Administration;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Console;

namespace Content.Server.Weapons;

[AdminCommand(AdminFlags.Fun)]
public sealed class TetherGunCommand : IConsoleCommand
{
    public string Command => SharedTetherGunSystem.CommandName;
    public string Description => "Allows you to drag mobs around with your mouse.";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<TetherGunSystem>();
        system.Toggle(shell.Player);

        if (system.IsEnabled(shell.Player))
            shell.WriteLine("Tether gun toggled on");
        else
            shell.WriteLine("Tether gun toggled off");
    }
}
