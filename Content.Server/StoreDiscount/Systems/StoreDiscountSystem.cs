using System.Linq;
using Content.Server.Store.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.StoreDiscount.Components;
using Robust.Shared.Random;

namespace Content.Server.StoreDiscount.Systems;

public sealed class StoreDiscountSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    private readonly DiscountSettings _discountSettings = new();

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreInitializedEvent>(OnUplinkInitialized);
    }

    private void OnUplinkInitialized(ref StoreInitializedEvent ev)
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
        IEnumerable<ListingData> storeComponent,
        DiscountSettings settings
    )
    {
        var listingsByDiscountCategory = storeComponent.Where(x => x.DiscountDownTo?.Count > 0)
                                                       .GroupBy(x => x.DiscountCategory)
                                                       .ToDictionary(
                                                           x => x.Key,
                                                           x => x.ToArray()
                                                       );
        var chosenDiscounts = new Dictionary<DiscountCategory, int>
        {
            [DiscountCategory.RareDiscounts] = 0,
            [DiscountCategory.UsualDiscounts] = 0,
            [DiscountCategory.VeryRareDiscounts] = 0,
        };
        var veryRareDiscountCount = 0;
        var rareDiscountCount = 0;
        for (var i = 0; i < settings.TotalAvailableDiscounts; i++)
        {
            var roll = _random.Next(100);

            if (roll <= settings.VeryRareDiscountChancePercent)
            {
                chosenDiscounts[DiscountCategory.VeryRareDiscounts]++;
                if (veryRareDiscountCount >= settings.MaxVeryRareDiscounts)
                {
                    chosenDiscounts[DiscountCategory.UsualDiscounts]++;
                }
                else
                {
                    veryRareDiscountCount++;
                }
            }
            else if (roll <= settings.RareDiscountChancePercent)
            {
                if (rareDiscountCount <= settings.MaxRareDiscounts)
                {
                    chosenDiscounts[DiscountCategory.RareDiscounts]++;
                    rareDiscountCount++;
                }
                else
                {
                    chosenDiscounts[DiscountCategory.UsualDiscounts]++;
                }
            }
            else
            {
                chosenDiscounts[DiscountCategory.UsualDiscounts]++;
            }
        }

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
                var discountAmountByCurrencyId = new Dictionary<string, FixedPoint2>();
                foreach (var kvp in cost)
                {
                    if (!listingData.DiscountDownTo.TryGetValue(kvp.Key, out var discountUntilValue))
                    {
                        continue;
                    }

                    var discountUntilRolledValue =
                        _random.NextDouble(discountUntilValue.Double(), kvp.Value.Double());
                    var leftover = discountUntilRolledValue % 1;
                    var discountedCost = kvp.Value - (discountUntilRolledValue - leftover);

                    discountAmountByCurrencyId.Add(kvp.Key.Id, discountedCost);
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

    /// <summary>
    /// Settings for discount initializations.
    /// </summary>
    public sealed class DiscountSettings
    {
        /// <summary>
        /// Total count of discounts that can be attached to uplink.
        /// </summary>
        public int TotalAvailableDiscounts { get; set; } = 3;

        /// <summary>
        /// Maximum count of category 2 (not cheap stuff) items to be discounted.
        /// </summary>
        public int MaxVeryRareDiscounts { get; set; } = 1;

        /// <summary>
        /// Maximum count of category 0 (very low-costing stuff) items to be discounted.
        /// </summary>
        public int MaxRareDiscounts { get; set; } = 2;

        /// <summary>
        /// % chance (out of 100) to roll discount on listing item with category <see cref="DiscountCategory.RareDiscounts"/>.
        /// Is considered only after comparing roll result with <see cref="VeryRareDiscountChancePercent"/>.
        /// </summary>
        public int RareDiscountChancePercent { get; set; } = 20;

        /// <summary>
        /// % chance (out of 100) to roll discount on listing item with category <see cref="DiscountCategory.VeryRareDiscounts"/>. 
        /// </summary>
        public int VeryRareDiscountChancePercent { get; set; } = 2;
    }
}
