using System.Linq;
using Content.Server.Store.Systems;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.StoreDiscount.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StoreDiscount.Systems;

/// <summary>
/// Populates the Second Hand tab of an uplink with a random selection of worn/damaged syndicate items.
/// Mirrors the structure of <see cref="StoreDiscountSystem"/> but for second-hand item listings.
/// </summary>
public sealed class SecondHandSystem : EntitySystem
{
    private static readonly ProtoId<StoreCategoryPrototype> SecondHandStoreCategoryKey = "SecondHandItems";
    // Number of second-hand items shown per uplink per round.
    private const int MinSecondHandItems = 8;
    private const int MaxSecondHandItems = 14;

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreInitializedEvent>(OnStoreInitialized);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnBuyFinished);
    }

    /// <summary>
    /// Removes a second-hand listing from the tab once its purchase count is exhausted.
    /// Second-hand items are one-per-uplink; the category is stripped so the listing disappears.
    /// </summary>
    private void OnBuyFinished(ref StoreBuyFinishedEvent ev)
    {
        var (storeId, purchasedItem) = ev;
        if (!TryComp<StoreSecondHandComponent>(storeId, out var secondHandComp))
            return;

        if (!TryGetSecondHandData(secondHandComp.SecondHandItems, purchasedItem, out var data) || data.Count == 0)
            return;

        data.Count--;
        if (data.Count > 0)
            return;

        // Remove the SecondHandItems category from this listing so it disappears from the tab.
        purchasedItem.Categories.Remove(SecondHandStoreCategoryKey);
    }

    /// <summary>
    /// Populates the Second Hand tab when the store is initialized, if second-hand items are enabled for this uplink.
    /// </summary>
    private void OnStoreInitialized(ref StoreInitializedEvent ev)
    {
        if (!ev.UseSecondHand)
            return;

        if (!TryComp<StoreComponent>(ev.Store, out var storeComp))
            return;

        var secondHandComp = EnsureComp<StoreSecondHandComponent>(ev.Store);
        var selectedItems = SelectSecondHandItems(storeComp.FullListingsCatalog);

        if (selectedItems.Count == 0)
            return;

        ApplySecondHandItems(storeComp, selectedItems);
        secondHandComp.SecondHandItems = selectedItems;
    }

    /// <summary>
    /// Selects a random set of second-hand listings from the full catalog using weighted category sampling.
    /// Total item count is rolled uniformly in [<see cref="MinSecondHandItems"/>, <see cref="MaxSecondHandItems"/>].
    /// </summary>
    private IReadOnlyList<StoreSecondHandData> SelectSecondHandItems(
        IReadOnlyCollection<ListingDataWithCostModifiers> fullCatalog)
    {
        var prototypes = _prototypeManager.EnumeratePrototypes<SecondHandCategoryPrototype>();
        var categoryWeightMap = new CumulativeWeightMap<SecondHandCategoryPrototype>(prototypes);

        var totalItems = _random.Next(MinSecondHandItems, MaxSecondHandItems + 1);
        var countByCategory = PickCategoriesToRoll(totalItems, categoryWeightMap);
        return RollItems(fullCatalog, countByCategory);
    }

    /// <summary>
    /// Rolls <paramref name="totalItems"/> category slots using weighted random selection.
    /// A category is removed from the pool once it hits its <see cref="SecondHandCategoryPrototype.MaxItems"/> cap,
    /// so remaining rolls redistribute to the other categories automatically.
    /// </summary>
    private Dictionary<ProtoId<SecondHandCategoryPrototype>, int> PickCategoriesToRoll(
        int totalItems,
        CumulativeWeightMap<SecondHandCategoryPrototype> weightMap)
    {
        var chosen = new Dictionary<ProtoId<SecondHandCategoryPrototype>, int>();
        for (var i = 0; i < totalItems; i++)
        {
            var category = weightMap.RollCategory(_random);
            if (category == null)
                break;

            chosen.TryGetValue(category.ID, out var existing);
            var newCount = existing + 1;
            chosen[category.ID] = newCount;

            if (newCount >= category.MaxItems)
                weightMap.Remove(category);
        }

        return chosen;
    }

    /// <summary>
    /// Picks specific listings from each category's pool using the counts determined by <see cref="PickCategoriesToRoll"/>.
    /// If a category has fewer available listings than requested, all available listings are used.
    /// </summary>
    private IReadOnlyList<StoreSecondHandData> RollItems(
        IEnumerable<ListingDataWithCostModifiers> fullCatalog,
        Dictionary<ProtoId<SecondHandCategoryPrototype>, int> countByCategory)
    {
        var byCategory = GroupSecondHandListingsByCategory(fullCatalog);

        var result = new List<StoreSecondHandData>();
        foreach (var (categoryId, count) in countByCategory)
        {
            if (!byCategory.TryGetValue(categoryId, out var pool))
                continue;

            var actualCount = Math.Min(count, pool.Count);
            var chosen = _random.GetItems(pool, actualCount, allowDuplicates: false);
            foreach (var listing in chosen)
            {
                result.Add(new StoreSecondHandData
                {
                    ListingId = listing.ID,
                    Count = 1,
                    SecondHandCategory = listing.SecondHandCategory!.Value,
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Makes selected listings visible in the Second Hand tab and enables the tab on the store.
    /// </summary>
    private void ApplySecondHandItems(
        StoreComponent storeComp,
        IReadOnlyList<StoreSecondHandData> selectedItems)
    {
        storeComp.Categories.Add(SecondHandStoreCategoryKey);

        foreach (var data in selectedItems)
        {
            ListingDataWithCostModifiers? listing = null;
            foreach (var entry in storeComp.FullListingsCatalog)
            {
                if (entry.ID == data.ListingId)
                {
                    listing = entry;
                    break;
                }
            }

            if (listing == null)
            {
                Log.Warning($"SecondHandSystem: Could not find listing '{data.ListingId}' in catalog.");
                continue;
            }

            listing.Categories.Add(SecondHandStoreCategoryKey);
        }
    }

    /// <summary>
    /// Groups all second-hand-eligible listings from the catalog by their <see cref="SecondHandCategoryPrototype"/> ID.
    /// Listings without a <c>SecondHandCategory</c> are skipped.
    /// </summary>
    private static Dictionary<ProtoId<SecondHandCategoryPrototype>, List<ListingDataWithCostModifiers>>
        GroupSecondHandListingsByCategory(IEnumerable<ListingDataWithCostModifiers> catalog)
    {
        var grouped = new Dictionary<ProtoId<SecondHandCategoryPrototype>, List<ListingDataWithCostModifiers>>();
        foreach (var listing in catalog)
        {
            if (listing.SecondHandCategory == null)
                continue;

            if (!grouped.TryGetValue(listing.SecondHandCategory.Value, out var list))
            {
                list = new List<ListingDataWithCostModifiers>();
                grouped[listing.SecondHandCategory.Value] = list;
            }

            list.Add(listing);
        }

        return grouped;
    }

    /// <summary>
    /// Finds the <see cref="StoreSecondHandData"/> entry for a purchased listing, if it was a second-hand item.
    /// </summary>
    private static bool TryGetSecondHandData(
        IReadOnlyList<StoreSecondHandData> items,
        ListingDataWithCostModifiers purchased,
        out StoreSecondHandData data)
    {
        foreach (var item in items)
        {
            if (item.ListingId == purchased.ID)
            {
                data = item;
                return true;
            }
        }

        data = null!;
        return false;
    }

}
