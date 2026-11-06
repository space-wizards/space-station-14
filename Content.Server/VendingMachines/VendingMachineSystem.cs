using System.Linq;
using System.Numerics;
using Content.Server.Cargo.Systems;
using Content.Server.VendingMachines.Components;
using Content.Server.Vocalization.Systems;
using Content.Shared.Cargo;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Throwing;
using Content.Shared.VendingMachines;
using Content.Shared.VendingMachines.Components;
using Content.Shared.Wall;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.VendingMachines;

public sealed partial class VendingMachineSystem : SharedVendingMachineSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private PricingSystem _pricing = default!;
    [Dependency] private ThrowingSystem _throwingSystem = default!;

    private const float WallVendEjectDistanceFromWall = 1f;

    protected override bool ShouldThrowVendItem(Entity<VendingMachineEjectComponent> entity)
    {
        return HasComp<VendingMachineShootComponent>(entity.Owner);
    }

    protected override void EjectItem(Entity<VendingMachineComponent?, VendingMachineEjectComponent?> entity, bool forceEject = false)
    {
        if (!Resolve(entity.Owner, ref entity.Comp1, ref entity.Comp2))
            return;

        var uid = entity.Owner;
        var ejectComponent = entity.Comp2;

        if (ejectComponent.NextItemToEject is not { } item)
        {
            ejectComponent.ThrowNextItem = false;
            return;
        }

        // Default spawn coordinates
        var xform = Transform(uid);
        var spawnCoordinates = xform.Coordinates;

        //Make sure the wallvends spawn outside of the wall.
        if (TryComp<WallMountComponent>(uid, out var wallMountComponent))
        {
            var offset = (wallMountComponent.Direction + xform.LocalRotation - Math.PI / 2).ToVec() * WallVendEjectDistanceFromWall;
            spawnCoordinates = spawnCoordinates.Offset(offset);
        }

        var ent = Spawn(item, spawnCoordinates);

        if (ejectComponent.ThrowNextItem)
        {
            var range = ejectComponent.NonLimitedEjectRange;
            var direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
            _throwingSystem.TryThrow(ent, direction, ejectComponent.NonLimitedEjectForce);
        }

        ejectComponent.NextItemToEject = null;
        ejectComponent.ThrowNextItem = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = Timing.CurTime;
        var dispenseOnHitQuery = EntityQueryEnumerator<VendingMachineDispenseOnHitComponent>();
        while (dispenseOnHitQuery.MoveNext(out _, out var dispenseOnHit))
        {
            if (dispenseOnHit.NextDispenseTime is not { } nextDispenseTime || curTime <= nextDispenseTime)
                continue;

            dispenseOnHit.NextDispenseTime = null;
        }

        var disabled = EntityQueryEnumerator<EmpDisabledComponent, VendingMachineComponent, VendingMachineEjectComponent>();
        while (disabled.MoveNext(out var uid, out _, out var comp, out var eject))
        {
            if (eject.NextEmpEject >= curTime) continue;

            EjectRandom((uid, comp, eject), true, false);
            eject.NextEmpEject += (5 * eject.EjectDelay);
        }
    }

    [SubscribeLocalEvent]
    private void OnVendingPrice(Entity<VendingMachineComponent> entity, ref PriceCalculationEvent args)
    {
        var price = 0.0;

        foreach (var entry in entity.Comp.Inventory.Values)
        {
            if (!ProtoMan.TryIndex<EntityPrototype>(entry.ID, out var proto))
            {
                Log.Error($"Unable to find entity prototype {entry.ID} on {ToPrettyString(entity)} vending.");
                continue;
            }

            price += entry.Amount * _pricing.GetEstimatedPrice(proto);
        }

        args.Price += price;
    }

    [SubscribeLocalEvent]
    private void OnDamageChanged(Entity<VendingMachineComponent> entity, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased && entity.Comp.Broken)
        {
            entity.Comp.Broken = false;
            Dirty(entity);
            return;
        }

        if (!TryComp<VendingMachineDispenseOnHitComponent>(entity.Owner, out var dispenseOnHit))
            return;

        if (entity.Comp.Broken || dispenseOnHit.CoolingDown || args.DamageDelta == null)
            return;

        if (!(args.DamageIncreased && args.DamageDelta.GetTotal() >= dispenseOnHit.Threshold) ||
            !_random.Prob(dispenseOnHit.Chance)) return;

        if (dispenseOnHit.NextDispenseDelay != null)
        {
            dispenseOnHit.NextDispenseTime = Timing.CurTime + dispenseOnHit.NextDispenseDelay.Value;
        }

        EjectRandom((entity.Owner, entity.Comp), throwItem: true, forceEject: true);
    }

    [SubscribeLocalEvent]
    private void OnSelfDispense(Entity<VendingMachineComponent> entity, ref VendingMachineSelfDispenseEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        EjectRandom((entity.Owner, entity.Comp), throwItem: true, forceEject: false);
    }

    [SubscribeLocalEvent]
    private void OnPriceCalculation(Entity<VendingMachineRestockComponent> entity, ref PriceCalculationEvent args)
    {
        List<double> priceSets = new();

        // Find the most expensive inventory and use that as the highest price.
        foreach (var vendingInventory in entity.Comp.CanRestock)
        {
            double total = 0;

            if (ProtoMan.TryIndex(vendingInventory, out VendingMachineInventoryPrototype? inventoryPrototype))
            {
                foreach (var (item, amount) in inventoryPrototype.EnumerateInventory(InventoryType.Regular))
                {
                    if (ProtoMan.TryIndex(item, out EntityPrototype? prototype))
                        total += _pricing.GetEstimatedPrice(prototype) * amount;
                }
            }

            priceSets.Add(total);
        }

        args.Price += priceSets.Max();
    }

    [SubscribeLocalEvent]
    private void OnTryVocalize(Entity<VendingMachineComponent> ent, ref TryVocalizeEvent args)
    {
        args.Cancelled |= ent.Comp.Broken;
    }

    public void SetShooting(Entity<VendingMachineEjectComponent?> entity, bool canShoot)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        if (canShoot)
            EnsureComp<VendingMachineShootComponent>(entity.Owner);
        else
            RemComp<VendingMachineShootComponent>(entity.Owner);
    }

    /// <summary>
    /// Sets the <see cref="VendingMachineComponent.Contraband"/> property of the vending machine.
    /// </summary>
    public void SetContraband(Entity<VendingMachineComponent> entity, bool contraband)
    {
        entity.Comp.Contraband = contraband;
        Dirty(entity);
    }

    /// <summary>
    /// Ejects a random item from the available stock. Will do nothing if the vending machine is empty.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="throwItem">Whether to throw the item in a random direction after dispensing it.</param>
    /// <param name="forceEject">Whether to skip the regular ejection checks and immediately dispense the item without animation.</param>
    public void EjectRandom(
        Entity<VendingMachineComponent?, VendingMachineEjectComponent?> entity,
        bool throwItem,
        bool forceEject = false)
    {
        if (!Resolve(entity.Owner, ref entity.Comp1, ref entity.Comp2))
            return;

        var uid = entity.Owner;
        var vendComponent = entity.Comp1;
        var ejectComponent = entity.Comp2;
        var availableItems = GetAvailableInventory(uid, vendComponent);
        if (availableItems.Count <= 0)
            return;

        var item = _random.Pick(availableItems);

        if (forceEject)
        {
            ejectComponent.NextItemToEject = item.ID;
            ejectComponent.ThrowNextItem = throwItem;
            var entry = GetEntry(uid, item.ID, item.Type, vendComponent);
            if (entry != null)
            {
                entry.Amount--;
                Dirty(uid, vendComponent);
                UpdateUI((uid, vendComponent));
            }

            EjectItem((uid, vendComponent, ejectComponent), forceEject);
        }
        else
        {
            TryEjectVendorItem(uid, item.Type, item.ID, throwItem, user: null, vendComponent: vendComponent, ejectComponent: ejectComponent);
        }
    }
}
