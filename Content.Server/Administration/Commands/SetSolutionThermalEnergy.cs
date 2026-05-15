using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components.SolutionManager;
using Robust.Shared.Console;
using System.Linq;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class SetSolutionThermalEnergy : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public override string Command => "setsolutionthermalenergy";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3)
            {
                shell.WriteLine(Loc.GetString("cmd-setsolutionthermalenergy-not-enough-args"));
                shell.WriteLine(Help);
                return;
            }

            if (!NetEntity.TryParse(args[0], out var uidNet) || !_entManager.TryGetEntity(uidNet, out var uid))
            {
                shell.WriteLine(Loc.GetString("cmd-setsolutionthermalenergy-invalid-id"));
                return;
            }

            var solutionContainerSystem = _entManager.System<SharedSolutionContainerSystem>();
            if (!solutionContainerSystem.TryGetSolution(uid.Value, args[1], out var solutionEnt, out var solution))
            {
                var solutions = solutionContainerSystem.EnumerateSolutions(uid.Value).ToArray();
                if (!solutions.Any())
                {
                    shell.WriteLine(Loc.GetString("cmd-setsolutionthermalenergy-no-solutions"));
                    return;
                }

                var validSolutions = string.Join(", ", solutions.Select(s => s.Name));
                shell.WriteLine(Loc.GetString("cmd-setsolutionthermalenergy-no-solution", ("solution", args[1])));
                return;
            }

            if (!float.TryParse(args[2], out var quantity))
            {
                shell.WriteLine(Loc.GetString("cmd-setsolutionthermalenergy-parse-error"));
                return;
            }

            if (solution.GetHeatCapacity(null) <= 0.0f)
            {
                if (quantity != 0.0f)
                {
                    shell.WriteLine(Loc.GetString("cmd-setsolutionthermalenergy-negative-value-error"));
                    return;
                }
            }
            else if (quantity <= 0.0f)
            {
                shell.WriteLine(Loc.GetString("cmd-setsolutionthermalenergy-negative-thermal-error"));
                return;
            }

            solutionContainerSystem.SetThermalEnergy(solutionEnt.Value, quantity);
        }
    }
}
