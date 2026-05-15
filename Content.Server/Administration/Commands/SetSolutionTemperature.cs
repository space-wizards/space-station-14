using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components.SolutionManager;
using Robust.Shared.Console;
using System.Linq;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class SetSolutionTemperature : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public override string Command => "setsolutiontemperature";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3)
            {
                shell.WriteLine(Loc.GetString("cmd-setsolutiontemperature-not-enough-args"));
                shell.WriteLine(Help);
                return;
            }

            if (!NetEntity.TryParse(args[0], out var uidNet) || !_entManager.TryGetEntity(uidNet, out var uid))
            {
                shell.WriteLine(Loc.GetString("cmd-setsolutiontemperature-invalid-id"));
                return;
            }

            var solutionContainerSystem = _entManager.System<SharedSolutionContainerSystem>();
            if (!solutionContainerSystem.TryGetSolution(uid.Value, args[1], out var solution))
            {
                var solutions = solutionContainerSystem.EnumerateSolutions(uid.Value).ToArray();
                if (!solutions.Any())
                {
                    shell.WriteLine(Loc.GetString("cmd-setsolutiontemperature-no-solutions"));
                    return;
                }

                var validSolutions = string.Join(", ", solutions.Select(s => s.Name));
                shell.WriteLine(Loc.GetString("cmd-setsolutiontemperature-no-solution", ("solution", args[1])));
                return;
            }

            if (!float.TryParse(args[2], out var quantity))
            {
                shell.WriteLine(Loc.GetString("cmd-setsolutiontemperature-parse-error"));
                return;
            }

            if (quantity <= 0.0f)
            {
                shell.WriteLine(Loc.GetString("cmd-setsolutiontemperature-negative-value-error"));
                return;
            }

            solutionContainerSystem.SetTemperature(solution.Value, quantity);
        }
    }
}
