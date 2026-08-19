using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Animation;

/// <summary>
/// Makes a entity spawn visual effects (particles) on movement.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EffectGeneratorComponent : Component
{
    /// <summary>
    /// Prototype of the effect to be spawned
    /// </summary>
    [DataField]
    public EntProtoId EffectPrototype = "JetpackEffect";

    /// <summary>
    /// If it should add a random rotation to the effect
    /// </summary>
    [DataField]
    public bool RandomRotation;

    /// <summary>
    /// Cooldown time between spawning effects
    /// </summary>
    [DataField]
    public TimeSpan EffectCooldown = TimeSpan.FromSeconds(0.3);

    /// <summary>
    /// Next time to spawn a effect after the cooldown.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextEffectSpawnTime;

    /// <summary>
    /// Max distance between effects.
    ///
    /// If the entity gets too far from the last point where it spawned a effect.
    /// it will spawn a new effect entity regardless of the cooldown.
    /// </summary>
    [DataField]
    public float MaxDistance = 0.7f;

    /// <summary>
    /// Last point where the effect was spawned.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityCoordinates LastCoordinates;
}
