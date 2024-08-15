using System.Linq;
using Content.Server.Store.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.StoreDiscount.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StoreDiscount.Systems;

/// <summary>
/// Discount system that is part of <see cref="StoreSystem"/>.
/// </summary>
public sealed class StoreDiscountSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreInitializedEvent>(OnStoreInitialized);
        SubscribeLocalEvent<StoreBuyAttemptEvent>(OnBuyRequest);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnBuyFinished);
        SubscribeLocalEvent<GetDiscountsEvent>(OnGetDiscounts);
    }

    /// <summary> Extracts discount data if there any on <see cref="GetDiscountsEvent.Store"/>. </summary>
    private void OnGetDiscounts(GetDiscountsEvent ev)
    {
        if (TryComp<StoreDiscountComponent>(ev.Store, out var discountsComponent))
        {
            ev.DiscountsData = discountsComponent.Discounts;
        }
    }

    /// <summary> Decrements discounted item count. </summary>
    private void OnBuyFinished(ref StoreBuyFinishedEvent ev)
    {
        var (storeId, purchasedItemId) = ev;
        var discounts = Array.Empty<StoreDiscountData>();
        if (TryComp<StoreDiscountComponent>(storeId, out var discountsComponent))
        {
            discounts = discountsComponent.Discounts;
        }

        var discountData = discounts.FirstOrDefault(x => x.Count > 0 && x.ListingId == purchasedItemId);
        if (discountData == null)
        {
            return;
        }

        discountData.Count--;
    }

    /// <summary> Refine listing item cost using discounts. </summary>
    private void OnBuyRequest(StoreBuyAttemptEvent ev)
    {
        var discounts = Array.Empty<StoreDiscountData>();
        if (TryComp<StoreDiscountComponent>(ev.StoreUid, out var discountsComponent))
        {
            discounts = discountsComponent.Discounts;
        }

        var discountData = discounts.FirstOrDefault(x => x.Count > 0 && x.ListingId == ev.PurchasingItemId);
        if (discountData == null)
        {
            return;
        }

        var withDiscount = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>();
        foreach (var (currency, amount) in ev.Cost)
        {
            var totalAmount = amount;
            if (discountData?.DiscountAmountByCurrency.TryGetValue(currency, out var discount) == true)
            {
                totalAmount -= discount;
            }

            withDiscount.Add(currency, totalAmount);
        }

        ev.Cost = withDiscount;
    }

    /// <summary> Initialized discounts if required. </summary>
    private void OnStoreInitialized(ref StoreInitializedEvent ev)
    {
        if (!TryComp<StoreComponent>(ev.Store, out var store))
        {
            return;
        }

        if (!ev.UseDiscounts)
        {
            return;
        }

        var discountComponent = EnsureComp<StoreDiscountComponent>(ev.Store);
        discountComponent.Discounts = InitializeDiscounts(ev.Listings);
    }

    private StoreDiscountData[] InitializeDiscounts(
        IEnumerable<ListingData> listings,
        int totalAvailableDiscounts = 3
    )
    {
        // get list of categories with cumulative weights.
        // for example if we have categories with weights 2, 18 and 80
        // list of cumulative ones will be 2,20,100 (with 100 being total)
        var discountCumulativeWeightByDiscountCategoryId = PreCalculateDiscountCategoriesWithCumulativeWeights();

        // roll HOW MANY different listing items to discount in which categories.
        var chosenDiscounts = PickCategoriesToRoll(totalAvailableDiscounts, discountCumulativeWeightByDiscountCategoryId);

        return RollItems(listings, chosenDiscounts).ToArray();
    }

    /// <summary>
    /// Prepares list of discount categories with pre-calculated cumulative wights.
    /// Only categories that have weight and have more then 0 max items in category.
    /// </summary>
    /// <returns> List of discount categories.</returns>
    private List<DiscountCategoryWithCumulativeWeight> PreCalculateDiscountCategoriesWithCumulativeWeights()
    {
        List<DiscountCategoryWithCumulativeWeight> discountCumulativeWeightByDiscountCategoryId = new();

        var cumulativeWeight = 0;
        foreach (var discountCategory in _prototypeManager.EnumeratePrototypes<DiscountCategoryPrototype>())
        {
            if (discountCategory.Weight == 0 || discountCategory.MaxItems == 0)
            {
                continue;
            }

            cumulativeWeight += discountCategory.Weight;
            discountCumulativeWeightByDiscountCategoryId.Add(new(discountCategory, cumulativeWeight));
        }

        return discountCumulativeWeightByDiscountCategoryId;
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
    /// <param name="discountCumulativeWeightByDiscountCategoryId">Map of discount category cumulative weight by its protoId.</param>
    /// <returns>Map: count of different listing items to be discounted by their discount category.</returns>
    private Dictionary<ProtoId<DiscountCategoryPrototype>, int> PickCategoriesToRoll(
        int totalAvailableDiscounts,
        List<DiscountCategoryWithCumulativeWeight> discountCumulativeWeightByDiscountCategoryId
    )
    {
        // cumulative weight of last discount category is total weight of all categories
        var sumWeight = discountCumulativeWeightByDiscountCategoryId[^1].CumulativeWeight;
        var chosenDiscounts = new Dictionary<ProtoId<DiscountCategoryPrototype>, int>();
        for (var i = 0; i < totalAvailableDiscounts; i++)
        {
            var roll = _random.Next(sumWeight);

            // We rolled random point inside range of 0 and 'total weight' to pick category respecting category weights
            // now we find index of category we rolled. If category cumulative weight is less than roll -
            // we rolled other category, skip and try next
            var index = 0;
            DiscountCategoryWithCumulativeWeight container;
            while ((container = discountCumulativeWeightByDiscountCategoryId[index]).CumulativeWeight < roll)
            {
                index++;
            }

            // * if category was not previously picked - we mark it as picked 1 time
            // * if category was previously picked - we increase its 'picked' marker
            // * if category 'picked' marker going to exceed limit on category - we remove it from further rolls
            // and reduce total cumulative count by that category weight, so it won't affect next rolls
            if (!chosenDiscounts.TryGetValue(container.DiscountCategory.ID, out var alreadySelectedCount))
            {
                chosenDiscounts[container.DiscountCategory.ID] = 1;
            }
            else if (alreadySelectedCount < container.DiscountCategory.MaxItems)
            {
                var newDiscountCount = chosenDiscounts[container.DiscountCategory.ID] + 1;
                chosenDiscounts[container.DiscountCategory.ID] = newDiscountCount;
                if (newDiscountCount == container.DiscountCategory.MaxItems)
                {
                    discountCumulativeWeightByDiscountCategoryId.Remove(container);
                    sumWeight -= container.DiscountCategory.Weight;
                }
            }
        }

        return chosenDiscounts;
    }

    private List<StoreDiscountData> RollItems(IEnumerable<ListingData> listings, Dictionary<ProtoId<DiscountCategoryPrototype>, int> chosenDiscounts)
    {
        // To roll for discounts on items we: pick listing items that have values inside 'DiscountDownTo'.
        // then we iterate over discount categories that were chosen on previous step and pick unique set
        // of items from that exact category. Then we roll for their cost:
        // cost could be anything between DiscountDownTo value and actual item cost.
        var listingsByDiscountCategory = listings.Where(x => x is { DiscountCategory: not null, DiscountDownTo.Count: > 0 })
                                                 .GroupBy(x => x.DiscountCategory!.Value)
                                                 .ToDictionary(
                                                     x => x.Key,
                                                     x => x.ToArray()
                                                 );
        var list = new List<StoreDiscountData>(chosenDiscounts.Sum(x => x.Value));

        foreach (var (discountCategory, itemsCount) in chosenDiscounts)
        {
            if (!listingsByDiscountCategory.TryGetValue(discountCategory, out var itemsForDiscount))
            {
                continue;
            }

            var chosen = _random.GetItems(itemsForDiscount, itemsCount, allowDuplicates: false);
            foreach (var listingData in chosen)
            {
                var cost = listingData.Cost;
                var discountAmountByCurrencyId = RollItemCost(cost, listingData);

                var discountData = new StoreDiscountData
                {
                    ListingId = listingData.ID,
                    Count = 1,
                    DiscountAmountByCurrency = discountAmountByCurrencyId
                };
                list.Add(discountData);
            }
        }

        return list;
    }

    /// <summary> Roll amount of each currency by which item cost should be reduced. </summary>
    private Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> RollItemCost(Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> cost, ListingData listingData)
    {
        var discountAmountByCurrencyId = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(cost.Count);
        foreach (var (currency, amount) in cost)
        {
            if (!listingData.DiscountDownTo.TryGetValue(currency, out var discountUntilValue))
            {
                continue;
            }

            var discountUntilRolledValue = _random.NextDouble(discountUntilValue.Double(), amount.Double());
            var leftover = discountUntilRolledValue % 1;
            var discountedCost = amount - (discountUntilRolledValue - leftover);

            discountAmountByCurrencyId.Add(currency.Id, discountedCost);
        }

        return discountAmountByCurrencyId;
    }

    private sealed class DiscountCategoryWithCumulativeWeight(DiscountCategoryPrototype discountCategory, int cumulativeWeight)
    {
        public DiscountCategoryPrototype DiscountCategory { get; set; } = discountCategory;
        public int CumulativeWeight { get; set; } = cumulativeWeight;
    }

}

/// <summary> Attempt to get list of discounts. </summary>
public sealed partial class GetDiscountsEvent(EntityUid store)
{
    /// <summary>
    /// EntityUid for which discounts should be retrieved
    /// </summary>

    public EntityUid Store { get; } = store;

    /// <summary>
    /// Collection of discounts to fill.
    /// </summary>

    public StoreDiscountData[]? DiscountsData { get; set; }
}
