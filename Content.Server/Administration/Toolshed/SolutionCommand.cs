using System.Linq;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Server.Administration.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class SolutionCommand : ToolshedCommand
{
    private SolutionContainerSystem? _solutionContainer;

    [CommandImplementation("get")]
    public SolutionRef? Get(
            [CommandInvocationContext] IInvocationContext ctx,
            [PipedArgument] EntityUid input,
            [CommandArgument] ValueRef<string> name
        )
    {
        _solutionContainer ??= GetSys<SolutionContainerSystem>();

        _solutionContainer.TryGetSolution(input, name.Evaluate(ctx)!, out var solution);

        if (solution is not null)
            return new SolutionRef(input, solution);

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
}

public readonly record struct SolutionRef(EntityUid Owner, Solution Solution)
{
    public override string ToString()
    {
        return $"{Owner} {Solution}";
    }
}
