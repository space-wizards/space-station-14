using Content.Server.Administration;
using Content.Server.Shuttles.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Shuttles.Commands;

/// <summary>
/// Delays the round from ending via the shuttle call. Can still be ended via other means.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class DelayRoundEndCommand : IConsoleCommand
{
    public string Command => "delayroundend";
    public string Description => "Stops the timer that ends the round when the emergency shuttle exits hyperspace.";
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ShuttleSystem>();
        if (system.DelayEmergencyRoundEnd())
        {
            shell.WriteLine("Round delayed");
        }
        else
        {
            shell.WriteLine("Unable to delay round end.");
        }
    }
}
