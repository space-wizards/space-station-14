using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This component is used for paracusia, which causes auditory hallucinations.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedParacusiaSystem))]
public sealed partial class ParacusiaComponent : Component
{
    /// <summary>
    /// The maximum time between incidents in seconds
    /// </summary>
    [DataField("maxTimeBetweenIncidents", required: true), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float MaxTimeBetweenIncidents = 60f;

    /// <summary>
    /// The minimum time between incidents in seconds
    /// </summary>
    [DataField("minTimeBetweenIncidents", required: true), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float MinTimeBetweenIncidents = 30f;

    /// <summary>
    /// How far away at most can the sound be?
    /// </summary>
    [DataField("maxSoundDistance", required: true), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float MaxSoundDistance;

    /// <summary>
    /// The sounds to choose from
    /// </summary>
    [DataField("sounds", required: true)]
    [AutoNetworkedField]
    public SoundSpecifier Sounds = default!;

    [DataField("timeBetweenIncidents", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextIncidentTime;

    public EntityUid? Stream;
}
