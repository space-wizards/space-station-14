using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Medical.Wounds;

[DataRecord, Serializable, NetSerializable]
public record struct TraumaDamage(
    [field: DataField("damage", required: true)]
    FixedPoint2 Damage, //Damage represents the amount of trauma dealt
    [field: DataField("penChance", required: false)]
    FixedPoint2 PenetrationChance,
    [field: DataField("penType", required: false, customTypeSerializer: typeof(PrototypeIdSerializer<TraumaPrototype>))]
    string? PenTraumaType = null
); //Penetration represents how much this damage penetrates to hit child parts
