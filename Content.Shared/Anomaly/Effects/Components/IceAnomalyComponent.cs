using Content.Shared.Atmos;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Anomaly.Effects.Components;

[RegisterComponent]
public sealed class IceAnomalyComponent : Component
{
    /// <sumarry>
    /// The prototype of the projectile that will be shot when the anomaly pulses
    /// </summary>
    [DataField("projectilePrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ProjectilePrototype = default!;


    [DataField("projectileDamage", required: true)]
    public DamageSpecifier ProjectileDamage = default!;

    /// <summary>
    /// The MAXIMUM speed <see cref="ProjectilePrototype"/> can travel
    /// </summary>
    [DataField("maxprojectileSpeed")]
    public float MaxProjectileSpeed = 50f;

    /// <summary>
    /// The MAXIMUM range an entity has to be in to be shot at
    /// </summary>
    [DataField("projectileRange")]
    public float ProjectileRange = 50f;

    /// <summary>
    /// The MAXIMUM amount of chill released per second.
    /// This is scaled linearly with the Severity of the anomaly.
    /// </summary>
    [DataField("chillPerSecond")]
    public float HeatPerSecond = 25;

    /// <summary>
    /// The minimum amount of severity required
    /// before the anomaly becomes a hotspot.
    /// </summary>
    [DataField("anomalyFreezeZoneThreshold")]
    public float AnomalyFreezeZoneThreshold = 0.6f;

    /// <summary>
    /// The temperature of the hotspot where the anomaly is
    /// </summary>
    [DataField("freezeZoneExposeTemperature")]
    public float FreezeZoneExposeTemperature = -1000;

    /// <summary>
    /// The volume of the hotspot where the anomaly is.
    /// </summary>
    [DataField("freezeZoneExposeVolume")]
    public float FreezeZoneExposeVolume = 50;

    /// <summary>
    /// Gas released when the anomaly goes supercritical.
    /// </summary>
    [DataField("supercriticalGas")]
    public Gas SupercriticalGas = Gas.Frezon;

    /// <summary>
    /// The amount of gas released when the anomaly goes supercritical
    /// </summary>
    [DataField("supercriticalMoleAmount")]
    public float SupercriticalMoleAmount = 75f;
}
