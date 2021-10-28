using Content.Server.Administration;
using Content.Server.Power.Components;
using Content.Server.Items;
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
        public string Description => "Drains *all non-item batteries*. Useful FOR DEBUGGING to make sure that an engine provides enough power to sustain the station.";
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
                // WORKAROUND FOR ADMEMES USING THIS AS AN EVENT
                if (batteryComp.Owner.HasComponent<ItemComponent>()) continue;
                batteryComp.CurrentCharge = 0;
            }

            shell.WriteLine("Done!");
        }
    }

    [AdminCommand(AdminFlags.Admin)]
    public class DrainBatteryCommand : IConsoleCommand
    {
        public string Command => "drainbattery";
        public string Description => "Drains a battery by entity uid.";
        public string Help => $"{Command} <id>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine($"Invalid amount of arguments.\n{Help}");
                return;
            }

            if (!EntityUid.TryParse(args[0], out var id))
            {
                shell.WriteLine($"{args[0]} is not a valid entity id.");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetComponent<BatteryComponent>(id, out var battery))
            {
                shell.WriteLine($"No battery found with id {id}.");
                return;
            }
            battery.CurrentCharge = 0;
            shell.WriteLine($"Drained battery with id {id}.");
        }
    }
}
