using Content.Server.Administration;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Power
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class SetBatteryPercentCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "setbatterypercent";
        public string Description => "Drains or recharges a battery by entity uid and percentage, i.e.: forall with Battery do setbatterypercent $ID 0";
        public string Help => $"{Command} <id> <percent>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine($"Invalid amount of arguments.\n{Help}");
                return;
            }

            if (!NetEntity.TryParse(args[0], out var netEnt) || !_entManager.TryGetEntity(netEnt, out var id))
            {
                shell.WriteLine($"{args[0]} is not a valid entity id.");
                return;
            }

            if (!float.TryParse(args[1], out var percent))
            {
                shell.WriteLine($"{args[1]} is not a valid float (percentage).");
                return;
            }

            if (!_entManager.TryGetComponent<BatteryComponent>(id, out var battery))
            {
                shell.WriteLine($"No battery found with id {id}.");
                return;
            }
            var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<BatterySystem>();
            system.SetCharge(id.Value, battery.MaxCharge * percent / 100, battery);
            // Don't acknowledge b/c people WILL forall this
        }
    }
}
