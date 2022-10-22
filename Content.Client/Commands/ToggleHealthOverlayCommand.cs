using Content.Client.HealthOverlay;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Client.Commands
{
    public sealed class ToggleHealthOverlayCommand : IConsoleCommand
    {
        public string Command => "togglehealthoverlay";
        public string Description => "Toggles a health bar above mobs.";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<HealthOverlaySystem>();
            system.Enabled = !system.Enabled;

            shell.WriteLine($"Health overlay system {(system.Enabled ? "enabled" : "disabled")}.");
        }
    }
}
