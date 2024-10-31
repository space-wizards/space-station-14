using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed class ReactiveContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReactiveContainerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ReactiveContainerComponent, SolutionContainerChangedEvent>(OnSolutionChange);
    }

    private void OnInserted(EntityUid uid, ReactiveContainerComponent comp, EntInsertedIntoContainerMessage args)
    {
        // Only reactive entities can react with the solution
        if (!HasComp<ReactiveComponent>(args.Entity))
            return;

        if (!_solutionContainerSystem.TryGetSolution(uid, comp.Solution, out _, out var solution))
            return;
        if (solution.Volume == 0)
            return;

        _reactiveSystem.DoEntityReaction(args.Entity, solution, ReactionMethod.Touch);
    }

    private void OnSolutionChange(EntityUid uid, ReactiveContainerComponent comp, SolutionContainerChangedEvent args)
    {
        if (!_solutionContainerSystem.TryGetSolution(uid, comp.Solution, out _, out var solution))
            return;
        if (solution.Volume == 0)
            return;

        var manager = EnsureComp<ContainerManagerComponent>(uid);
        var container = _containerSystem.EnsureContainer<ContainerSlot>(uid, comp.Container, manager);
        foreach (var entity in container.ContainedEntities)
        {
            if (!HasComp<ReactiveComponent>(entity))
                continue;
            _reactiveSystem.DoEntityReaction(entity, solution, ReactionMethod.Touch);
        }
    }
}
