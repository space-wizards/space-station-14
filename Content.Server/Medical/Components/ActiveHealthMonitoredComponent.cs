using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Medical.Components;

[RegisterComponent]
[Access(typeof(HealthAnalyzerSystem))]
public sealed partial class ActiveHealthMonitoredComponent : Component
{
    /// <summary>
    /// When should the next update be sent for this patient
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// The delay between patient health updates
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

}
