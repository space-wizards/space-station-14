using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Robust.Shared.Console;
using System.Linq;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class SetSolutionCapacity : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public override string Command => "setsolutioncapacity";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 3)
            {
                shell.WriteLine(Loc.GetString("cmd-setsolutioncapacity-not-enough-args"));
                shell.WriteLine(Help);
                return;
            }

            if (!NetEntity.TryParse(args[0], out var uidNet))
            {
                shell.WriteLine(Loc.GetString("cmd-setsolutioncapacity-invalid-id"));
                return;
            }

            if (!_entManager.TryGetEntity(uidNet, out var uid) || !_entManager.TryGetComponent(uid, out SolutionContainerManagerComponent? man))
            {
                shell.WriteLine(Loc.GetString("cmd-setsolutioncapacity-no-solutions"));
                return;
            }

            var solutionContainerSystem = _entManager.System<SharedSolutionContainerSystem>();
            if (!solutionContainerSystem.TryGetSolution((uid.Value, man), args[1], out var solution))
            {
                var validSolutions = string.Join(", ", solutionContainerSystem.EnumerateSolutions((uid.Value, man)).Select(s => s.Name));
                shell.WriteLine(Loc.GetString("cmd-setsolutioncapacity-no-solution", ("solution", args[1])));
                shell.WriteLine(validSolutions);
                return;
            }

            if (!float.TryParse(args[2], out var quantityFloat))
            {
                shell.WriteLine(Loc.GetString("cmd-setsolutioncapacity-parse-error"));
                return;
            }

            if (quantityFloat < 0.0f)
            {
                shell.WriteLine(Loc.GetString("cmd-setsolutioncapacity-negative-value-error"));
                return;
            }

            var quantity = FixedPoint2.New(quantityFloat);
            solutionContainerSystem.SetCapacity(solution.Value, quantity);
        }
    }
}
