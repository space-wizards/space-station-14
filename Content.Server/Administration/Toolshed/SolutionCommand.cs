using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class SolutionCommand : ToolshedCommand
{
    private SharedSolutionContainerSystem? _solutionContainer;

    [CommandImplementation("get")]
    public SolutionRef? Get([PipedArgument] EntityUid input, string name)
    {
        _solutionContainer ??= GetSys<SharedSolutionContainerSystem>();

        if (_solutionContainer.TryGetSolution(input, name, out var solution))
            return new SolutionRef(solution.Value);

        return null;
    }

    [CommandImplementation("get")]
    public IEnumerable<SolutionRef> Get([PipedArgument] IEnumerable<EntityUid> input, string name)
    {
        return input.Select(x => Get(x, name)).Where(x => x is not null).Cast<SolutionRef>();
    }

    [CommandImplementation("adjreagent")]
    public SolutionRef AdjReagent(
            [PipedArgument] SolutionRef input,
            ProtoId<ReagentPrototype> proto,
            float amount
        )
    {
        _solutionContainer ??= GetSys<SharedSolutionContainerSystem>();

        // Convert float to FixedPoint2
        var amountFixed = FixedPoint2.New(amount);

        if (amountFixed > 0)
        {
            _solutionContainer.TryAddReagent(input.Solution, proto, amountFixed, out _);
        }
        else if (amountFixed < 0)
        {
            _solutionContainer.RemoveReagent(input.Solution, proto, -amountFixed);
        }

        return input;
    }

    [CommandImplementation("adjreagent")]
    public IEnumerable<SolutionRef> AdjReagent(
            [PipedArgument] IEnumerable<SolutionRef> input,
            ProtoId<ReagentPrototype> name,
            float amount
        )
        => input.Select(x => AdjReagent(x, name, amount));
}

public readonly record struct SolutionRef(Entity<SolutionComponent> Solution)
{
    public override string ToString()
    {
        return $"{Solution.Owner} {Solution.Comp.Solution}";
    }
}
