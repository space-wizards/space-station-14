using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Anomaly;

/// <summary>
/// Tracks anomalies going supercritical
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class AnomalySupercriticalComponent : Component
{
    /// <summary>
    /// The time when the supercritical animation ends and it does whatever effect.
    /// </summary>
    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndTime = TimeSpan.MaxValue;

    /// <summary>
    /// The length of the animation before it goes supercritical.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SupercriticalDuration = TimeSpan.FromSeconds(15);
}

[Serializable, NetSerializable]
public sealed class AnomalySupercriticalComponentState : ComponentState
{
    public TimeSpan EndTime;
}
