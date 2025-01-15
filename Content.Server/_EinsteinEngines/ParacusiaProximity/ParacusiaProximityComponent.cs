using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Server._EinsteinEngines.ParacusiaProximity;

[RegisterComponent]
public sealed partial class ParacusiaProximityComponent : Component
{
    // Most of this is matched to the values in ParacusiaComponent

    /// <summary>
    /// The range at which paracusia is attempted to be given to mobs.
    /// </summary>
    [DataField("range")]
    public float Range;
    
    /// <summary>
    /// The maximum time between incidents in seconds.
    /// </summary>
    [DataField("maxTimeBetweenIncidents", required: true)]
    public float MaxTimeBetweenIncidents = 60f;

    /// <summary>
    /// The minimum time between incidents in seconds.
    /// </summary>
    [DataField("minTimeBetweenIncidents", required: true)]
    public float MinTimeBetweenIncidents = 30f;

    /// <summary>
    /// How far away at most can the sound be?
    /// </summary>
    [DataField("maxSoundDistance", required: true)]
    public float MaxSoundDistance;

    /// <summary>
    /// The sounds to choose from.
    /// </summary>
    [DataField("sounds", required: true)]
    public SoundSpecifier Sounds = default!;
}
