using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Anomaly.Components;

[RegisterComponent]
public sealed class ProjectileAnomalyComponent : Component
{
    /// <sumarry>
    /// The prototype of the projectile that will be shot when the anomaly pulses
    /// </summary>
    [DataField("projectilePrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ProjectilePrototype = default!;

    /// <summary>
    /// The MAXIMUM speed <see cref="ProjectilePrototype"/> can travel
    /// </summary>
    [DataField("maxProjectileSpeed")]
    public float MaxProjectileSpeed = 30f;

    /// <summary>
    /// The MAXIMUM number of projectiles shot per pulse
    /// </summary>
    [DataField("maxProjectiles")]
    public int MaxProjectiles = 5;

    /// <summary>
    /// The MAXIMUM range for targeting entities
    /// </summary>
    [DataField("projectileRange")]
    public float ProjectileRange = 50f;

    /// <summary>
    /// Chance that a non sentient entity will be targeted, value must be between 0.0-1.0
    /// </summary>
    [DataField("targetNonSentientChance")]
    public float TargetNonSentientChance = 0.5f;
}
