using System.Linq;
using System.Runtime.InteropServices;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Medical.Wounds.Prototypes;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent]
public sealed class WoundableComponent : Component
{
    [Access(typeof(WoundSystem),Other = AccessPermissions.Read)]
    public Dictionary<string, List<WoundData>> Wounds = new();

    [Access(typeof(WoundSystem),Other = AccessPermissions.Read)]
    [ViewVariables, DataField("damageResistance", required:false)]
    public DamageModifierSet DamageResistance = new();

    [Access(typeof(WoundSystem), Other = AccessPermissions.Read)]
    [ViewVariables, DataField("woundDamageCaps", required: false, customTypeSerializer:typeof(PrototypeIdDictionarySerializer<WoundDamageCap, DamageTypePrototype>))]
    public Dictionary<string, WoundDamageCap> WoundDamageCaps = new();
}


[Serializable, NetSerializable, DataRecord]
public readonly record struct WoundDamageCap ([field:DataField("skinDamageCap")]float SkinDamageCap,
    [field:DataField("fleshDamageCap")]float InternalDamageCap, [field:DataField("solidDamageCap")]float SolidDamageCap);
