using System.Linq;
using System.Runtime.InteropServices;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Medical.Wounds.Prototypes;
using Content.Shared.Medical.Wounds.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Medical.Wounds.Components;

[RegisterComponent]
public sealed class WoundableComponent : Component
{
    [Access(typeof(WoundSystem),Other = AccessPermissions.Read)]
    [DataField("WoundData", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<WoundableData, DamageTypePrototype>))]
    public Dictionary<string, WoundableData> WoundData = new();

    [Access(typeof(WoundSystem),Other = AccessPermissions.Read)]
    [ViewVariables, DataField("damageResistance")]
    public DamageModifierSet DamageResistance = new();
}

[Serializable, NetSerializable, DataRecord]
public record struct WoundableData (
    [field:DataField("wounds", customTypeSerializer:typeof(PrototypeIdListSerializer<DamageTypePrototype>))]List<WoundData> Wounds,
    [field:DataField("skinDamageCap")]float SkinDamageCap,
    [field:DataField("fleshDamageCap")]float InternalDamageCap,
    [field:DataField("solidDamageCap")]float SolidDamageCap
    );
