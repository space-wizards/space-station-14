using System.Linq;
using Content.Shared.FixedPoint;
using Content.Shared.StoreDiscount.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Store;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ListingDataWithDiscount : ListingData
{
    public StoreDiscountData? DiscountData;

    /// <inheritdoc />
    public ListingDataWithDiscount(ListingData listingData, StoreDiscountData? discountData)
        : base(
            listingData.Name,
            listingData.DiscountCategory,
            listingData.Description,
            listingData.Conditions,
            listingData.Icon,
            listingData.Priority,
            listingData.ProductEntity,
            listingData.ProductAction,
            listingData.ProductUpgradeId,
            listingData.ProductActionEntity,
            listingData.ProductEvent,
            listingData.RaiseProductEventOnUser,
            listingData.PurchaseAmount,
            listingData.ID,
            listingData.Categories,
            listingData.Cost,
            listingData.RestockTime,
            listingData.DiscountDownTo
        )
    {
        DiscountData = discountData;
    }
}

/// <summary>
///     This is the data object for a store listing which is passed around in code.
///     this allows for prices and features of listings to be dynamically changed in code
///     without having to modify the prototypes.
/// </summary>
[Serializable, NetSerializable]
[Virtual, DataDefinition]
public partial class ListingData : IEquatable<ListingData>, ICloneable
{
    public ListingData()
    {
    }

    public ListingData(
        string? name,
        ProtoId<DiscountCategoryPrototype>? discountCategory,
        string? description,
        List<ListingCondition>? conditions,
        SpriteSpecifier? icon,
        int priority,
        EntProtoId? productEntity,
        EntProtoId? productAction,
        ProtoId<ListingPrototype>? productUpgradeId,
        EntityUid? productActionEntity,
        object? productEvent,
        bool raiseProductEventOnUser,
        int purchaseAmount,
        string id,
        List<ProtoId<StoreCategoryPrototype>> categories,
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> cost,
        TimeSpan restockTime,
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> listingDataDiscountDownTo
    )
    {
        Name = name;
        DiscountCategory = discountCategory;
        Description = description;
        Conditions = conditions;
        Icon = icon;
        Priority = priority;
        ProductEntity = productEntity;
        ProductAction = productAction;
        ProductUpgradeId = productUpgradeId;
        ProductActionEntity = productActionEntity;
        ProductEvent = productEvent;
        RaiseProductEventOnUser = raiseProductEventOnUser;
        PurchaseAmount = purchaseAmount;
        ID = id;
        Categories = categories;
        Cost = cost;
        RestockTime = restockTime;
    }

    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of the listing. If empty, uses the entity's name (if present)
    /// </summary>
    [DataField]
    public string? Name;

    /// <summary>
    /// Discount category for listing item. This marker describes chance of how often will item be discounted.
    /// </summary>
    [DataField("discountCategory")]
    public ProtoId<DiscountCategoryPrototype>? DiscountCategory;

    /// <summary>
    /// The description of the listing. If empty, uses the entity's description (if present)
    /// </summary>
    [DataField]
    public string? Description;

    /// <summary>
    /// The categories that this listing applies to. Used for filtering a listing for a store.
    /// </summary>
    [DataField]
    public List<ProtoId<StoreCategoryPrototype>> Categories = new();

    /// <summary>
    /// The cost of the listing. String represents the currency type while the FixedPoint2 represents the amount of that currency.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> Cost = new();

    /// <summary>
    /// Specific customizable conditions that determine whether or not the listing can be purchased.
    /// </summary>
    [NonSerialized]
    [DataField(serverOnly: true)]
    public List<ListingCondition>? Conditions;

    /// <summary>
    /// The icon for the listing. If null, uses the icon for the entity or action.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Icon;

    /// <summary>
    /// The priority for what order the listings will show up in on the menu.
    /// </summary>
    [DataField]
    public int Priority;

    /// <summary>
    /// The entity that is given when the listing is purchased.
    /// </summary>
    [DataField]
    public EntProtoId? ProductEntity;

    /// <summary>
    /// The action that is given when the listing is purchased.
    /// </summary>
    [DataField]
    public EntProtoId? ProductAction;

    /// <summary>
    /// The listing ID of the related upgrade listing. Can be used to link a <see cref="ProductAction"/> to an
    /// upgrade or to use standalone as an upgrade
    /// </summary>
    [DataField]
    public ProtoId<ListingPrototype>? ProductUpgradeId;

    /// <summary>
    /// Keeps track of the current action entity this is tied to, for action upgrades
    /// </summary>
    [DataField]
    [NonSerialized]
    public EntityUid? ProductActionEntity;

    /// <summary>
    /// The event that is broadcast when the listing is purchased.
    /// </summary>
    [DataField]
    public object? ProductEvent;

    [DataField]
    public bool RaiseProductEventOnUser;

    /// <summary>
    /// used internally for tracking how many times an item was purchased.
    /// </summary>
    [DataField]
    public int PurchaseAmount;

    /// <summary>
    /// Used to delay purchase of some items.
    /// </summary>
    [DataField]
    public TimeSpan RestockTime = TimeSpan.Zero;

    /// <summary>
    /// Options for discount - from max amount down to how much item costs can be cut by discount, absolute value.
    /// </summary>
    [DataField("discountDownTo")]
    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> DiscountDownTo = new();

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
            ProductEvent?.GetType() != listing.ProductEvent?.GetType() ||
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
    /// Creates a unique instance of a listing. ALWAYS USE THIS WHEN ENUMERATING LISTING PROTOTYPES
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
            ProductUpgradeId = ProductUpgradeId,
            ProductActionEntity = ProductActionEntity,
            ProductEvent = ProductEvent,
            PurchaseAmount = PurchaseAmount,
            RestockTime = RestockTime,
            DiscountCategory = DiscountCategory,
        };
    }
}

/// <summary>
///     Defines a set item listing that is available in a store
/// </summary>
[Prototype("listing")]
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ListingPrototype : ListingData, IPrototype;

/// <summary>
///     Defines set of rules for category of discounts -
///     how <see cref="StoreDiscountComponent"/> will be filled by respective system.
/// </summary>
[Prototype("discountCategory")]
[DataDefinition, Serializable, NetSerializable]
public sealed partial class DiscountCategoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Weight that sets chance to roll discount of that category.
    /// </summary>
    [DataField("weight")]
    public int Weight { get; private set; }

    /// <summary>
    /// Maximum amount of items that are allowed to be picked from this category.
    /// </summary>
    [DataField("maxItems")]
    public int? MaxItems { get; private set; }
}
