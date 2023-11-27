using Content.Shared.Chemistry.Containers.Components;
using Content.Shared.Chemistry.Solutions.Events;
using ContainerChangedEvent = Content.Shared.Chemistry.Containers.Events.SolutionChangedEvent;
using ContainerOverflowEvent = Content.Shared.Chemistry.Containers.Events.SolutionOverflowEvent;

namespace Content.Shared.Chemistry.Containers.EntitySystems;

public abstract partial class SharedSolutionContainerSystem
{
    protected void InitializeRelays()
    {
        SubscribeLocalEvent<ContainerSolutionComponent, SolutionChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<ContainerSolutionComponent, SolutionOverflowEvent>(OnSolutionOverflow);
    }


    protected virtual void OnSolutionChanged(EntityUid uid, ContainerSolutionComponent comp, ref SolutionChangedEvent args)
    {
        var (solutionId, solutionComp) = args.Solution;
        var solution = solutionComp.Solution;

        UpdateAppearance(comp.Container, (solutionId, solutionComp, comp));
        RaiseLocalEvent(comp.Container, new ContainerChangedEvent(solution, comp.Name));
    }

    protected virtual void OnSolutionOverflow(EntityUid uid, ContainerSolutionComponent comp, ref SolutionOverflowEvent args)
    {
        var solution = args.Solution.Comp.Solution;
        var overflow = solution.SplitSolution(args.Overflow);
        var relayEv = new ContainerOverflowEvent(uid, solution, overflow)
        {
            Handled = args.Handled,
        };

        RaiseLocalEvent(uid, ref relayEv);
        args.Handled = relayEv.Handled;
    }
}
