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
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, TraumaTypePrototype>))]
    public Dictionary<string, FixedPoint2> Coefficients = new();

    [DataField("flatReductions",
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, TraumaTypePrototype>))]
    public Dictionary<string, FixedPoint2> FlatReduction = new();

    public bool TryGetCoefficentForTraumaType(string traumaType, out FixedPoint2 coefficent)
    {
        return Coefficients.TryGetValue(traumaType, out coefficent);
    }

    public bool TryGetFlatReductionForTraumaType(string traumaType, out FixedPoint2 reduction)
    {
        return Coefficients.TryGetValue(traumaType, out reduction);
    }


    public TraumaModifierSet()
    {
    }
}
