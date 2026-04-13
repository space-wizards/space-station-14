using Content.Shared.Singularity.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Singularity.Events;

/// <summary>
/// An event raised whenever the level of a singularity changes.
/// This usually causes significant changes to the radius/pull power/radioactivity of the singularity.
/// </summary>
/// <param name="Singularity">The singularity that has changed its level.</param>
/// <param name="OldValue">The previous level of the singularity, if any.</param>
[ByRefEvent]
public readonly record struct SingularityLevelChangedEvent(Entity<SingularityComponent> Singularity, byte? OldValue)
{
    /// <summary>
    /// A getter for the new level for the singularity.
    /// </summary>
    public readonly byte NewValue => Singularity.Comp.Level;

    /// <summary>
    /// Whether the level of the singularity has actually changed (true) or this is being called on startup (false).
    /// </summary>
    public readonly bool ValueChanged => OldValue is not null && NewValue != OldValue.Value;

    [Obsolete("This constructor is obsolete, use the Entity<T> overload")]
    public SingularityLevelChangedEvent(byte newValue, byte oldValue, SingularityComponent singularity) : this((singularity.Owner, singularity), oldValue)
    {
        DebugTools.Assert(newValue == NewValue, $"attempted to use the obsolete {nameof(SingularityLevelChangedEvent)} constructor with an incorrect new level");
    }
}
