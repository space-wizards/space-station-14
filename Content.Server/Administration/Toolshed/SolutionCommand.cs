using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using System.Linq;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Systems;

namespace Content.Server.Administration.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class SolutionCommand : ToolshedCommand
{
    private SharedSolutionSystem? solutionSystem;

    [CommandImplementation("get")]
    public SolutionRef? Get(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] EntityUid input,
            [CommandArgument] ValueRef<string> name
        )
    {
        solutionSystem ??= GetSys<SharedSolutionSystem>();

        if (solutionSystem.TryGetSolution(input, name.Evaluate(ctx)!, out var solution))
            return new SolutionRef(solution);

        return null;
    }

    [CommandImplementation("get")]
    public IEnumerable<SolutionRef> Get(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] ValueRef<string> name
    )
    {
        return input.Select(x => Get(ctx, x, name)).Where(x => x is not null).Cast<SolutionRef>();
    }

    [CommandImplementation("adjreagent")]
    public SolutionRef AdjReagent(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] SolutionRef input,
            [CommandArgument] Prototype<ReagentPrototype> name,
            [CommandArgument] ValueRef<FixedPoint2> amountRef
        )
    {
        solutionSystem ??= GetSys<SharedSolutionSystem>();

        var amount = amountRef.Evaluate(ctx);
        if (amount > 0)
        {
            solutionSystem.AddReagent(input.Solution, (name.Value.ID, amount), out _);
        }
        else if (amount < 0)
        {
            solutionSystem.RemoveReagent(input.Solution, (name.Value.ID, -amount), out _);
        }

        return input;
    }

    [CommandImplementation("adjreagent")]
    public IEnumerable<SolutionRef> AdjReagent(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] IEnumerable<SolutionRef> input,
            [CommandArgument] Prototype<ReagentPrototype> name,
            [CommandArgument] ValueRef<FixedPoint2> amountRef
        )
        => input.Select(x => AdjReagent(ctx, x, name, amountRef));
}

public readonly record struct SolutionRef(Entity<SolutionComponent> Solution)
{
    public override string ToString()
    {
        return $"{Solution.Owner} {Solution.Comp.Contents}";
    }
}
