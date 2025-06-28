using Content.Server.Administration;
using Content.Server.Shuttles.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Shuttles.Commands;

/// <summary>
/// Delays the round from ending via the shuttle call. Can still be ended via other means.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class DelayRoundEndCommand : LocalizedEntityCommands
{
    [Dependency] private readonly EmergencyShuttleSystem _shuttleSystem = default!;

    public override string Command => "delayroundend";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_shuttleSystem.DelayEmergencyRoundEnd())
            shell.WriteLine(Loc.GetString("emergency-shuttle-command-round-yes"));
        else
            shell.WriteLine(Loc.GetString("emergency-shuttle-command-round-no"));
    }
}
