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

        // this loop goes through the shuffled list of vending machines, and hits the first `toDispense` hittable machines,
        // skipping any unpowered/broken machines (stops if there are zero `toDispense` remaining).
        // Rolls to toggle contra inventory, rolls number of items to eject + ejects desired item quantity.
        foreach (var vendor in vendingMachines)
        {
            if (toDispense == 0)
                break;

            if (vendor.Comp.Broken || !_vendingSystem.IsPowered(vendor, EntityManager)) continue;

            toDispense--;

            if (RobustRandom.NextDouble() < ent.Comp1.ContrabandChance)
                _vendingSystem.SetContraband(vendor, true);

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
