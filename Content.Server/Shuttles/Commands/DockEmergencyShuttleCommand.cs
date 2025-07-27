using Content.Server.Administration;
using Content.Server.Shuttles.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Shuttles.Commands;

/// <summary>
/// Calls in the emergency shuttle.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class DockEmergencyShuttleCommand : LocalizedEntityCommands
{
    [Dependency] private readonly EmergencyShuttleSystem _shuttleSystem = default!;

    public override string Command => "dockemergencyshuttle";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _shuttleSystem.DockEmergencyShuttle();
    }
}
