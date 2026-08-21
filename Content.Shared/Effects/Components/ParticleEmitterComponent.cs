using Robust.Shared.Prototypes;

namespace Content.Shared.Effects.Components;

/// <summary>
/// Configures a client-side effect emitted while this entity is moving and active.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleEmitterComponent : Component
{
    /// <summary>
    /// The effect that will be spawned.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId? EffectPrototype;

    /// <summary>
    /// Maximum interval in seconds between spawned effects while the emitter is moving.
    /// An effect may be spawned earlier if <see cref="MaxSpawnDistance"/> is reached.
    /// </summary>
    [DataField]
    public float SpawnInterval = 0.3f;

    /// <summary>
    /// Maximum desired distance between spawned effects.
    /// </summary>
    [DataField]
    public float MaxSpawnDistance = 0.7f;
}
