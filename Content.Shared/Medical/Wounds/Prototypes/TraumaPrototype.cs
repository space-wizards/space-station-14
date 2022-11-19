using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Medical.Wounds.Prototypes;

[Prototype("trauma")]
public sealed class TraumaPrototype : IPrototype
{
    [IdDataField] public string ID { get; init; } = string.Empty;

    //Note: these should be defined in order of severity!
    //list of possible wounds sorted by their trauma cutoffs
    [DataField("wounds", required: true,
        customTypeSerializer: typeof(PrototypeIdValueDictionarySerializer<FixedPoint2, EntityPrototype>))]
    public SortedDictionary<FixedPoint2, string> Wounds { get; init; } = new();

    // TODO wounds wound cap
}
