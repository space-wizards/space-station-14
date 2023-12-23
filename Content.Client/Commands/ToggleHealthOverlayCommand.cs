using Content.Client.HealthOverlay;
using Robust.Shared.Console;

namespace Content.Client.Commands
{
    public sealed class ToggleHealthOverlayCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        // ReSharper disable once StringLiteralTypo
        public string Command => "togglehealthoverlay";
        public string Description => Loc.GetString("toggle-health-overlay-command-description");
        public string Help => Loc.GetString("toggle-health-overlay-command-help", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var system = _entitySystemManager.GetEntitySystem<HealthOverlaySystem>();
            system.Enabled = !system.Enabled;

            shell.WriteLine(Loc.GetString("toggle-health-overlay-command-notify", ("state", system.Enabled ? "enabled" : "disabled")));
        }
    }
}
