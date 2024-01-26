using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Medical.Components;

[RegisterComponent]
[Access(typeof(HealthAnalyzerSystem))]
public sealed partial class ActiveHealthMonitoredComponent : Component
{
    /// <summary>
    /// Set of health analyzers currently monitoring this component's parent entity
    /// </summary>
    [ViewVariables]
    [DataField]
    public HashSet<EntityUid> ActiveAnalyzers = new();

    /// <summary>
    /// When should the next update be sent for this patient
    /// </summary>
    [ViewVariables]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// The delay between patient health updates
    /// </summary>
    [ViewVariables]
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

}
