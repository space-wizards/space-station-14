using Content.Server.Anomaly.Effects;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// Shuffle Particle types after pulsation
/// </summary>
[RegisterComponent, Access(typeof(ShuffleParticlesAnomalySystem))]
public sealed partial class ShuffleParticlesAnomalyComponent : Component
{
    [DataField]
    public bool ShuffleOnPulse;

    [DataField]
    public bool ShuffleOnParticleHit;

    [DataField]
    public float Prob;
}
