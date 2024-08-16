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
        IReadOnlyCollection<StoreDiscountData> discounts = Array.Empty<StoreDiscountData>();
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
        IReadOnlyCollection<StoreDiscountData> discounts = Array.Empty<StoreDiscountData>();
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

    private IReadOnlyCollection<StoreDiscountData> InitializeDiscounts(
        IEnumerable<ListingData> listings,
        int totalAvailableDiscounts = 3
    )
    {
        // get list of categories with cumulative weights.
        // for example if we have categories with weights 2, 18 and 80
        // list of cumulative ones will be 2,20,100 (with 100 being total)
        var prototypes = _prototypeManager.EnumeratePrototypes<DiscountCategoryPrototype>();
        var categoriesWithCumulativeWeight = new CategoriesWithCumulativeWeight(prototypes);

        // roll HOW MANY different listing items to discount in which categories.
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
    /// <param name="categoriesWithCumulativeWeight">Map of discount category cumulative weight by its protoId.</param>
    /// <returns>Map: count of different listing items to be discounted by their discount category.</returns>
    private Dictionary<ProtoId<DiscountCategoryPrototype>, int> PickCategoriesToRoll(
        int totalAvailableDiscounts,
        CategoriesWithCumulativeWeight categoriesWithCumulativeWeight
    )
    {
        var chosenDiscounts = new Dictionary<ProtoId<DiscountCategoryPrototype>, int>();
        for (var i = 0; i < totalAvailableDiscounts; i++)
        {
            var discountCategory = categoriesWithCumulativeWeight.RollCategory(_random);
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
                categoriesWithCumulativeWeight.Remove(discountCategory);
            }
        }

        return chosenDiscounts;
    }

    private IReadOnlyCollection<StoreDiscountData> RollItems(IEnumerable<ListingData> listings, Dictionary<ProtoId<DiscountCategoryPrototype>, int> chosenDiscounts)
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

    private sealed record CategoriesWithCumulativeWeight
    {
        private readonly List<DiscountCategoryPrototype> _categories;
        private readonly List<int> _weights;
        private int _totalWeight;

        public CategoriesWithCumulativeWeight(IEnumerable<DiscountCategoryPrototype> prototypes)
        {
            var asArray = prototypes.ToArray();
            _weights = new (asArray.Length);
            _categories = new(asArray.Length);

            var currentIndex = 0;
            _totalWeight = 0;
            for (var i = 0; i < asArray.Length; i++)
            {
                var category = asArray[i];
                if (category.MaxItems == 0 || category.Weight == 0)
                {
                    continue;
                }

                _categories.Add(category);

                if (currentIndex == 0)
                {
                    _totalWeight = category.Weight;
                }
                else
                {
                    // cumulative weight of last discount category is total weight of all categories
                    _totalWeight += category.Weight;
                }
                _weights.Add(_totalWeight);

                currentIndex++;
            }
        }

        // decrease cumulativeWeight of every category that is following current one, and then
        // reduce total cumulative count by that category weight, so it won't affect next rolls in any way
        public void Remove(DiscountCategoryPrototype discountCategory)
        {
            var indexToRemove = _categories.IndexOf(discountCategory);
            if (indexToRemove == -1)
            {
                return;
            }

            for (var i = indexToRemove + 1; i < _categories.Count; i++)
            {
                _weights[i]-= discountCategory.Weight;
            }

            _totalWeight -= discountCategory.Weight;
            _categories.RemoveAt(indexToRemove);
            _weights.RemoveAt(indexToRemove);
        }

        // We rolled random point inside range of 0 and 'total weight' to pick category respecting category weights
        // now we find index of category we rolled. If category cumulative weight is less than roll -
        // we rolled other category, skip and try next
        public DiscountCategoryPrototype? RollCategory(IRobustRandom random)
        {
            var roll = random.Next(_totalWeight);
            for (int i = 0; i < _weights.Count; i++)
            {
                if (roll < _weights[i])
                {
                    return _categories[i];
                }
            }

            return null;
        }
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

    public IReadOnlyCollection<StoreDiscountData>? DiscountsData { get; set; }
}
