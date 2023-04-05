using System.Linq;
using Content.Server.Store.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Store;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Traitor.Uplink.SurplusBundle;

public sealed class SurplusBundleSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    private ListingData[] _listings = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurplusBundleComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<SurplusBundleComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, SurplusBundleComponent component, ComponentInit args)
    {
        var storePreset = _prototypeManager.Index<StorePresetPrototype>(component.StorePreset);

        _listings = _store.GetAvailableListings(uid, null, storePreset.Categories).ToArray();

        Array.Sort(_listings, (a, b) => (int) (b.Cost.Values.Sum() - a.Cost.Values.Sum())); //this might get weird with multicurrency but don't think about it
    }

    private void OnMapInit(EntityUid uid, SurplusBundleComponent component, MapInitEvent args)
    {
        FillStorage(uid, component);
    }

    private void FillStorage(EntityUid uid, SurplusBundleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var cords = Transform(uid).Coordinates;

        var content = GetRandomContent(component.TotalPrice);
        foreach (var item in content)
        {
            var ent = EntityManager.SpawnEntity(item.ProductEntity, cords);
            _entityStorage.Insert(ent, component.Owner);
        }
    }

    // wow, is this leetcode reference?
    private List<ListingData> GetRandomContent(FixedPoint2 targetCost)
    {
        var ret = new List<ListingData>();
        if (_listings.Length == 0)
            return ret;

        var totalCost = FixedPoint2.Zero;
        var index = 0;
        while (totalCost < targetCost)
        {
            // All data is sorted in price descending order
            // Find new item with the lowest acceptable price
            // All expansive items will be before index, all acceptable after
            var remainingBudget = targetCost - totalCost;
            while (_listings[index].Cost.Values.Sum() > remainingBudget)
            {
                index++;
                if (index >= _listings.Length)
                {
                    // Looks like no cheap items left
                    // It shouldn't be case for ss14 content
                    // Because there are 1 TC items
                    return ret;
                }
            }

            // Select random listing and add into crate
            var randomIndex = _random.Next(index, _listings.Length);
            var randomItem = _listings[randomIndex];
            ret.Add(randomItem);
            totalCost += randomItem.Cost.Values.Sum();
        }

        return ret;
    }
}
