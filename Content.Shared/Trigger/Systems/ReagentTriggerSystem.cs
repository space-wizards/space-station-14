using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Containers;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared.Trigger.Systems;

public sealed class ReagentTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddReagentOnTriggerComponent, TriggerEvent>(OnTriggered);
    }

    private void OnTriggered(Entity<AddReagentOnTriggerComponent> ent, ref TriggerEvent args)
    {

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!_solutionContainer.TryGetSolution(target.Value, ent.Comp.SolutionId, out var solutionRef, out _))
            return;

        _solutionContainer.TryAddSolution(solutionRef.Value, ent.Comp.AddedSolution);
    }
}
