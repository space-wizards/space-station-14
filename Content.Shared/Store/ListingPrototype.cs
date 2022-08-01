using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.FixedPoint;

namespace Content.Shared.Store;

/// <summary>
///     This is the data object for a store listing which is passed around in code.
///     this allows for prices and features of listings to be dynamically changed in code
///     without having to modify the prototypes.
/// </summary>
[Serializable, NetSerializable]
[Virtual, DataDefinition]
public record ListingData
{
    [DataField("name")]
    public string Name = string.Empty;

    [DataField("description")]
    public string Description = string.Empty;

    [DataField("categories", required: true, customTypeSerializer: typeof(PrototypeIdHashSetSerializer<StoreCategoryPrototype>))]
    public HashSet<string> Categories = new();

    [DataField("cost", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, CurrencyPrototype>))]
    public Dictionary<string, FixedPoint2> Cost = new();

    [DataField("conditions", serverOnly: true)]
    public HashSet<ListingCondition>? Conditions;

    [DataField("icon")]
    public SpriteSpecifier? Icon;

    [DataField("priority")]
    public int Priority = 10;

    [DataField("productEntity", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ProductEntity;

    [DataField("productAction", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string? ProductAction;

    [DataField("productEvent")]
    public object? ProductEvent;

    /// <summary>
    /// used internally for tracking how many times an item was purchased.
    /// </summary>
    public int PurchaseAmount = 0;
}

/// <summary>
///     Defines a set item listing that is available in a store
/// </summary>
[Prototype("listing")]
[Serializable, NetSerializable]
[DataDefinition]
public sealed record ListingPrototype : ListingData, IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;
}
