using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// Tracks anomalies going supercritical
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAnomalySystem))]
public sealed partial class AnomalySupercriticalComponent : Component
{
    /// <summary>
    /// The time when the supercritical animation ends and it does whatever effect.
    /// </summary>
    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndTime;

    /// <summary>
    /// The length of the animation before it goes supercritical.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SupercriticalDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The maximum size the anomaly scales to while going supercritical
    /// </summary>
    [DataField("maxScaleAmount")]
    public float MaxScaleAmount = 3;
}

[Serializable, NetSerializable]
public sealed class AnomalySupercriticalComponentState : ComponentState
{
    public TimeSpan EndTime;
    public TimeSpan Duration;
}
