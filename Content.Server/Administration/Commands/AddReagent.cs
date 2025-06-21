using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Administration.Commands;

/// <summary>
///     Command that allows you to edit an existing solution by adding (or removing) reagents.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class AddReagent : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _protomanager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override string Command => "addreagent";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 4)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("$properAmount", 4),
                ("currentAmount", args.Length)));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var uidNet) || !EntityManager.TryGetEntity(uidNet, out var uid))
        {
            shell.WriteLine(Loc.GetString($"shell-invalid-entity-id"));
            return;
        }

        if (!EntityManager.TryGetComponent(uid, out SolutionContainerManagerComponent? man))
        {
            shell.WriteError(Loc.GetString($"shell-entity-with-uid-lacks-component",
                ("uid", args[0]),
                ("componentName", nameof(SolutionContainerManagerComponent))));
            return;
        }

        if (!_solutionContainerSystem.TryGetSolution((uid.Value, man), args[1], out var solution))
        {
            var validSolutions = string.Join(", ", _solutionContainerSystem.EnumerateSolutions((uid.Value, man)).Select(s => s.Name));
            shell.WriteLine(Loc.GetString($"cmd-add-reagent-no-valid-solution", ("solution", args[1]), ("validSolutions", validSolutions)));
            return;
        }

        if (!_protomanager.HasIndex<ReagentPrototype>(args[2]))
        {
            shell.WriteLine(Loc.GetString($"cmd-addreagent-unknown-reagent"));
            return;
        }

        if (!float.TryParse(args[3], out var quantityFloat))
        {
            shell.WriteLine(Loc.GetString($"shell-argument-float-invalid", ("index", args[3])));
            return;
        }

        var quantity = FixedPoint2.New(MathF.Abs(quantityFloat));

        if (quantityFloat > 0)
            _solutionContainerSystem.TryAddReagent(solution.Value, args[2], quantity, out _);
        else
            _solutionContainerSystem.RemoveReagent(solution.Value, args[2], quantity);
    }
}
