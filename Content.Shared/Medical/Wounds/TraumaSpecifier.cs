using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Medical.Wounds;

[DataDefinition, NetSerializable, Serializable]
public struct TraumaSpecifier
{
    [field: DataField("traumas",
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<Trauma, TraumaTypePrototype>))]
    public Dictionary<string, Trauma> TraumaValues { get; }

    public void ApplyModifiers(TraumaModifierSet modifiers)
    {
        foreach (var traumaType in modifiers.Coefficients.Keys)
        {
            if (!TraumaValues.ContainsKey(traumaType))
                continue;
            var temp = TraumaValues[traumaType];
            TraumaValues[traumaType] = temp with {Damage = temp.Damage * modifiers.Coefficients[traumaType]};
        }

        foreach (var traumaType in modifiers.FlatReduction.Keys)
        {
            if (!TraumaValues.ContainsKey(traumaType))
                continue;
            var temp = TraumaValues[traumaType];
            TraumaValues[traumaType] = temp with {Damage = temp.Damage - modifiers.FlatReduction[traumaType]};
        }
    }

    public void ApplyPenModifiers(TraumaModifierSet modifiers)
    {
        foreach (var traumaType in modifiers.Coefficients.Keys)
        {
            if (!TraumaValues.ContainsKey(traumaType))
                continue;
            var temp = TraumaValues[traumaType];
            TraumaValues[traumaType] = temp with
            {
                PenetrationChance = temp.PenetrationChance * modifiers.Coefficients[traumaType]
            };
        }

        foreach (var traumaType in modifiers.FlatReduction.Keys)
        {
            if (!TraumaValues.ContainsKey(traumaType))
                continue;
            var temp = TraumaValues[traumaType];
            TraumaValues[traumaType] = temp with
            {
                PenetrationChance = temp.PenetrationChance - modifiers.FlatReduction[traumaType]
            };
        }
    }
}

[DataRecord, Serializable, NetSerializable]
public record struct Trauma(
    [field: DataField("damage", required: true)]
    FixedPoint2 Damage, //Damage represents the amount of trauma dealt
    [field: DataField("chance", required: false)]
    FixedPoint2 PenetrationChance,
    [field: DataField("penTraumaType", required: false, customTypeSerializer: typeof(PrototypeIdSerializer<TraumaTypePrototype>))]
    string? PenTraumaType = null
); //Penetration represents how much this damage penetrates to hit child parts
