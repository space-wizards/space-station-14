using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Medical.Wounds;

[DataDefinition]
[Serializable, NetSerializable]
public struct TraumaModifierSet
{
    [DataField("coefficients",
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, TraumaPrototype>))]
    public Dictionary<string, FixedPoint2> Coefficients = new();

    [DataField("flatReductions",
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, TraumaPrototype>))]
    public Dictionary<string, FixedPoint2> FlatReduction = new();

    public TraumaModifierSet()
    {
    }
}
