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

        if (!Station.TryGetRandomStation<StationEventEligibleComponent>(
            out var chosenStation,
            uid => _whitelist.IsWhitelistFailOrNull(ent.Comp1.Blacklist, uid)))
            return;

        var vendingMachines = new List<Entity<VendingMachineComponent>>();
        var query = EntityQueryEnumerator<VendingMachineComponent, TransformComponent>();
        while (query.MoveNext(out var vendUid, out var vendor, out var xform))
        {
            if ((!vendor.Broken) && _vendingSystem.IsPowered(vendUid, EntityManager) && CompOrNull<StationMemberComponent>(xform.GridUid)?.Station == chosenStation.Value.Owner)
            {
                vendingMachines.Add((vendUid, vendor));
            }
        }

        var toDispense = Math.Min(RobustRandom.Next(ent.Comp1.MinimumAffected, ent.Comp1.MaximumAffected), vendingMachines.Count);
        if (toDispense == 0)
            return;

        RobustRandom.Shuffle(vendingMachines);

        for (var i = 0; i < toDispense; i++)
        {
            var vendor = vendingMachines[i];
            if (RobustRandom.NextDouble() < ent.Comp1.ContrabandChance)
            {
                _vendingSystem.SetContraband(vendor, true);
            }
            _vendingSystem.EjectRandom(vendor.AsNullable(), true);
        }
    }
}
