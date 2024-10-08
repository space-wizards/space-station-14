using Content.Server.Administration;
using Content.Server.Shuttles.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Shuttles.Commands;

/// <summary>
/// Calls in the emergency shuttle.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class DockEmergencyShuttleCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _sysManager = default!;

    public string Command => "dockemergencyshuttle";
    public string Description => Loc.GetString("emergency-shuttle-command-dock-desc");
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = _sysManager.GetEntitySystem<EmergencyShuttleSystem>();
        system.DockEmergencyShuttle();
    }
}
