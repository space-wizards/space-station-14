using Content.Shared.AirlockPainter.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._FTL.Weapons;

/// <summary>
/// This is a prototype for ammo.
/// </summary>
[Prototype("ftlAmmo")]
public sealed class FTLAmmoType : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Does it bypass shields?
    /// </summary>
    [DataField("bypassShield")]
    public bool ShieldPiercing { get; set; }

    /// <summary>
    /// Will it only attack when shields are down?
    /// </summary>
    [DataField("noShields")]
    public bool NoShields { get; set; }

    /// <summary>
    /// Minimum hull damage
    /// </summary>
    [DataField("hullMin")]
    public int HullDamageMin { get; set; } = 1;

    /// <summary>
    /// Maximum hull damage
    /// </summary>
    [DataField("hullMax")]
    public int HullDamageMax { get; set; } = 3;

    /// <summary>
    /// How many times will it hit?
    /// </summary>
    [DataField("hitTimes")]
    public int HitTimes { get; set; } = 1;

    /// <summary>
    /// Entity prototype that is spawned
    /// </summary>
    [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BulletPrototype { get; set; } = "";
}
