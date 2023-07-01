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

    [DataField("bypassShield")]
    public bool ShieldPiercing { get; set; } = false;

    [DataField("hullMin")]
    public int HullDamageMin { get; set; } = 1;

    [DataField("hullMax")]
    public int HullDamageMax { get; set; } = 3;

    [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype { get; set; } = "";
}
