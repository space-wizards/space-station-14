using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Stains;

public sealed class StainsSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StainableComponent, TransferReagentEvent>(OnTransferReagent);
    }

    private void OnTransferReagent(EntityUid uid, StainableComponent component, ref TransferReagentEvent @event)
    {
        if (@event.Method != ReactionMethod.Touch)
        {
            return;
        }
        if (!_solutionContainer.TryGetSolution(uid, component.Solution, out var solution))
        {
            return;
        }
        _solutionContainer.TryAddReagent(uid, solution, @event.ReagentQuantity, out _);
    }
}
