using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// This event alerts system that the solution was changed
/// </summary>
public sealed class SolutionContainerChangedEvent : EntityEventArgs
{
    public readonly Solution Solution;
    public readonly string SolutionId;

    public SolutionContainerChangedEvent(Solution solution, string solutionId)
    {
        SolutionId = solutionId;
        Solution = solution;
    }
}

/// <summary>
/// An event raised when more reagents are added to a (managed) solution than it can hold.
/// </summary>
[ByRefEvent]
public record struct SolutionContainerOverflowEvent(EntityUid SolutionEnt, Solution SolutionHolder, Solution Overflow)
{
    /// <summary>The entity which contains the solution that has overflowed.</summary>
    public readonly EntityUid SolutionEnt = SolutionEnt;
    /// <summary>The solution that has overflowed.</summary>
    public readonly Solution SolutionHolder = SolutionHolder;
    /// <summary>The reagents that have overflowed the solution.</summary>
    public readonly Solution Overflow = Overflow;
    /// <summary>The volume by which the solution has overflowed.</summary>
    public readonly FixedPoint2 OverflowVol = Overflow.Volume;
    /// <summary>Whether some subscriber has taken care of the effects of the overflow.</summary>
    public bool Handled = false;
}

public abstract partial class SharedSolutionContainerSystem
{
    protected void InitializeRelays()
    {
        SubscribeLocalEvent<SolutionContainerComponent, SolutionChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<SolutionContainerComponent, SolutionOverflowEvent>(OnSolutionOverflow);
    }


    protected virtual void OnSolutionChanged(EntityUid uid, SolutionContainerComponent comp, ref SolutionChangedEvent args)
    {
        var (solutionId, solutionComp) = args.Solution;
        var solution = solutionComp.Solution;

        UpdateAppearance(comp.Container, (solutionId, solutionComp, comp));
        RaiseLocalEvent(comp.Container, new SolutionContainerChangedEvent(solution, comp.Name));
    }

    protected virtual void OnSolutionOverflow(EntityUid uid, SolutionContainerComponent comp, ref SolutionOverflowEvent args)
    {
        var solution = args.Solution.Comp.Solution;
        var overflow = solution.SplitSolution(args.Overflow);
        var relayEv = new SolutionContainerOverflowEvent(uid, solution, overflow)
        {
            Handled = args.Handled,
        };

        RaiseLocalEvent(uid, ref relayEv);
        args.Handled = relayEv.Handled;
    }
}
