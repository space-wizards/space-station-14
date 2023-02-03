using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Medical.Wounds;

[DataRecord, Serializable, NetSerializable]
public record struct TraumaDamage(
    FixedPoint2 PenChance,
    [field: DataField("penType", customTypeSerializer: typeof(PrototypeIdSerializer<TraumaPrototype>))]
    string? PenType = null
);
