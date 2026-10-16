using Content.Server.VendingMachines;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Vending;
using Content.Shared.VendingMachines.Components;

namespace Content.Server.EntityEffects.Effects.Vending;

/// <summary>
/// Throws out a specific amount of random items from a vending machine.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class EjectVendorItemsEntityEffectSystem : EntityEffectSystem<VendingMachineComponent, EjectVendorItems>
{
    [Dependency] private VendingMachineSystem _vendingMachine = default!;

    protected override void Effect(Entity<VendingMachineComponent> entity, ref EntityEffectEvent<EjectVendorItems> args)
    {
        var inventory = _vendingMachine.GetAvailableInventory(entity.Owner);
        if (inventory.Count <= 0)
            return;

        var toEject = Math.Min(inventory.Count * args.Effect.Percent, args.Effect.Max);
        for (var i = 0; i < toEject; i++)
        {
            _vendingMachine.EjectRandom(entity.AsNullable(), throwItem: true, forceEject: true);
        }
    }
}
