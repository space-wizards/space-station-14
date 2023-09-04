using Content.Server.Administration;
using Content.Server.Shuttles.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Shuttles.Commands;

/// <summary>
/// Delays the round from ending via the shuttle call. Can still be ended via other means.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
[InjectDependencies]
public sealed partial class DelayRoundEndCommand : IConsoleCommand
{
    [Dependency] private IEntitySystemManager _sysManager = default!;

    public string Command => "delayroundend";
    public string Description => Loc.GetString("emergency-shuttle-command-round-desc");
    public string Help => $"{Command}";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = _sysManager.GetEntitySystem<EmergencyShuttleSystem>();

        if (system.DelayEmergencyRoundEnd())
        {
            shell.WriteLine(Loc.GetString("emergency-shuttle-command-round-yes"));
        }
        else
        {
            shell.WriteLine(Loc.GetString("emergency-shuttle-command-round-no"));
        }
    }
}
