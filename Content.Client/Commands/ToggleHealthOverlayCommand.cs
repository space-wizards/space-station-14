using Content.Client.HealthOverlay;
using Robust.Shared.Console;

namespace Content.Client.Commands;

public sealed class ToggleHealthOverlayCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public override string Command => "togglehealthoverlay";

    public override string Help => LocalizationManager.GetString($"cmd-{Command}-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = _entitySystemManager.GetEntitySystem<HealthOverlaySystem>();
        system.Enabled = !system.Enabled;

        shell.WriteLine(LocalizationManager.GetString($"cmd-{Command}-notify", ("state", system.Enabled ? "enabled" : "disabled")));
    }
}
