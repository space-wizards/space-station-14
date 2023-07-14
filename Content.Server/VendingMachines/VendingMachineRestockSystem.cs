using System.Linq;
using Content.Server.Cargo.Systems;
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.VendingMachines;

public sealed class VendingMachineRestockSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VendingMachineRestockComponent, PriceCalculationEvent>(OnPriceCalculation);
    }

    private void OnPriceCalculation(EntityUid uid, VendingMachineRestockComponent component, ref PriceCalculationEvent args)
    {
        List<double> priceSets = new();

        // Find the most expensive inventory and use that as the highest price.
        foreach (var vendingInventory in component.CanRestock)
        {
            double total = 0;

            if (_prototypeManager.TryIndex(vendingInventory, out VendingMachineInventoryPrototype? inventoryPrototype))
            {
                foreach (var (item, amount) in inventoryPrototype.Inventory)
                {
                    if (_prototypeManager.TryIndex(item, out EntityPrototype? entity))
                        total += _pricing.GetEstimatedPrice(entity) * amount;
                }
            }

            priceSets.Add(total);
        }

        args.Price += priceSets.Max();
    }
}
