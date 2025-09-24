using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This is used for the narcolepsy trait.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
[Access(typeof(NarcolepsySystem))]
public sealed partial class NarcolepsyComponent : Component
{
    /// <summary>
    /// The maximum time between incidents.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public TimeSpan MaxTimeBetweenIncidents;

    /// <summary>
    /// The minimum time between incidents.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public TimeSpan MinTimeBetweenIncidents;

    /// <summary>
    /// The maximum duration of incidents.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public TimeSpan MaxDurationOfIncident;

    /// <summary>
    /// The minimum duration of incidents.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public TimeSpan MinDurationOfIncident;

    /// <summary>
    /// Next time indcident happens.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextIncidentTime = TimeSpan.Zero;
}
