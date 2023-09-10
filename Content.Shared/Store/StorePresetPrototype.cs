using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Content.Shared.FixedPoint;

namespace Content.Shared.Store;

/// <summary>
///     Specifies generic info for initializing a store.
/// </summary>
[Prototype("storePreset")]
[DataDefinition]
public sealed partial class StorePresetPrototype : IPrototype
{
    [ViewVariables] [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// The name displayed at the top of the store window
    /// </summary>
    [DataField("storeName", required: true)]
    public string StoreName { get; private set; } = string.Empty;

    /// <summary>
    /// The categories that this store can access
    /// </summary>
    [DataField("categories", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<StoreCategoryPrototype>))]
    public HashSet<string> Categories { get; private set; } = new();

    /// <summary>
    /// The inital balance that the store initializes with.
    /// </summary>
    [DataField("initialBalance",
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, CurrencyPrototype>))]
    public Dictionary<string, FixedPoint2>? InitialBalance { get; private set; }

    /// <summary>
    /// The currencies that are accepted in the store
    /// </summary>
    [DataField("currencyWhitelist", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<CurrencyPrototype>))]
    public HashSet<string> CurrencyWhitelist { get; private set; } = new();
}
