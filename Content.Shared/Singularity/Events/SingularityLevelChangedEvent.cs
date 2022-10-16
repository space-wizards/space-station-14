using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;

namespace Content.Shared.Singularity.Events;

/// <summary>
/// An event raised whenever a singularity changes its level.
/// </summary>
public sealed class SingularityLevelChangedEvent : EntityEventArgs
{
    /// <summary>
    /// The new level of the singularity.
    /// </summary>
    public readonly ulong NewValue;

    /// <summary>
    /// The previous level of the singularity.
    /// </summary>
    public readonly ulong OldValue;

    /// <summary>
    /// The singularity that just changed level.
    /// </summary>
    public readonly SharedSingularityComponent Singularity;

    /// <summary>
    /// The system that managed the level change.
    /// </summary>
    public readonly SharedSingularitySystem SingularitySystem;

    public SingularityLevelChangedEvent(ulong newValue, ulong oldValue, SharedSingularityComponent singularity, SharedSingularitySystem singularitySystem)
    {
        NewValue = newValue;
        OldValue = oldValue;
        Singularity = singularity;
        SingularitySystem = singularitySystem;
    }
}
