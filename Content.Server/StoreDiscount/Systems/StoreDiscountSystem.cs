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
    [ValidatePrototypeId<StoreCategoryPrototype>]
    private const string DiscountedStoreCategoryPrototypeKey = "DiscountedItems";

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private StoreCategoryPrototype _discountStoreCategoryPrototype = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        _discountStoreCategoryPrototype =
            _prototypeManager.Index<StoreCategoryPrototype>(DiscountedStoreCategoryPrototypeKey);

        SubscribeLocalEvent<StoreInitializedEvent>(OnStoreInitialized);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnBuyFinished);
        SubscribeLocalEvent<ListingItemsInitializingEvent>(OnListingItemInitialized);
    }

    private void OnListingItemInitialized(ListingItemsInitializingEvent ev)
    {
        if (ev.Store == null)
        {
            return;
        }

        if (TryComp<StoreDiscountComponent>(ev.Store, out var discountsComponent))
        {
            var discounts = discountsComponent.Discounts;
            foreach (var discountData in discounts)
            {
                if (discountData.Count <= 0)
                {
                    continue;
                }

                var found = ev.ListingData.First(x => x.ID == discountData.ListingId);
                found.CostModifiersBySourceId.Add(discountData.DiscountCategory, discountData.DiscountAmountByCurrency);
                found.Categories.Add(_discountStoreCategoryPrototype);
            }
        }
    }

    /// <summary> Decrements discounted item count. </summary>
    private void OnBuyFinished(ref StoreBuyFinishedEvent ev)
    {
        var (storeId, purchasedItemId, listingData) = ev;
        if (!TryComp<StoreDiscountComponent>(storeId, out var discountsComponent))
        {
            return;
        }

        var discounts = discountsComponent.Discounts;
        var discountData = discounts.FirstOrDefault(x => x.ListingId == purchasedItemId);
        if (discountData == null)
        {
            return;
        }

        if (discountData.Count > 0)
        {
            discountData.Count--;
            if (discountData.Count == 0)
            {
                listingData.CostModifiersBySourceId.Remove(discountData.DiscountCategory);
            }
        }
    }

    /// <summary> Initialized discounts if required. </summary>
    private void OnStoreInitialized(ref StoreInitializedEvent ev)
    {
        if (!ev.UseDiscounts)
        {
            return;
        }

        if (!TryComp<StoreComponent>(ev.Store, out _))
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
        // Get list of categories with cumulative weights.
        // for example if we have categories with weights 2, 18 and 80
        // list of cumulative ones will be 2,20,100 (with 100 being total).
        // Then roll amount of unique listing items to be discounted under
        // each category, and after that - roll exact items in categories
        // and their cost

        var prototypes = _prototypeManager.EnumeratePrototypes<DiscountCategoryPrototype>();
        var categoriesWithCumulativeWeight = new CategoriesWithCumulativeWeight(prototypes);
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
    /// <param name="categoriesWithCumulativeWeight">
    /// Map of discount category cumulative weights by respective protoId of discount category.
    /// </param>
    /// <returns>Map: <b>count</b> of different listing items to be discounted, by discount category.</returns>
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

    /// <summary>
    /// Rolls list of exact <see cref="ListingData"/> items to be discounted, and amount of currency to be discounted.
    /// </summary>
    /// <param name="listings">List of all available listing items from which discounted ones could be selected.</param>
    /// <param name="chosenDiscounts"></param>
    /// <returns>Collection of containers with rolled discount data.</returns>
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
        ListingData listingData
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

/// <summary>
/// Event of store items being initialized before usage.
/// This event is supposed to help refine listing items - add discounts, refine categories, etc
/// </summary>
public sealed class ListingItemsInitializingEvent
{
    public ListingItemsInitializingEvent(IReadOnlyCollection<ListingDataWithCostModifiers> listingData, EntityUid? store)
    {
        ListingData = listingData;
        Store = store;
    }

    public ListingItemsInitializingEvent(ListingDataWithCostModifiers listingData, EntityUid? store)
    {
        ListingData = new HashSet<ListingDataWithCostModifiers> { listingData };
        Store = store;
    }

    /// <summary> List of items to initialize. </summary>
    public IReadOnlyCollection<ListingDataWithCostModifiers> ListingData { get; }

    /// <summary> EntityUid of store entity. </summary>
    public EntityUid? Store { get; }
}

