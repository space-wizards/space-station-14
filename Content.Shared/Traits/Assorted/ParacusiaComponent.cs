using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This component is used for paracusia, which causes auditory hallucinations.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[AutoGenerateComponentPause]
[Access(typeof(SharedParacusiaSystem))]
public sealed partial class ParacusiaComponent : Component
{
    /// <summary>
    /// How far away at most can the sound be?
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float MaxSoundDistance;

    /// <summary>
    /// The maximum time between incidents in seconds
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float MaxTimeBetweenIncidents = 60f;

    /// <summary>
    /// The minimum time between incidents in seconds
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float MinTimeBetweenIncidents = 30f;

    [AutoPausedField]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextIncidentTime;

    /// <summary>
    /// The sounds to choose from
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public SoundSpecifier Sounds = default!;

    public EntityUid? Stream;
}
