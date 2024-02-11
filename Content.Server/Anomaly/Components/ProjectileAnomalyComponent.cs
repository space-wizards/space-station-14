using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Anomaly.Components;

[RegisterComponent]
public sealed partial class ProjectileAnomalyComponent : Component
{
    /// <summary>
    /// The prototype of the projectile that will be shot when the anomaly pulses
    /// </summary>
    [DataField("projectilePrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string ProjectilePrototype = default!;

    /// <summary>
    /// The speed <see cref="ProjectilePrototype"/> can travel
    /// </summary>
    [DataField("projectileSpeed"), ViewVariables(VVAccess.ReadWrite)]
    public float ProjectileSpeed = 30f;

    /// <summary>
    /// The minimum number of projectiles shot per pulse
    /// </summary>
    [DataField("minProjectiles"), ViewVariables(VVAccess.ReadWrite)]
    public int MinProjectiles = 2;

    /// <summary>
    /// The MAXIMUM number of projectiles shot per pulse
    /// </summary>
    [DataField("maxProjectiles"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxProjectiles = 9;

    /// <summary>
    /// The MAXIMUM range for targeting entities
    /// </summary>
    [DataField("projectileRange"), ViewVariables(VVAccess.ReadWrite)]
    public float ProjectileRange = 50f;
}
