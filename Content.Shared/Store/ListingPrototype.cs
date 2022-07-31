using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Store;

/// <summary>
///     This is the data object for a store listing which is passed around in code.
///     this allows for prices and features of listings to be dynamically changed in code
///     without having to modify the prototypes.
/// </summary>
[Serializable, NetSerializable]
[Virtual, DataDefinition]
public class ListingData
{
    [DataField("name")]
    public string Name = string.Empty;

    [DataField("description")]
    public string Description = string.Empty;

    [DataField("categories", required: true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<StoreCategoryPrototype>))]
    public HashSet<string> Categories = new HashSet<string>();

    [DataField("cost", required: true, customTypeSerializer: typeof(PrototypeIdDictionarySerializer<float, CurrencyPrototype>))]
    public Dictionary<string, float> Cost = new Dictionary<string, float>();

    [DataField("conditions")]
    public HashSet<ListingCondition>? Conditions;
}

/// <summary>
///     Defines a set item listing that is available in a store
/// </summary>
[Prototype("listing")]
[Serializable, NetSerializable]
[DataDefinition]
public sealed class ListingPrototype : ListingData, IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;
}
