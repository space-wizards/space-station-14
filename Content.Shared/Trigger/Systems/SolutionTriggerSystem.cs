using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Containers;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared.Trigger.Systems;

public sealed class SolutionTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddSolutionOnTriggerComponent, TriggerEvent>(OnTriggered);
    }

    private void OnTriggered(Entity<AddSolutionOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!_solutionContainer.TryGetSolution(target.Value, ent.Comp.Solution, out var solutionRef, out _))
            return;

        _solutionContainer.AddSolution(solutionRef.Value, ent.Comp.AddedSolution);

        args.Handled = true;
    }
}
