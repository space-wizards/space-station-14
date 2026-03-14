using Content.Server.Administration;
using Content.Server.Power.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Power.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class PowerStatCommand : LocalizedEntityCommands
{
    [Dependency] private readonly PowerNetSystem _powerNet = default!;

    public override string Command => "powerstat";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var stats = _powerNet.GetStatistics();
        shell.WriteLine(Loc.GetString("cmd-powerstat-output",
            ("networks", stats.CountNetworks),
            ("loads", stats.CountLoads),
            ("supplies", stats.CountSupplies),
            ("batteries", stats.CountBatteries)));
    }
}
