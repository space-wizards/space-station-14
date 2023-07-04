using Content.Client.HealthOverlay;
using Robust.Shared.Console;

namespace Content.Client.Commands
{
    public sealed class ToggleHealthOverlayCommand : IConsoleCommand
    {
        public string Command => "togglehealthoverlay";
        public string Description => "Toggles a health bar above mobs.";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var system = EntitySystem.Get<HealthOverlaySystem>();
            system.Enabled = !system.Enabled;
            system.AddDamageContainers(args);

            shell.WriteLine($"{nameof(HealthOverlaySystem)} {(system.Enabled ? "enabled" : "disabled")}.");
        }
    }
}
