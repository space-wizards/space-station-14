using Robust.Shared.Console;

namespace Content.Client.Weapons.Ranged;

public sealed class TetherGunCommand : IConsoleCommand
{
    public string Command => "tethergun";
    public string Description => "Allows you to drag mobs around with your mouse.";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<TetherGunSystem>().Enabled ^= true;
    }
}
