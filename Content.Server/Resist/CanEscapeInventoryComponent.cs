using Content.Shared.DoAfter;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Resist;

[RegisterComponent]
public sealed partial class CanEscapeInventoryComponent : Component
{
    /// <summary>
    /// Base doafter length for uncontested breakouts.
    /// </summary>
    [DataField("baseResistTime")]
    public float BaseResistTime = 5f;

    /// <summary>
    ///     Initial amount of time when you cannot be picked up when dropped while escaping
    /// </summary>
    [DataField("basePenaltyTime")]
    public float BasePenaltyTime = 1.0f;

    /// <summary>
    /// Penalty time for when you are let go during resisting.
    /// No one can pick you up before current time reaches this value.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan PenaltyTimer = TimeSpan.MaxValue;

    public bool IsEscaping => DoAfter != null;

    [DataField("doAfter")]
    public DoAfterId? DoAfter;
}
