using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Anomaly.Components;

/// <summary>
/// Tracks anomalies going supercritical
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAnomalySystem))]
public sealed partial class AnomalySupercriticalComponent : Component
{
    /// <summary>
    /// The time when the supercritical animation ends and it does whatever effect.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndTime;

    /// <summary>
    /// The length of the animation before it goes supercritical.
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SupercriticalDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The maximum size the anomaly scales to while going supercritical
    /// </summary>
    [DataField]
    public float MaxScaleAmount = 3;
}
