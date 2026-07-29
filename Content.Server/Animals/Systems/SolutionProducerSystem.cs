using Content.Server.Animals.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;

namespace Content.Server.Animals.Systems;

/// <inheritdoc cref="SolutionProducerComponent"/>
public sealed partial class SolutionProducerSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    [SubscribeLocalEvent]
    private void OnProduce(Entity<SolutionProducerComponent> ent, ref HungerProductionEvent args)
    {
        if (!TryComp(ent, out SolutionComponent? solution))
            return;

        var amount = FixedPoint2.Min(solution.Solution.AvailableVolume, ent.Comp.Generated.Volume);
        if (amount <= FixedPoint2.Zero)
            return;

        var generated = amount == ent.Comp.Generated.Volume
            ? ent.Comp.Generated
            : ent.Comp.Generated.Clone().SplitSolution(amount);

        if (!_solutionContainer.TryAddSolution((ent.Owner, solution), generated))
            return;

        args.Produced = true;
    }
}
