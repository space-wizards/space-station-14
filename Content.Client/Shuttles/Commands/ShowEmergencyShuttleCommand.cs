using Content.Client.Shuttles.Systems;
using Robust.Shared.Console;

namespace Content.Client.Shuttles.Commands;

public sealed class ShowEmergencyShuttleCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ShuttleSystem _shuttle = default!;

    public override string Command => "showemergencyshuttle";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _shuttle.EnableShuttlePosition ^= true;
        shell.WriteLine(Loc.GetString($"cmd-showemergencyshuttle-status", ("status", _shuttle.EnableShuttlePosition)));
    }
}
