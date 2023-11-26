using Content.Shared.Chemistry.Solutions;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Containers.Events;

/// <summary>
/// This event alerts system that the solution was changed
/// </summary>
public sealed class SolutionChangedEvent : EntityEventArgs
{
    public readonly Solution Solution;
    public readonly string SolutionId;

    public SolutionChangedEvent(Solution solution, string solutionId)
    {
        SolutionId = solutionId;
        Solution = solution;
    }
}

/// <summary>
/// An event raised when more reagents are added to a (managed) solution than it can hold.
/// </summary>
[ByRefEvent]
public record struct SolutionOverflowEvent(EntityUid SolutionEnt, Solution SolutionHolder, Solution Overflow)
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