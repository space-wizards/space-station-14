using Robust.Shared.Prototypes;

namespace Content.Shared.Effects.Components;

/// <summary>
/// Configures an effect emitted while this entity is moving and active.
/// </summary>
[RegisterComponent]
public sealed partial class ParticleEmitterComponent : Component
{
    [DataField(required: true)]
    public EntProtoId? EffectPrototype;

    /// <summary>
    /// Interval in seconds between spawned effects while moving.
    /// An effect may be spawned earlier when <see cref="MaxSpawnDistance"/> is reached.
    /// </summary>
    [DataField]
    public float SpawnInterval = 0.3f;

    /// <summary>
    /// Maximum distance between spawned effects.
    /// </summary>
    [DataField]
    public float MaxSpawnDistance = 0.7f;
}
