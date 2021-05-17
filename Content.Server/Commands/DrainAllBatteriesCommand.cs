#nullable enable
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Power;
using Content.Shared.Administration;
using Content.Shared.GameObjects.Components;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Commands
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

            var entityManager = IoCManager.Resolve<IEntityManager>();
            foreach (var ent in entityManager.GetEntities(new TypeEntityQuery(typeof(BatteryComponent))))
            {
                ent.GetComponent<BatteryComponent>().CurrentCharge = 0;
            }
            shell.WriteLine("Done!");
        }
    }
}
