using Content.Shared.Chemistry.Solutions.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Solutions.Events;

/// <summary>
/// The event raised whenever a solution entity is modified.
/// </summary>
/// <remarks>
/// Raised after chemcial reactions and <see cref="SolutionOverflowEvent"/> are handled.
/// </remarks>
/// <param name="Solution">The solution entity that has been modified.</param>
[ByRefEvent]
public readonly partial record struct SolutionChangedEvent(Entity<SolutionComponent> Solution);

/// <summary>
/// The event raised whenever a solution entity is filled past its capacity.
/// </summary>
/// <param name="Solution">The solution entity that has been overfilled.</param>
/// <param name="Overflow">The amount by which the solution entity has been overfilled.</param>
[ByRefEvent]
public partial record struct SolutionOverflowEvent(Entity<SolutionComponent> Solution, FixedPoint2 Overflow)
{
    /// <summary>The solution entity that has been overfilled.</summary>
    public readonly Entity<SolutionComponent> Solution = Solution;
    /// <summary>The amount by which the solution entity has been overfilled.</summary>
    public readonly FixedPoint2 Overflow = Overflow;
    /// <summary>Whether any of the event handlers for this event have handled overflow behaviour.</summary>
    public bool Handled = false;
}
