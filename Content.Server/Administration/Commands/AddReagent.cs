using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Administration.Commands
{
    /// <summary>
    ///     Command that allows you to edit an existing solution by adding (or removing) reagents.
    /// </summary>
    [AdminCommand(AdminFlags.Admin)]
    public sealed class AddReagent : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IPrototypeManager _protomanager = default!;

        public override string Command => "addreagent";

        public override string Help => Loc.GetString($"cmd-{Command}-help", ("command", Command));

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 4)
            {
                shell.WriteLine(Loc.GetString($"cmd-{Command}-not-enough-args"));
                shell.WriteLine(Help);
                return;
            }

            if (!NetEntity.TryParse(args[0], out var uidNet) || !_entManager.TryGetEntity(uidNet, out var uid))
            {
                shell.WriteLine(Loc.GetString($"cmd-{Command}-invalid-id"));
                return;
            }

            if (!_entManager.TryGetComponent(uid, out SolutionContainerManagerComponent? man))
            {
                shell.WriteLine(Loc.GetString($"cmd-{Command}-no-solutions"));
                return;
            }

            var solutionContainerSystem = _entManager.System<SharedSolutionContainerSystem>();
            if (!solutionContainerSystem.TryGetSolution((uid.Value, man), args[1], out var solution))
            {
                var validSolutions = string.Join(", ", solutionContainerSystem.EnumerateSolutions((uid.Value, man)).Select(s => s.Name));
                shell.WriteLine(Loc.GetString($"cmd-{Command}-no-solution", ("solution", args[1]), ("validSolutions", validSolutions)));
                return;
            }

            if (!_protomanager.HasIndex<ReagentPrototype>(args[2]))
            {
                shell.WriteLine(Loc.GetString($"cmd-{Command}-unknown-reagent"));
                return;
            }

            if (!float.TryParse(args[3], out var quantityFloat))
            {
                shell.WriteLine(Loc.GetString($"cmd-{Command}-bad-quantity"));
                return;
            }
            var quantity = FixedPoint2.New(MathF.Abs(quantityFloat));

            if (quantityFloat > 0)
                solutionContainerSystem.TryAddReagent(solution.Value, args[2], quantity, out _);
            else
                solutionContainerSystem.RemoveReagent(solution.Value, args[2], quantity);
        }
    }
}
