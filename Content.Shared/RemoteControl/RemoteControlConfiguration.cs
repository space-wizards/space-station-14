using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.RemoteControl;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RemoteControlConfiguration
{
    // Break the chains
    /// <summary>
    /// Whether or not the user should be notified when their body is interacted with.
    /// </summary>
    [DataField]
    public bool NotifyOnInteract = true;

    /// <summary>
    /// Whether remote control should end when the remote is dropped.
    /// </summary>
    [DataField]
    public bool BreakOnDropController = true;

    /// <summary>
    /// Whether the user will get notified when their body takes damage.
    /// </summary>
    [DataField]
    public bool NotifyOnDamaged = true;

    /// <summary>
    /// Whether the remote control will end on the user taking damage.
    /// </summary>
    [DataField]
    public bool BreakOnDamaged = true;

    /// <summary>
    /// Threshold for user damage. This damage has to be dealt in a single event, not over time.
    /// Used for NotifyOnDamaged.
    /// </summary>
    [DataField]
    public FixedPoint2 NotifyDamageThreshold = 1;

    /// <summary>
    /// Threshold for user damage. This damage has to be dealt in a single event, not over time.
    /// Used for BreakOnDamaged.
    /// </summary>
    [DataField]
    public FixedPoint2 BreakDamageThreshold = 5;
}
