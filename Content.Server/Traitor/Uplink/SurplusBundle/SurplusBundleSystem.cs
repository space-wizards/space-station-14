using System.Linq;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.PDA;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Traitor.Uplink.SurplusBundle;

public sealed class SurplusBundleSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;

    private UplinkStoreListingPrototype[] _uplinks = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurplusBundleComponent, MapInitEvent>(OnMapInit);

        InitList();
    }

    private void InitList()
    {
        // sort data in price descending order
        _uplinks = _prototypeManager.EnumeratePrototypes<UplinkStoreListingPrototype>()
            .Where(item => item.CanSurplus).ToArray();
        Array.Sort(_uplinks, (a, b) => b.Price - a.Price);
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
            var ent = EntityManager.SpawnEntity(item.ItemId, cords);
            _entityStorage.Insert(ent, component.Owner);
        }
    }

    // wow, is this leetcode reference?
    private List<UplinkStoreListingPrototype> GetRandomContent(int targetCost)
    {
        var ret = new List<UplinkStoreListingPrototype>();
        if (_uplinks.Length == 0)
            return ret;

        var totalCost = 0;
        var index = 0;
        while (totalCost < targetCost)
        {
            // All data is sorted in price descending order
            // Find new item with the lowest acceptable price
            // All expansive items will be before index, all acceptable after
            var remainingBudget = targetCost - totalCost;
            while (_uplinks[index].Price > remainingBudget)
            {
                index++;
                if (index >= _uplinks.Length)
                {
                    // Looks like no cheap items left
                    // It shouldn't be case for ss14 content
                    // Because there are 1 TC items
                    return ret;
                }
            }

            // Select random listing and add into crate
            var randomIndex = _random.Next(index, _uplinks.Length);
            var randomItem = _uplinks[randomIndex];
            ret.Add(randomItem);
            totalCost += randomItem.Price;
        }

        return ret;
    }
}
