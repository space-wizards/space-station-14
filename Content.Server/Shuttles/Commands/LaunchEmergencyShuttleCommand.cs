using Content.Server.Administration;
using Content.Server.Shuttles.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Shuttles.Commands;

/// <summary>
/// Early launches in the emergency shuttle.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class LaunchEmergencyShuttleCommand : IConsoleCommand
{
    public string Command => "launchemergencyshuttle";
    public string Description => Loc.GetString("emergency-shuttle-command-launch-desc");
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ShuttleSystem>();
        system.EarlyLaunch();
    }
}
