using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

/// <summary>
/// Spawns an entity of a given prototype in a specified inventory slot owned by this entity.
/// Fails if it cannot spawn the entity in the given slot.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SpawnEntityInInventoryEntityEffectSystem : EntityEffectSystem<InventoryComponent, SpawnEntityInInventory>
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    protected override void Effect(Entity<InventoryComponent> entity, ref EntityEffectEvent<SpawnEntityInInventory> args)
    {
        _inventory.SpawnItemInSlot(entity, args.Effect.Slot, args.Effect.Entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SpawnEntityInInventory : EntityEffectBase<SpawnEntityInInventory>
{
    /// <summary>
    /// Name of the slot we're spawning the item into.
    /// </summary>
    [DataField(required: true)]
    public string Slot = string.Empty; // Rider is drunk and keeps yelling at me to fill this out or make required: true but, it is required true so it's just being an asshole.

    /// <summary>
    /// Prototype ID of item to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Entity;
}
