using Content.Server.Administration;
using Content.Server.Power.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Power
{
    [AdminCommand(AdminFlags.Admin)]
    public class DrainAllBatteriesCommand : IConsoleCommand
    {
        public string Command => "drainallbatteries";
        public string Description => "Drains *all* batteries. Useful to make sure that an engine provides enough power to sustain the station.";
        public string Help => $"{Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 0)
            {
                shell.WriteLine($"Invalid amount of arguments: {args.Length}.\n{Help}");
                return;
            }

            var entMan = IoCManager.Resolve<IEntityManager>();
            foreach (var batteryComp in entMan.EntityQuery<BatteryComponent>())
            {
                batteryComp.CurrentCharge = 0;
            }

            shell.WriteLine("Done!");
        }
    }
}
