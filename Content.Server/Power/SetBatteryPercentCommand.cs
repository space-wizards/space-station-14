using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Console;

namespace Content.Server.Power
{
    [AdminCommand(AdminFlags.Debug)]
    public sealed class SetBatteryPercentCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly SharedBatterySystem _batterySystem = default!;

        public override string Command => "setbatterypercent";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine(Loc.GetString($"shell-wrong-arguments-number-need-specific",
                    ("properAmount", 2),
                    ("currentAmount", args.Length)));
                return;
            }

            if (!NetEntity.TryParse(args[0], out var netEnt) || !EntityManager.TryGetEntity(netEnt, out var id))
            {
                shell.WriteLine(Loc.GetString($"shell-invalid-entity-uid", ("uid", args[0])));
                return;
            }

            if (!float.TryParse(args[1], out var percent))
            {
                shell.WriteLine(Loc.GetString($"cmd-setbatterypercent-not-valid-percent", ("arg", args[1])));
                return;
            }

            if (EntityManager.TryGetComponent<BatteryComponent>(id, out var battery))
                _batterySystem.SetCharge((id.Value, battery), battery.MaxCharge * percent / 100);
            else
            {
                shell.WriteLine(Loc.GetString($"cmd-setbatterypercent-battery-not-found", ("id", id)));
                return;
            }
            // Don't acknowledge b/c people WILL forall this
        }
    }
}
