using Content.Client.HealthOverlay;
using Robust.Shared.Console;

namespace Content.Client.Commands
{
    public sealed class ToggleHealthOverlayCommand : IConsoleCommand
    {
        public string Command => "togglehealthoverlay";
        public string Description => Loc.GetString("toggle-health-overlay-command-description");
        public string Help => Loc.GetString("toggle-health-overlay-command-help", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var system = EntitySystem.Get<HealthOverlaySystem>();
            system.Enabled = !system.Enabled;

            shell.WriteLine(Loc.GetString("toggle-health-overlay-command-notify", ("state", (system.Enabled ? "enabled" : "disabled"))));
        }
    }
}
