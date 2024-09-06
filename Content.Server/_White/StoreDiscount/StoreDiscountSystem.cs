using System.Linq;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._White.StoreDiscount;

public sealed class StoreDiscountSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public void ApplyDiscounts(IEnumerable<ListingData> listings, StoreComponent store)
    {
        if (!store.Sales.Enabled)
            return;

        var count = _random.Next(store.Sales.MinItems, store.Sales.MaxItems + 1);

        listings = listings
            .Where(l => !l.SaleBlacklist && l.Cost.Any(x => x.Value > 1) && store.Categories.Overlaps(l.Categories)) // goob edit
            .OrderBy(_ => _random.Next()).Take(count).ToList();

        foreach (var listing in listings)
        {
            var sale = GetDiscount(store.Sales.MinMultiplier, store.Sales.MaxMultiplier);
            var newCost = listing.Cost.ToDictionary(x => x.Key,
                x => FixedPoint2.New(Math.Max(1, (int) MathF.Round(x.Value.Float() * sale))));

            if (listing.Cost.All(x => x.Value.Int() == newCost[x.Key].Int()))
                continue;

            var key = listing.Cost.First(x => x.Value > 0).Key;
            listing.OldCost = listing.Cost;
            listing.DiscountValue = 100 - (newCost[key] / listing.Cost[key] * 100).Int();
            listing.Cost = newCost;
            listing.Categories = new() { store.Sales.SalesCategory };
        }
    }

    private float GetDiscount(float minMultiplier, float maxMultiplier)
    {
        return _random.NextFloat() * (maxMultiplier - minMultiplier) + minMultiplier;
    }
}
