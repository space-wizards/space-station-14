using System.Linq;
using Content.Shared.FixedPoint;
using Content.Shared.Store.Components;
using Content.Shared.StoreDiscount.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Store;

/// <summary>
///     This is the data object for a store listing which is passed around in code.
///     this allows for prices and features of listings to be dynamically changed in code
///     without having to modify the prototypes.
/// </summary>
[Serializable, NetSerializable]
[Virtual, DataDefinition]
public partial class ListingData : IEquatable<ListingData>
{
    public ListingData()
    {
    }

    public ListingData(ListingData other) : this(
        other.Name,
        other.DiscountCategory,
        other.Description,
        other.Conditions,
        other.Icon,
        other.Priority,
        other.ProductEntity,
        other.ProductAction,
        other.ProductUpgradeId,
        other.ProductActionEntity,
        other.ProductEvent,
        other.RaiseProductEventOnUser,
        other.PurchaseAmount,
        other.ID,
        other.Categories,
        other.OriginalCost,
        other.RestockTime,
        other.DiscountDownTo,
        other.DisableRefund
    )
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
        HashSet<ProtoId<StoreCategoryPrototype>> categories,
        IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> originalCost,
        TimeSpan restockTime,
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> dataDiscountDownTo,
        bool disableRefund
    )
    {
        Name = name;
        DiscountCategory = discountCategory;
        Description = description;
        Conditions = conditions?.ToList();
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
        Categories = categories.ToHashSet();
        OriginalCost = originalCost;
        RestockTime = restockTime;
        DiscountDownTo = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(dataDiscountDownTo);
        DisableRefund = disableRefund;
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
    [DataField]
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
    public HashSet<ProtoId<StoreCategoryPrototype>> Categories = new();

    /// <summary>
    /// The original cost of the listing. FixedPoint2 represents the amount of that currency.
    /// This fields should not be used for getting actual cost of item, as there could be
    /// cost modifiers (due to discounts or surplus). Use Cost property on derived class instead.
    /// </summary>
    [DataField]
    public IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> OriginalCost = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>();

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
    [DataField]
    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> DiscountDownTo = new();

    /// <summary>
    /// Whether or not to disable refunding for the store when the listing is purchased from it.
    /// </summary>
    [DataField]
    public bool DisableRefund = false;

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

        if (!OriginalCost.OrderBy(x => x).SequenceEqual(listing.OriginalCost.OrderBy(x => x)))
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
public sealed partial class ListingPrototype : ListingData, IPrototype
{
    /// <summary> Setter/getter for item cost from prototype. </summary>
    [DataField]
    public IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> Cost
    {
        get => OriginalCost;
        set => OriginalCost = value;
    }
}

/// <summary> Wrapper around <see cref="ListingData"/> that enables controller and centralized cost modification. </summary>
/// <remarks>
/// Server lifecycle of those objects is bound to <see cref="StoreComponent.FullListingsCatalog"/>, which is their local cache. To fix
/// cost changes after server side change (for example, when all items with set discount are bought up) <see cref="ApplyAllModifiers"/> is called
/// on changes.
/// Client side lifecycle is possible due to modifiers and original cost being transferred fields and cost being calculated when needed. Modifiers changes
/// should not (are not expected) be happening on client.
/// </remarks>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class ListingDataWithCostModifiers : ListingData
{
    private IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2>? _costModified;

    /// <summary>
    /// Map of values, by which calculated cost should be modified, with modification sourceId.
    /// Instead of modifying this field - use <see cref="RemoveCostModifier"/> and <see cref="AddCostModifier"/>
    /// when possible.
    /// </summary>
    [DataField]
    public Dictionary<string, Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>> CostModifiersBySourceId = new();

    /// <inheritdoc />
    public ListingDataWithCostModifiers(ListingData listingData)
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
            listingData.OriginalCost,
            listingData.RestockTime,
            listingData.DiscountDownTo,
            listingData.DisableRefund
        )
    {
    }

    /// <summary> Marker, if cost of listing item have any modifiers. </summary>
    public bool IsCostModified => CostModifiersBySourceId.Count > 0;

    /// <summary> Cost of listing item after applying all available modifiers. </summary>
    public IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> Cost
    {
        get
        {
            return _costModified ??= CostModifiersBySourceId.Count == 0
                ? new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(OriginalCost)
                : ApplyAllModifiers();
        }
    }

    /// <summary> Add map with currencies and value by which cost should be modified when final value is calculated. </summary>
    /// <param name="modifierSourceId">Id of modifier source. Can be used for removing modifier later.</param>
    /// <param name="modifiers">Values for cost modification.</param>
    public void AddCostModifier(string modifierSourceId, Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> modifiers)
    {
        CostModifiersBySourceId.Add(modifierSourceId, modifiers);
        if (_costModified != null)
        {
            _costModified = ApplyAllModifiers();
        }
    }

    /// <summary> Remove cost modifier with passed sourceId. </summary>
    public void RemoveCostModifier(string modifierSourceId)
    {
        CostModifiersBySourceId.Remove(modifierSourceId);
        if (_costModified != null)
        {
            _costModified = ApplyAllModifiers();
        }
    }

    /// <summary> Check if listing item can be bought with passed balance. </summary>
    public bool CanBuyWith(Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> balance)
    {
        foreach (var (currency, amount) in Cost)
        {
            if (!balance.ContainsKey(currency))
                return false;

            if (balance[currency] < amount)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets percent of reduced/increased cost that modifiers give respective to <see cref="ListingData.OriginalCost"/>.
    /// Percent values are numbers between 0 and 1.
    /// </summary>
    public IReadOnlyDictionary<ProtoId<CurrencyPrototype>, float> GetModifiersSummaryRelative()
    {
        var modifiersSummaryAbsoluteValues = CostModifiersBySourceId.Aggregate(
            new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(),
            (accumulator, x) =>
            {
                foreach (var (currency, amount) in x.Value)
                {
                    accumulator.TryGetValue(currency, out var accumulatedAmount);
                    accumulator[currency] = accumulatedAmount + amount;
                }

                return accumulator;
            }
        );
        var relativeModifiedPercent = new Dictionary<ProtoId<CurrencyPrototype>, float>();
        foreach (var (currency, discountAmount) in modifiersSummaryAbsoluteValues)
        {
            if (OriginalCost.TryGetValue(currency, out var originalAmount))
            {
                var discountPercent = (float)discountAmount.Value / originalAmount.Value;
                relativeModifiedPercent.Add(currency, discountPercent);
            }
        }

        return relativeModifiedPercent;

    }

    private Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> ApplyAllModifiers()
    {
        var dictionary = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(OriginalCost);
        foreach (var (_, modifier) in CostModifiersBySourceId)
        {
            ApplyModifier(dictionary, modifier);
        }

        return dictionary;
    }

    private void ApplyModifier(
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> applyTo,
        IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> modifier
    )
    {
        foreach (var (currency, modifyBy) in modifier)
        {
            if (applyTo.TryGetValue(currency, out var currentAmount))
            {
                var modifiedAmount = currentAmount + modifyBy;
                if (modifiedAmount < 0)
                {
                    modifiedAmount = 0;
                    // no negative cost allowed
                }
                applyTo[currency] = modifiedAmount;
            }
        }
    }
}

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
    [DataField]
    public int Weight { get; private set; }

    /// <summary>
    /// Maximum amount of items that are allowed to be picked from this category.
    /// </summary>
    [DataField]
    public int? MaxItems { get; private set; }
}
