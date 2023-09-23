using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Stains;
using Robust.Shared.Prototypes;

namespace Content.Server.Stains;

public sealed class StainsSystem : SharedStainsSystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StainableComponent, TransferReagentEvent>(OnTransferReagent);
        SubscribeLocalEvent<StainableComponent, SolutionChangedEvent>(OnSolutionChanged);
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

    private void OnSolutionChanged(EntityUid uid, StainableComponent component, SolutionChangedEvent @event)
    {
        if (@event.Solution.Name == component.Solution)
        {
            var color = @event.Solution.GetColor(_prototype);
            var lambda = @event.Solution.FillFraction / 4;
            component.StainColor = Color.InterpolateBetween(Color.White, color, lambda);
            Dirty(uid, component);
        }
    }
}
