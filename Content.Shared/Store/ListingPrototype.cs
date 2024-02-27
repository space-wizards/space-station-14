using System.Linq;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;

namespace Content.Shared.Store;

/// <summary>
///     This is the data object for a store listing which is passed around in code.
///     this allows for prices and features of listings to be dynamically changed in code
///     without having to modify the prototypes.
/// </summary>
[Serializable, NetSerializable]
[Virtual, DataDefinition]
public partial class ListingData : IEquatable<ListingData>, ICloneable
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of the listing. If empty, uses the entity's name (if present)
    /// </summary>
    [DataField("name")]
    public string Name = string.Empty;

    /// <summary>
    /// The description of the listing. If empty, uses the entity's description (if present)
    /// </summary>
    [DataField("description")]
    public string Description = string.Empty;

    /// <summary>
    /// The categories that this listing applies to. Used for filtering a listing for a store.
    /// </summary>
    [DataField("categories", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<StoreCategoryPrototype>))]
    public List<string> Categories = new();

    /// <summary>
    /// The cost of the listing. String represents the currency type while the FixedPoint2 represents the amount of that currency.
    /// </summary>
    [DataField("cost", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, CurrencyPrototype>))]
    public Dictionary<string, FixedPoint2> Cost = new();

    /// <summary>
    /// Specific customizeable conditions that determine whether or not the listing can be purchased.
    /// </summary>
    [NonSerialized]
    [DataField("conditions", serverOnly: true)]
    public List<ListingCondition>? Conditions;

    /// <summary>
    /// The icon for the listing. If null, uses the icon for the entity or action.
    /// </summary>
    [DataField("icon")]
    public SpriteSpecifier? Icon;

    /// <summary>
    /// The priority for what order the listings will show up in on the menu.
    /// </summary>
    [DataField("priority")]
    public int Priority = 0;

    /// <summary>
    /// The entity that is given when the listing is purchased.
    /// </summary>
    [DataField("productEntity", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ProductEntity;

    /// <summary>
    /// The action that is given when the listing is purchased.
    /// </summary>
    [DataField("productAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ProductAction;

    /// <summary>
    ///     The listing ID of the related upgrade listing. Can be used to link a <see cref="ProductAction"/> to an
    ///         upgrade or to use standalone as an upgrade
    /// </summary>
    [DataField]
    public ProtoId<ListingPrototype>? ProductUpgradeID;

    /// <summary>
    ///     Keeps track of the current action entity this is tied to, for action upgrades
    /// </summary>
    [DataField]
    [NonSerialized]
    public EntityUid? ProductActionEntity;

    /// <summary>
    /// The event that is broadcast when the listing is purchased.
    /// </summary>
    [DataField("productEvent")]
    public object? ProductEvent;

    /// <summary>
    /// used internally for tracking how many times an item was purchased.
    /// </summary>
    public int PurchaseAmount = 0;

    /// <summary>
    /// Used to delay purchase of some items.
    /// </summary>
    [DataField("restockTime")]
    public int RestockTime;

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
            ProductEvent != listing.ProductEvent ||
            RestockTime != listing.RestockTime)
            return false;

        if (Icon != null && !Icon.Equals(listing.Icon))
            return false;

        // more complicated conditions that eat perf. these don't really matter
        // as much because you will very rarely have to check these.
        if (!Categories.OrderBy(x => x).SequenceEqual(listing.Categories.OrderBy(x => x)))
            return false;

        if (!Cost.OrderBy(x => x).SequenceEqual(listing.Cost.OrderBy(x => x)))
            return false;

        if ((Conditions != null && listing.Conditions != null) &&
            !Conditions.OrderBy(x => x).SequenceEqual(listing.Conditions.OrderBy(x => x)))
            return false;

        return true;
    }

    /// <summary>
    /// Creates a unique instance of a listing. ALWAWYS USE THIS WHEN ENUMERATING LISTING PROTOTYPES
    /// DON'T BE DUMB AND MODIFY THE PROTOTYPES
    /// </summary>
    /// <returns>A unique copy of the listing data.</returns>
    public object Clone()
    {
        return new ListingData
        {
            ID = ID,
            Name = Name,
            Description = Description,
            Categories = Categories,
            Cost = Cost,
            Conditions = Conditions,
            Icon = Icon,
            Priority = Priority,
            ProductEntity = ProductEntity,
            ProductAction = ProductAction,
            ProductUpgradeID = ProductUpgradeID,
            ProductActionEntity = ProductActionEntity,
            ProductEvent = ProductEvent,
            PurchaseAmount = PurchaseAmount,
            RestockTime = RestockTime,
        };
    }
}

//<inheritdoc>
/// <summary>
///     Defines a set item listing that is available in a store
/// </summary>
[Prototype("listing")]
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ListingPrototype : ListingData, IPrototype
{

}
