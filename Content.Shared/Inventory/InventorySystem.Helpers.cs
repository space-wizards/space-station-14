using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Inventory;

public partial class InventorySystem
{
    [Dependency] private readonly SharedStorageSystem _storageSystem = default!;

    /// <summary>
    /// Yields all entities in hands or inventory slots with the specific flags.
    /// </summary>
    public IEnumerable<EntityUid> GetHandOrInventoryEntities(Entity<HandsComponent?, InventoryComponent?> user, SlotFlags flags = SlotFlags.All)
    {
        if (Resolve(user.Owner, ref user.Comp1, false))
        {
            foreach (var hand in user.Comp1.Hands.Values)
            {
                if (hand.HeldEntity == null)
                    continue;

                yield return hand.HeldEntity.Value;
            }
        }

        if (!Resolve(user.Owner, ref user.Comp2, false))
            yield break;

        var slotEnumerator = new InventorySlotEnumerator(user.Comp2, flags);
        while (slotEnumerator.NextItem(out var item))
        {
            yield return item;
        }
    }

    /// <summary>
    ///     Returns the definition of the inventory slot that the given entity is currently in..
    /// </summary>
    public bool TryGetContainingSlot(Entity<TransformComponent?, MetaDataComponent?> entity, [NotNullWhen(true)] out SlotDefinition? slot)
    {
        if (!_containerSystem.TryGetContainingContainer(entity, out var container))
        {
            slot = null;
            return false;
        }

        return TryGetSlot(container.Owner, container.ID, out slot);
    }

    /// <summary>
    ///     Returns true if the given entity is equipped to an inventory slot with the given inventory slot flags.
    /// </summary>
    public bool InSlotWithFlags(Entity<TransformComponent?, MetaDataComponent?> entity, SlotFlags flags)
    {
        return TryGetContainingSlot(entity, out var slot)
               && (slot.SlotFlags & flags) == flags;
    }

    public bool SpawnItemInSlot(EntityUid uid, string slot, string prototype, bool silent = false, bool force = false, InventoryComponent? inventory = null)
    {
        if (!Resolve(uid, ref inventory, false))
            return false;

        // Let's do nothing if the owner of the inventory has been deleted.
        if (Deleted(uid))
            return false;

        // If we don't have that slot or there's already an item there, we do nothing.
        if (!HasSlot(uid, slot) || TryGetSlotEntity(uid, slot, out _, inventory))
            return false;

        // If the prototype in question doesn't exist, we do nothing.
        if (!_prototypeManager.HasIndex<EntityPrototype>(prototype))
            return false;

        // Let's spawn this first...
        var item = EntityManager.SpawnEntity(prototype, Transform(uid).Coordinates);

        // Helper method that deletes the item and returns false.
        bool DeleteItem()
        {
            EntityManager.DeleteEntity(item);
            return false;
        }

        // We finally try to equip the item, otherwise we delete it.
        return TryEquip(uid, item, slot, silent, force) || DeleteItem();
    }

    /// <summary>
    /// Will attempt to spawn a list of items inside of an entities bag, pockets, hands or nearby
    /// </summary>
    /// <param name="entity">The entity that you want to spawn an item on</param>
    /// <param name="items">A list of prototype IDs that you want to spawn in the bag.</param>
    public void SpawnItemsOnEntity(EntityUid entity, List<string> items)
    {
        foreach (var item in items)
        {
            SpawnItemOnEntity(entity, item);
        }
    }

    /// <summary>
    /// Will attempt to spawn an item inside of an entities bag, pockets, hands or nearby
    /// </summary>
    /// <param name="entity">The entity that you want to spawn an item on</param>
    /// <param name="item">The prototype ID that you want to spawn in the bag.</param>
    public void SpawnItemOnEntity(EntityUid entity, EntProtoId item)
    {
        //Transform() throws error if TransformComponent doesnt exist
        if (!HasComp<TransformComponent>(entity))
            return;

        var xform = Transform(entity);
        var mapCoords = _transform.GetMapCoordinates(xform);

        var itemToSpawn = Spawn(item, mapCoords);

        //Try insert into the backpack
        if (TryGetSlotContainer(entity, "back", out var backSlot, out _)
            && backSlot.ContainedEntity.HasValue
            && _storageSystem.Insert(backSlot.ContainedEntity.Value, itemToSpawn, out _)
            )
            return;

        //Try insert into pockets
        if (TryGetSlotContainer(entity, "pocket1", out var pocket1, out _)
            && _containerSystem.Insert(itemToSpawn, pocket1)
            )
            return;

        if (TryGetSlotContainer(entity, "pocket2", out var pocket2, out _)
            && _containerSystem.Insert(itemToSpawn, pocket2)
            )
            return;

        //Try insert into hands, or drop on the floor
        _handsSystem.PickupOrDrop(entity, itemToSpawn, false);
    }
}
