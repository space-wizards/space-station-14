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
    public sealed partial class AddReagent : LocalizedCommands
    {
        [Dependency] private IEntityManager _entManager = default!;
        [Dependency] private IPrototypeManager _protomanager = default!;
        [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

        public override string Command => "addreagent";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 4)
            {
                shell.WriteLine(Loc.GetString("shell-need-minimum-arguments", ("minimum", 4)));
                shell.WriteLine(Help);
                return;
            }

            if (!NetEntity.TryParse(args[0], out var uidNet) || !_entManager.TryGetEntity(uidNet, out var uid))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-uid", ("uid", args[0])));
                return;
            }

            if (!_solutionContainer.TryGetSolution(uid.Value, args[1], out var solution))
            {
                var solutions = _solutionContainer.EnumerateSolutions(uid.Value).ToArray();
                if (!solutions.Any())
                {
                    shell.WriteLine(Loc.GetString("cmd-addreagent-no-solutions"));
                    return;
                }

                var validSolutions = string.Join(", ", solutions.Select(s => s.Name));
                shell.WriteLine(Loc.GetString("cmd-addreagent-no-solution", ("solution", args[1]), ("validSolutions", validSolutions)));
                return;
            }

            if (!_protomanager.HasIndex<ReagentPrototype>(args[2]))
            {
                shell.WriteLine(Loc.GetString("cmd-addreagent-unknown-reagent"));
                return;
            }

            if (!float.TryParse(args[3], out var quantityFloat))
            {
                shell.WriteLine(Loc.GetString("cmd-addreagent-bad-quantity"));
                return;
            }
            var quantity = FixedPoint2.New(MathF.Abs(quantityFloat));

            if (quantityFloat > 0)
                _solutionContainer.TryAddReagent(solution.Value, args[2], quantity, out _);
            else
                _solutionContainer.RemoveReagent(solution.Value, args[2], quantity);
        }
    }
}
