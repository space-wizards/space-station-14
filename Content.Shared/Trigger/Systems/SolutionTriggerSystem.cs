using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared.Trigger.Systems;

public sealed class SolutionTriggerSystem : XOnTriggerSystem<AddSolutionOnTriggerComponent>
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void OnTrigger(Entity<AddSolutionOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (!_solutionContainer.TryGetSolution(target, ent.Comp.Solution, out var solutionRef, out _))
            return;

        _solutionContainer.AddSolution(solutionRef.Value, ent.Comp.AddedSolution);

        args.Handled = true;
    }
}
