using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using System;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This component is used for paracusia, which causes auditory hallucinations.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedParacusiaSystem))]
public sealed class ParacusiaComponent : Component
{
    /// <summary>
    /// The maximum time between incidents in seconds
    /// </summary>
    [DataField("maxTimeBetweenIncidents", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float MaxTimeBetweenIncidents = 30f;

    /// <summary>
    /// The minimum time between incidents in seconds
    /// </summary>
    [DataField("minTimeBetweenIncidents", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float MinTimeBetweenIncidents = 60f;

    /// <summary>
    /// How far away at most can the sound be?
    /// </summary>
    [DataField("maxSoundDistance", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float MaxSoundDistance;

    /// <summary>
    /// The sounds to choose from
    /// </summary>
    [DataField("sounds", required: true)]
    public SoundSpecifier Sounds = default!;

    [DataField("timeBetweenIncidents", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextIncidentTime;

    public IPlayingAudioStream? Stream;
}

[Serializable, NetSerializable]
public sealed class ParacusiaComponentState : ComponentState
{
    public readonly float MaxTimeBetweenIncidents;
    public readonly float MinTimeBetweenIncidents;
    public readonly float MaxSoundDistance;
    public readonly SoundSpecifier Sounds = default!;

    public ParacusiaComponentState(float maxTimeBetweenIncidents, float minTimeBetweenIncidents, float maxSoundDistance, SoundSpecifier sounds)
    {
        MaxTimeBetweenIncidents = maxTimeBetweenIncidents;
        MinTimeBetweenIncidents = minTimeBetweenIncidents;
        MaxSoundDistance = maxSoundDistance;
        Sounds = sounds;
    }
}
