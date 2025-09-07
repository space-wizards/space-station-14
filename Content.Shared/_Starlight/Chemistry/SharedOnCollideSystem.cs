using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared._Starlight.Chemistry.Components;
using Robust.Shared.Physics.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared._Starlight.Chemistry;

public sealed class SharedOnCollideSystem : EntitySystem
{
    [Dependency] protected readonly ReactiveSystem _reactiveSystem = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem _solutionContainers = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<InjectOnCollideComponent, StartCollideEvent>(OnCollide);
        base.Initialize();
    }

    private void OnCollide(EntityUid uid, InjectOnCollideComponent component, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;
        if (_solutionContainers.TryGetInjectableSolution(target, out var targetSoln, out _))
        {
            var solution = new Solution(component.Reagents);

            foreach (var reagent in component.Reagents)
                if (component.ReagentLimit != null && _solutionContainers.GetTotalPrototypeQuantity(target, reagent.Reagent.ToString()) >= FixedPoint2.New(component.ReagentLimit.Value))
                    return;

            _reactiveSystem.DoEntityReaction(target, solution, ReactionMethod.Injection);
            _solutionContainers.TryAddSolution(targetSoln.Value, solution);
        }
    }
}