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

        var toDispense = ent.Comp1.AffectedMachines.Next(RobustRandom);
        if (toDispense <= 0)
        {
            Log.Error($"Gamerule {ToPrettyString(ent)} selected an invalid number of machines to affect: {toDispense}");
            return;
        }

        // get list of all vending machines on the target station, and then shuffle it so we get a random sample later on
        var vendingMachines = Station.GetEntitiesWithComponentOnStation<VendingMachineComponent>(true).ToList();
        RobustRandom.Shuffle(vendingMachines);

        // finds valid vending machines (powered + not broken), maybe toggles contra inventory, ejects some contents
        foreach (var vendor in vendingMachines)
        {
            if (toDispense == 0)
                break;

            if (vendor.Comp.Broken || !_vendingSystem.IsPowered(vendor, EntityManager))
                continue;

            toDispense--;

            if (RobustRandom.NextDouble() < ent.Comp1.ContrabandChance)
                _vendingSystem.SetContraband(vendor, true);

            var toEject = ent.Comp1.ItemsToEject.Next(RobustRandom);
            if (toEject <= 0)
                continue;

            _vendingSystem.EjectRandom(vendor.AsNullable(), true); // ensures the vending noise plays for the first ejected item
            for (var j = 1; j < toEject; j++)
            {
                _vendingSystem.EjectRandom(vendor.AsNullable(), true, true);
            }
        }
    }
}
