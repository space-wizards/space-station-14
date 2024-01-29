using Content.Shared.Anomaly;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// Anomaly Vessels can have an anomaly "stored" in them
/// by interacting on them with an anomaly scanner. Then,
/// they generate points for the selected server based on
/// the anomaly's stability and severity.
/// </summary>
[RegisterComponent, Access(typeof(SharedAnomalySystem))]
public sealed partial class AnomalyVesselComponent : Component
{
    /// <summary>
    /// The anomaly that the vessel is storing.
    /// Can be null.
    /// </summary>
    [ViewVariables]
    public EntityUid? Anomaly;

    /// <summary>
    /// A multiplier applied to the amount of points generated.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float PointMultiplier = 1;

    /// <summary>
    /// The maximum time between each beep
    /// </summary>
    [DataField("maxBeepInterval")]
    public TimeSpan MaxBeepInterval = TimeSpan.FromSeconds(2f);

    /// <summary>
    /// The minimum time between each beep
    /// </summary>
    [DataField("minBeepInterval")]
    public TimeSpan MinBeepInterval = TimeSpan.FromSeconds(0.75f);

    /// <summary>
    /// When the next beep sound will play
    /// </summary>
    [DataField("nextBeep", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextBeep = TimeSpan.Zero;

    /// <summary>
    /// The sound that is played repeatedly when the anomaly is destabilizing/decaying
    /// </summary>
    [DataField("beepSound")]
    public SoundSpecifier BeepSound = new SoundPathSpecifier("/Audio/Machines/vessel_warning.ogg");
}
