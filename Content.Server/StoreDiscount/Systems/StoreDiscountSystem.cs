using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.StoreDiscount.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StoreDiscount.Systems;

/// <summary>
/// Discount system that is part of <see cref="StoreSystem"/>.
/// </summary>
public sealed class StoreDiscountSystem : EntitySystem
{
    private static readonly ProtoId<StoreCategoryPrototype> DiscountedStoreCategoryPrototypeKey = "DiscountedItems";

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreInitializedEvent>(OnStoreInitialized);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnBuyFinished);
    }

    /// <summary> Decrements discounted item count, removes discount modifier and category, if counter reaches zero. </summary>
    private void OnBuyFinished(ref StoreBuyFinishedEvent ev)
    {
        var (storeId, purchasedItem) = ev;
        if (!TryComp<StoreDiscountComponent>(storeId, out var discountsComponent))
        {
            return;
        }

        // find and decrement discount count for item, if there is one.
        if (!TryGetDiscountData(discountsComponent.Discounts, purchasedItem, out var discountData) || discountData.Count == 0)
        {
            return;
        }

        discountData.Count--;
        if (discountData.Count > 0)
        {
            return;
        }

        // if there were discounts, but they are all bought up now - restore state: remove modifier and remove store category
        purchasedItem.RemoveCostModifier(discountData.DiscountCategory);
        purchasedItem.Categories.Remove(DiscountedStoreCategoryPrototypeKey);
    }

    /// <summary> Initialized discounts if required. </summary>
    private void OnStoreInitialized(ref StoreInitializedEvent ev)
    {
        if (!ev.UseDiscounts)
        {
            return;
        }

        var discountComponent = EnsureComp<StoreDiscountComponent>(ev.Store);
        var discounts = InitializeDiscounts(ev.Listings);
        ApplyDiscounts(ev.Listings, discounts);
        discountComponent.Discounts = discounts;
    }

    private IReadOnlyList<StoreDiscountData> InitializeDiscounts(
        IReadOnlyCollection<ListingDataWithCostModifiers> listings,
        int totalAvailableDiscounts = 6
    )
    {
        // Get list of categories with cumulative weights.
        // for example if we have categories with weights 2, 18 and 80
        // list of cumulative ones will be 2,20,100 (with 100 being total).
        // Then roll amount of unique listing items to be discounted under
        // each category, and after that - roll exact items in categories
        // and their cost

        var prototypes = _prototypeManager.EnumeratePrototypes<DiscountCategoryPrototype>();
        var categoriesWithCumulativeWeight = new CumulativeWeightMap<DiscountCategoryPrototype>(prototypes);
        var uniqueListingItemCountByCategory = PickCategoriesToRoll(totalAvailableDiscounts, categoriesWithCumulativeWeight);

        return RollItems(listings, uniqueListingItemCountByCategory);
    }

    /// <summary>
    /// Roll <b>how many</b> unique listing items which discount categories going to have. This will be used later to then pick listing items
    /// to actually set discounts.
    /// </summary>
    /// <remarks>
    /// Not every discount category have equal chance to be rolled, and not every discount category even can be rolled.
    /// This step is important to distribute discounts properly (weighted) and with respect of
    /// category maxItems, and more importantly - to not roll same item multiple times on next step.
    /// </remarks>
    /// <param name="totalAvailableDiscounts">
    /// Total amount of different listing items to be discounted. Depending on <see cref="DiscountCategoryPrototype.MaxItems"/>
    /// there might be less discounts then <see cref="totalAvailableDiscounts"/>, but never more.
    /// </param>
    /// <param name="categoriesWithCumulativeWeightMap">
    /// Map of discount category cumulative weights by respective protoId of discount category.
    /// </param>
    /// <returns>Map: <b>count</b> of different listing items to be discounted, by discount category.</returns>
    private Dictionary<ProtoId<DiscountCategoryPrototype>, int> PickCategoriesToRoll(
        int totalAvailableDiscounts,
        CumulativeWeightMap<DiscountCategoryPrototype> categoriesWithCumulativeWeightMap
    )
    {
        var chosenDiscounts = new Dictionary<ProtoId<DiscountCategoryPrototype>, int>();
        for (var i = 0; i < totalAvailableDiscounts; i++)
        {
            var discountCategory = categoriesWithCumulativeWeightMap.RollCategory(_random);
            if (discountCategory == null)
            {
                break;
            }

            // * if category was not previously picked - we mark it as picked 1 time
            // * if category was previously picked - we increment its 'picked' marker
            // * if category 'picked' marker going to exceed limit on category - we need to remove IT from further rolls
            int newDiscountCount;
            if (!chosenDiscounts.TryGetValue(discountCategory.ID, out var alreadySelectedCount))
            {
                newDiscountCount = 1;
            }
            else
            {
                newDiscountCount = alreadySelectedCount + 1;
            }
            chosenDiscounts[discountCategory.ID] = newDiscountCount;

            if (newDiscountCount >= discountCategory.MaxItems)
            {
                categoriesWithCumulativeWeightMap.Remove(discountCategory);
            }
        }

        return chosenDiscounts;
    }

    /// <summary>
    /// Rolls list of exact <see cref="ListingData"/> items to be discounted, and amount of currency to be discounted.
    /// </summary>
    /// <param name="listings">List of all available listing items from which discounted ones could be selected.</param>
    /// <param name="chosenDiscounts"></param>
    /// <returns>Collection of containers with rolled discount data.</returns>
    private IReadOnlyList<StoreDiscountData> RollItems(IEnumerable<ListingDataWithCostModifiers> listings, Dictionary<ProtoId<DiscountCategoryPrototype>, int> chosenDiscounts)
    {
        // To roll for discounts on items we: pick listing items that have values inside 'DiscountDownTo'.
        // then we iterate over discount categories that were chosen on previous step and pick unique set
        // of items from that exact category. Then we roll for their cost:
        // cost could be anything between DiscountDownTo value and actual item cost.

        var listingsByDiscountCategory = GroupDiscountableListingsByDiscountCategory(listings);

        var list = new List<StoreDiscountData>();
        foreach (var (discountCategory, itemsCount) in chosenDiscounts)
        {
            if (!listingsByDiscountCategory.TryGetValue(discountCategory, out var itemsForDiscount))
            {
                continue;
            }

            var chosen = _random.GetItems(itemsForDiscount, itemsCount, allowDuplicates: false);
            foreach (var listingData in chosen)
            {
                var cost = listingData.OriginalCost;
                var discountAmountByCurrencyId = RollItemCost(cost, listingData);

                var discountData = new StoreDiscountData
                {
                    ListingId = listingData.ID,
                    Count = 1,
                    DiscountCategory = listingData.DiscountCategory!.Value,
                    DiscountAmountByCurrency = discountAmountByCurrencyId
                };
                list.Add(discountData);
            }
        }

        return list;
    }

    /// <summary> Roll amount of each currency by which item cost should be reduced. </summary>
    /// <remarks>
    /// No point in confusing user with a fractional number, so we remove numbers after dot that were rolled.
    /// </remarks>
    private Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> RollItemCost(
        IReadOnlyDictionary<ProtoId<CurrencyPrototype>, FixedPoint2> originalCost,
        ListingDataWithCostModifiers listingData
    )
    {
        var discountAmountByCurrencyId = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(originalCost.Count);
        foreach (var (currency, amount) in originalCost)
        {
            if (!listingData.DiscountDownTo.TryGetValue(currency, out var discountUntilValue))
            {
                continue;
            }

            var discountUntilRolledValue = _random.NextDouble(discountUntilValue.Double(), amount.Double());
            var discountedCost = amount - Math.Floor(discountUntilRolledValue);

            // discount is negative modifier for cost
            discountAmountByCurrencyId.Add(currency.Id, -discountedCost);
        }

        return discountAmountByCurrencyId;
    }

    private void ApplyDiscounts(IReadOnlyList<ListingDataWithCostModifiers> listings, IReadOnlyCollection<StoreDiscountData> discounts)
    {
        foreach (var discountData in discounts)
        {
            if (discountData.Count <= 0)
            {
                continue;
            }

            ListingDataWithCostModifiers? found = null;
            for (var i = 0; i < listings.Count; i++)
            {
                var current = listings[i];
                if (current.ID == discountData.ListingId)
                {
                    found = current;
                    break;
                }
            }

            if (found == null)
            {
                Log.Warning($"Attempted to apply discount to listing item with {discountData.ListingId}, but found no such listing item.");
                return;
            }

            found.AddCostModifier(discountData.DiscountCategory, discountData.DiscountAmountByCurrency);
            found.Categories.Add(DiscountedStoreCategoryPrototypeKey);
        }
    }

    private static Dictionary<ProtoId<DiscountCategoryPrototype>, List<ListingDataWithCostModifiers>> GroupDiscountableListingsByDiscountCategory(
        IEnumerable<ListingDataWithCostModifiers> listings
    )
    {
        var listingsByDiscountCategory = new Dictionary<ProtoId<DiscountCategoryPrototype>, List<ListingDataWithCostModifiers>>();
        foreach (var listing in listings)
        {
            var category = listing.DiscountCategory;
            if (category == null || listing.DiscountDownTo.Count == 0)
            {
                continue;
            }

            if (!listingsByDiscountCategory.TryGetValue(category.Value, out var list))
            {
                list = new List<ListingDataWithCostModifiers>();
                listingsByDiscountCategory[category.Value] = list;
            }

            list.Add(listing);
        }

        return listingsByDiscountCategory;
    }

    private static bool TryGetDiscountData(
        IReadOnlyList<StoreDiscountData> discounts,
        ListingDataWithCostModifiers purchasedItem,
        [MaybeNullWhen(false)] out StoreDiscountData discountData
    )
    {
        for (var i = 0; i < discounts.Count; i++)
        {
            var current = discounts[i];
            if (current.ListingId == purchasedItem.ID)
            {
                discountData = current;
                return true;
            }
        }

        discountData = null!;
        return false;
    }

}

/// <summary>
/// Event of store being initialized.
/// </summary>
/// <param name="TargetUser">EntityUid of store entity owner.</param>
/// <param name="Store">EntityUid of store entity.</param>
/// <param name="UseDiscounts">Marker, if store should have discounts.</param>
/// <param name="UseSecondHand">Marker, if store should have a second-hand (worn/damaged items) tab.</param>
/// <param name="Listings">List of available listings items.</param>
[ByRefEvent]
public record struct StoreInitializedEvent(
    EntityUid TargetUser,
    EntityUid Store,
    bool UseDiscounts,
    bool UseSecondHand,
    IReadOnlyList<ListingDataWithCostModifiers> Listings
);
