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
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly DiscountSettings _discountSettings = new();
    
    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreInitializedEvent>(OnStoreInitialized);
        SubscribeLocalEvent<StoreBuyAttemptEvent>(OnBuyRequest);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnBuyFinished);
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
        var availableListings = _store.GetAvailableListings(ev.TargetUser, store.Listings, store.Categories, null);
        discountComponent.Discounts = InitializeDiscounts(availableListings, _discountSettings);
    }

    private StoreDiscountData[] InitializeDiscounts(
        IEnumerable<ListingData> listings,
        DiscountSettings settings
    )
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

        var chosenDiscounts = new Dictionary<ProtoId<DiscountCategoryPrototype>, int>();
        for (var i = 0; i < settings.TotalAvailableDiscounts; i++)
        {
            var roll = _random.Next(cumulativeWeight);
            for (var discountCategoryIndex = 0; discountCategoryIndex < discountCumulativeWeightByDiscountCategoryId.Count; discountCategoryIndex++)
            {
                var container = discountCumulativeWeightByDiscountCategoryId[discountCategoryIndex];
                if (roll <= container.CumulativeWeight)
                {
                    continue;
                }

                if (!chosenDiscounts.TryGetValue(container.DiscountCategory.ID, out var alreadySelectedCount))
                {
                    chosenDiscounts[container.DiscountCategory.ID] = 1;
                }
                else if(alreadySelectedCount < container.DiscountCategory.MaxItems)
                {
                    var newDiscountCount = chosenDiscounts[container.DiscountCategory.ID] + 1;
                    chosenDiscounts[container.DiscountCategory.ID] = newDiscountCount;
                    if (newDiscountCount == container.DiscountCategory.MaxItems)
                    {
                        discountCumulativeWeightByDiscountCategoryId.Remove(container);
                    }
                    break;
                }
            }
        }

        var listingsByDiscountCategory = listings.Where(x => x.DiscountDownTo?.Count > 0)
                                                 .GroupBy(x => x.DiscountCategory)
                                                 .ToDictionary(
                                                     x => x.Key,
                                                     x => x.ToArray()
                                                 );
        var list = new List<StoreDiscountData>();
        foreach (var (discountCategory, itemsCount) in chosenDiscounts)
        {
            if (itemsCount == 0)
            {
                continue;
            }

            if (!listingsByDiscountCategory.TryGetValue(discountCategory, out var itemsForDiscount))
            {
                continue;
            }

            var chosen = _random.GetItems(itemsForDiscount, itemsCount, allowDuplicates: false);
            foreach (var listingData in chosen)
            {
                var cost = listingData.Cost;
                var discountAmountByCurrencyId = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>();
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

                var discountData = new StoreDiscountData
                {
                    ListingId = listingData.ID,
                    Count = 1,
                    DiscountAmountByCurrency = discountAmountByCurrencyId
                };
                list.Add(discountData);
            }
        }

        return list.ToArray();
    }

    private sealed class DiscountCategoryWithCumulativeWeight(DiscountCategoryPrototype discountCategory, int cumulativeWeight)
    {
        public DiscountCategoryPrototype DiscountCategory { get; set; } = discountCategory;
        public int CumulativeWeight { get; set; } = cumulativeWeight;
    }

    /// <summary>
    /// Settings for discount initializations.
    /// </summary>
    public sealed class DiscountSettings
    {
        /// <summary>
        /// Total count of discounts that can be attached to uplink.
        /// </summary>
        public int TotalAvailableDiscounts { get; set; } = 3;
    }

}
