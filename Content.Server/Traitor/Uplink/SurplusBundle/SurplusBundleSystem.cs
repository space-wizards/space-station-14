using System.Linq;
using Content.Server.Storage.EntitySystems;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Random;

namespace Content.Server.Traitor.Uplink.SurplusBundle;

public sealed class SurplusBundleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurplusBundleComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SurplusBundleComponent component, MapInitEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        FillStorage((uid, component, store));
    }

    private void FillStorage(Entity<SurplusBundleComponent, StoreComponent> ent)
    {
        var cords = Transform(ent).Coordinates;
        var content = GetRandomContent(ent);
        foreach (var item in content)
        {
            var dode = Spawn(item.ProductEntity, cords);
            _entityStorage.Insert(dode, ent);
        }
    }

    // wow, is this leetcode reference?
    private List<ListingData> GetRandomContent(Entity<SurplusBundleComponent, StoreComponent> ent)
    {
        var ret = new List<ListingData>();

        var listings = _store.GetAvailableListings(ent, null, ent.Comp2.Categories)
            .OrderBy(p => p.Cost.Values.Sum())
            .ToList();

        if (listings.Count == 0)
            return ret;

        var totalCost = FixedPoint2.Zero;
        var index = 0;
        while (totalCost < ent.Comp1.TotalPrice)
        {
            // All data is sorted in price descending order
            // Find new item with the lowest acceptable price
            // All expansive items will be before index, all acceptable after
            var remainingBudget = ent.Comp1.TotalPrice - totalCost;
            while (listings[index].Cost.Values.Sum() > remainingBudget)
            {
                index++;
                if (index >= listings.Count)
                {
                    // Looks like no cheap items left
                    // It shouldn't be case for ss14 content
                    // Because there are 1 TC items
                    return ret;
                }
            }

            // Select random listing and add into crate
            var randomIndex = _random.Next(index, listings.Count);
            var randomItem = listings[randomIndex];
            ret.Add(randomItem);
            totalCost += randomItem.Cost.Values.Sum();
        }

        return ret;
    }
}
