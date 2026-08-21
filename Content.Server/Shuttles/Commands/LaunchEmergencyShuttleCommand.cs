using Content.Server.Administration;
using Content.Server.Shuttles.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Shuttles.Commands;

/// <summary>
/// Early launches in the emergency shuttle.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed partial class LaunchEmergencyShuttleCommand : LocalizedEntityCommands
{
    [Dependency] private EmergencyShuttleSystem _shuttleSystem = default!;

    public override string Command => "launchemergencyshuttle";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _shuttleSystem.EarlyLaunch();
    }
}
