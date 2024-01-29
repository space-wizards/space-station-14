using Robust.Shared.Serialization;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// Shuffle Particle types after pulsation
/// </summary>
[RegisterComponent]
public sealed partial class ShuffleParticlesAnomalyComponent : Component
{
    [DataField]
    public bool ShuffleOnPulse;

    [DataField]
    public bool ShuffleOnParticleHit;

    [DataField]
    public float Prob;
}
