using Content.Shared.Singularity.Components;

namespace Content.Shared.Singularity.Events;

/// <summary>
/// An event raised whenever a singularity changes its level.
/// </summary>
public sealed class SingularityLevelChangedEvent : EntityEventArgs
{
    /// <summary>
    /// The new level of the singularity.
    /// </summary>
    public readonly byte NewValue;

    /// <summary>
    /// The previous level of the singularity.
    /// </summary>
    public readonly byte OldValue;

    /// <summary>
    /// The singularity that just changed level.
    /// </summary>
    public readonly SingularityComponent Singularity;

    public SingularityLevelChangedEvent(byte newValue, byte oldValue, SingularityComponent singularity)
    {
        NewValue = newValue;
        OldValue = oldValue;
        Singularity = singularity;
    }
}
