using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Server.StationEvents.Components;
using Content.Server.VendingMachines;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared.VendingMachines.Components;
using Content.Shared.Whitelist;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that cause some vending machines to eject some of their contents.
/// </summary>
/// <seealso cref="VendorMalfunctionRuleComponent"/>
public sealed partial class VendorMalfunctionRule : StationEventSystem<VendorMalfunctionRuleComponent>
{
    [Dependency] private VendingMachineSystem _vendingSystem = default!;

    protected override void Started(Entity<VendorMalfunctionRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        // work out how many machines we want to hit
        var toDispense = ent.Comp1.AffectedMachines.Next(RobustRandom);
        if (toDispense == 0)
            return;

        // get a list of all vending machines on the target station, and then shuffle it.
        var vendingMachines = Station.GetEntitiesWithComponentOnStation<VendingMachineComponent>(true).ToList();
        RobustRandom.Shuffle(vendingMachines);

        // target number of vending machines to hit is `toDispense`.
        // this loop goes through the shuffled list of vending machines, and hits the first `toDispense` hittable machines,
        // skipping any unpowered/broken machines.
        foreach (var vendor in vendingMachines)
        {
            // give up if we've reached our quota of machines to hit.
            if (toDispense == 0)
                break;

            // make sure the vendor is powered and isn't broken
            if (vendor.Comp.Broken || !_vendingSystem.IsPowered(vendor, EntityManager)) continue;

            // this vending machine has been hit (even if nothing ultimately happens), decrement toDispense.
            toDispense--;

            // roll to enable contraband inventory
            if (RobustRandom.NextDouble() < ent.Comp1.ContrabandChance)
                _vendingSystem.SetContraband(vendor, true);

            // roll for the quantity of items to eject, skip if we roll 0
            var toEject = ent.Comp1.ItemsToEject.Next(RobustRandom);
            if (toEject <= 0)
                continue;
            _vendingSystem.EjectRandom(vendor.AsNullable(), true); // ensures the vending noise plays for the first ejected item
            for (var j = 1; j < toEject; j++)
            {
                _vendingSystem.EjectRandom(vendor.AsNullable(), true, true); // handles ejecting the other items
            }
        }
    }
}
