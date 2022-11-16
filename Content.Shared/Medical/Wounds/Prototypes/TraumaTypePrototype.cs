using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Medical.Wounds.Prototypes;

[Prototype("traumaType")]
public sealed class TraumaTypePrototype : IPrototype
{
    [IdDataField] public string ID { get; init; } = string.Empty;

    //Note: these should be defined in order of severity!
    //list of possible wounds sorted by their trauma cutoffs
    [DataField("woundPool", required: true,
        customTypeSerializer: typeof(PrototypeIdValueDictionarySerializer<FixedPoint2, InjuryPrototype>))]
    public SortedDictionary<FixedPoint2, string> WoundPool { get; init; } = new();
}
