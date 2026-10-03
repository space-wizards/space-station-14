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
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    protected override void Started(Entity<VendorMalfunctionRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        // ensure that we aren't affecting a structure/grid which is blacklisted by this rule
        if (!Station.TryGetRandomStation<StationEventEligibleComponent>(
            out var chosenStation,
            uid => _whitelist.IsWhitelistFailOrNull(ent.Comp1.Blacklist, uid)))
            return;

        // create a list of all vending machines on the target station which have power and aren't broken
        var vendingMachines = new List<Entity<VendingMachineComponent>>();
        var query = EntityQueryEnumerator<VendingMachineComponent, TransformComponent>();
        while (query.MoveNext(out var vendUid, out var vendor, out var xform))
        {
            if (vendor.Broken || !_vendingSystem.IsPowered(vendUid, EntityManager)) continue;
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station == chosenStation.Value.Owner)
            {
                vendingMachines.Add((vendUid, vendor));
            }
        }

        // work out how many machines we want to hit
        var toDispense = Math.Min(ent.Comp1.AffectedMachines.Next(RobustRandom), vendingMachines.Count);
        if (toDispense == 0)
            return;

        // we will hit the first `n=toDispense` machines in our list; shuffling the list ensures we hit a random subset of them.
        RobustRandom.Shuffle(vendingMachines);

        // this performs the necessary actions (contraband inventory/item ejection) on the affected machines.
        for (var i = 0; i < toDispense; i++)
        {
            var vendor = vendingMachines[i];

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
