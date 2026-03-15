using System.Linq;
using Content.Server.Store.Systems;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.StoreDiscount.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StoreDiscount.Systems;

/// Populates the Second Hand tab of an uplink with a random selection of worn/damaged syndicate items.
/// Mirrors the structure of <see cref="StoreDiscountSystem"/> but for second-hand item listings.
public sealed class SecondHandSystem : EntitySystem
{
    private static readonly ProtoId<StoreCategoryPrototype> SecondHandStoreCategoryKey = "SecondHandItems";
    private const int MinSecondHandItems = 5;
    private const int MaxSecondHandItems = 10;

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreInitializedEvent>(OnStoreInitialized);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnBuyFinished);
    }

    /// Removes a second-hand item from the tab after it is purchased (one-purchase-only).
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

    /// Populates the Second Hand tab if the store was initialized with second-hand items enabled.
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

    /// Selects a weighted random set of second-hand listings from the full catalog.
    private IReadOnlyList<StoreSecondHandData> SelectSecondHandItems(
        IReadOnlyCollection<ListingDataWithCostModifiers> fullCatalog)
    {
        var prototypes = _prototypeManager.EnumeratePrototypes<SecondHandCategoryPrototype>();
        var categoryWeightMap = new SecondHandCategoryWeightMap(prototypes);

        var totalItems = _random.Next(MinSecondHandItems, MaxSecondHandItems + 1);
        var countByCategory = PickCategoriesToRoll(totalItems, categoryWeightMap);
        return RollItems(fullCatalog, countByCategory);
    }

    /// Determines how many items to draw from each second-hand category using weighted random selection.
    private Dictionary<ProtoId<SecondHandCategoryPrototype>, int> PickCategoriesToRoll(
        int totalItems,
        SecondHandCategoryWeightMap weightMap)
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

    /// Picks the specific listings to activate for each category.
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

    /// Adds the SecondHandItems category to the selected listings and to the store's category whitelist.
    private void ApplySecondHandItems(
        StoreComponent storeComp,
        IReadOnlyList<StoreSecondHandData> selectedItems)
    {
        // Allow the store to show the SecondHandItems tab.
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

    /// Weighted category selection map, mirrors <see cref="StoreDiscountSystem"/>'s inner class.
    private sealed record SecondHandCategoryWeightMap
    {
        private readonly List<SecondHandCategoryPrototype> _categories;
        private readonly List<int> _weights;
        private int _totalWeight;

        public SecondHandCategoryWeightMap(IEnumerable<SecondHandCategoryPrototype> prototypes)
        {
            var asArray = prototypes.ToArray();
            _categories = new(asArray.Length);
            _weights = new(asArray.Length);
            _totalWeight = 0;

            foreach (var category in asArray)
            {
                if (category.MaxItems <= 0 || category.Weight <= 0)
                    continue;

                _totalWeight += category.Weight;
                _categories.Add(category);
                _weights.Add(_totalWeight);
            }
        }

        public void Remove(SecondHandCategoryPrototype category)
        {
            var index = _categories.IndexOf(category);
            if (index == -1)
                return;

            for (var i = index + 1; i < _categories.Count; i++)
                _weights[i] -= category.Weight;

            _totalWeight -= category.Weight;
            _categories.RemoveAt(index);
            _weights.RemoveAt(index);
        }

        public SecondHandCategoryPrototype? RollCategory(IRobustRandom random)
        {
            if (_totalWeight <= 0)
                return null;

            var roll = random.Next(_totalWeight);
            for (var i = 0; i < _weights.Count; i++)
            {
                if (roll < _weights[i])
                    return _categories[i];
            }

            return null;
        }
    }
}
