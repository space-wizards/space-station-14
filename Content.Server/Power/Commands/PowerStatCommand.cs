using Content.Server.Administration;
using Content.Server.Power.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Power.Commands
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class PowerStatCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _e = default!;

        public string Command => "powerstat";
        public string Description => "Shows statistics for pow3r";
        public string Help => "Usage: powerstat";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var stats = _e.System<PowerNetSystem>().GetStatistics();

            shell.WriteLine($"networks: {stats.CountNetworks}");
            shell.WriteLine($"loads: {stats.CountLoads}");
            shell.WriteLine($"supplies: {stats.CountSupplies}");
            shell.WriteLine($"batteries: {stats.CountBatteries}");
        }
    }
}
