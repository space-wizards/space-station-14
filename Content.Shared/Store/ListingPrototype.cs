using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.FixedPoint;
using System.Linq;

namespace Content.Shared.Store;

/// <summary>
///     This is the data object for a store listing which is passed around in code.
///     this allows for prices and features of listings to be dynamically changed in code
///     without having to modify the prototypes.
/// </summary>
[Serializable, NetSerializable]
[Virtual, DataDefinition]
public class ListingData : IEquatable<ListingData>
{
    [DataField("name")]
    public string Name = string.Empty;

    [DataField("description")]
    public string Description = string.Empty;

    [DataField("categories", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<StoreCategoryPrototype>))]
    public List<string> Categories = new();

    [DataField("cost", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, CurrencyPrototype>))]
    public Dictionary<string, FixedPoint2> Cost = new();

    [NonSerialized]
    [DataField("conditions", serverOnly: true)]
    public List<ListingCondition>? Conditions;

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

    public bool Equals(ListingData? listing)
    {
        if (listing == null)
            return false;

        //simple conditions
        if (Priority != listing.Priority ||
            Name != listing.Name ||
            Description != listing.Description ||
            ProductEntity != listing.ProductEntity ||
            ProductAction != listing.ProductAction ||
            ProductEvent != listing.ProductEvent)
            return false;

        if (Icon != null && !Icon.Equals(listing.Icon))
            return false;

        ///more complicated conditions that eat perf. these don't really matter
        ///as much because you will very rarely have to check these. 
        if (!Categories.OrderBy(x => x).SequenceEqual(listing.Categories.OrderBy(x => x)))
            return false;

        if (!Cost.OrderBy(x => x).SequenceEqual(listing.Cost.OrderBy(x => x)))
            return false;

        if ((Conditions != null && listing.Conditions != null) &&
            !Conditions.OrderBy(x => x).SequenceEqual(listing.Conditions.OrderBy(x => x)))
            return false;

        return true;
    }
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
