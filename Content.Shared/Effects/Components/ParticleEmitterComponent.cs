using Robust.Shared.Prototypes;

namespace Content.Shared.Effects.Components;

[RegisterComponent]
public sealed partial class ParticleEmitterComponent : Component
{
    /// <summary>
    /// The effect that will be spawned.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId EffectPrototype;

    [DataField]
    public float SpawnInterval = 0.3f;

    /// <summary>
    /// Maximum desired distance between spawned effects.
    /// </summary>
    [DataField]
    public float MaxSpawnDistance = 0.7f;
}
