using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Containers;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared.Trigger.Systems;

public sealed class AddReagentOnTriggerSystem : EntitySystem
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

        if (ent.Comp.Generated == null)
            return;

        if (!_solutionContainer.ResolveSolution(target.Value, ent.Comp.SolutionName, ref ent.Comp.SolutionRef, out var solution))
            return;

        if (ent.Comp.SolutionRef == null)
            return;

        foreach (var reagent in ent.Comp.Generated.Contents)
        {
            _solutionContainer.TryAddReagent(ent.Comp.SolutionRef.Value, reagent.Reagent.Prototype, reagent.Quantity, out _);
        }
    }
}
