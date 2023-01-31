using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This component is used for paracusia, which causes auditory hallucinations.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class ParacusiaComponent : Component
{
    /// <summary>
    /// The random time between incidents, (min, max).
    /// </summary>
    [DataField("maxTimeBetweenIncidents", required: true)]
    public float MaxTimeBetweenIncidents { get; }

    [DataField("minTimeBetweenIncidents", required: true)]
    public float MinTimeBetweenIncidents { get; }

    /// <summary>
    /// How far away at most can the sound be?
    /// </summary>
    [DataField("maxSoundDistance", required: true)]
    public float MaxSoundDistance { get; }

    /// <summary>
    /// The sounds to choose from
    /// </summary
    [DataField("sounds", required: true)]
    public SoundSpecifier Sounds { get; } = default!;

    public float NextIncidentTime;
}

[Serializable, NetSerializable]
public sealed class ParacusiaComponentState : ComponentState
{
    public float MaxTimeBetweenIncidents { get; init; }
    public float MinTimeBetweenIncidents { get; init; }
    public float MaxSoundDistance { get; init; }
    public SoundSpecifier Sounds { get; init; } = default!;
}
