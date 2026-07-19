using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Inventory;

public partial class InventorySystem
{
    [Dependency] private SharedStorageSystem _storageSystem = default!;

    /// <summary>
    /// Yields all entities in hands or inventory slots with the specific flags.
    /// </summary>
    public IEnumerable<EntityUid> GetHandOrInventoryEntities(Entity<HandsComponent?, InventoryComponent?> user, SlotFlags flags = SlotFlags.All)
    {
        if (Resolve(user.Owner, ref user.Comp1, false))
        {
            foreach (var held in _handsSystem.EnumerateHeld(user))
            {
                yield return held;
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
    /// Returns true if the given entity is equipped to an inventory slot with exactly matching inventory slot flags.
    /// </summary>
    /// <seealso cref="InSlotWithAnyFlags" />
    public bool InSlotWithFlags(Entity<TransformComponent?, MetaDataComponent?> entity, SlotFlags flags)
    {
        return TryGetContainingSlot(entity, out var slot)
               && (slot.SlotFlags & flags) == flags;
    }

    /// <summary>
    /// Returns true if the given entity is equipped to an inventory slot that
    /// has any flags in common with the given ones.
    /// </summary>
    /// <seealso cref="InSlotWithFlags" />
    public bool InSlotWithAnyFlags(Entity<TransformComponent?, MetaDataComponent?> ent, SlotFlags flags)
    {
        return TryGetContainingSlot(ent, out var slot)
               && (slot.SlotFlags & flags) != 0;
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
        if (!ProtoMan.HasIndex<EntityPrototype>(prototype))
            return false;

        // Let's spawn this first...
        var item = Spawn(prototype, Transform(uid).Coordinates);

        // Helper method that deletes the item and returns false.
        bool DeleteItem()
        {
            Del(item);
            return false;
        }

        // We finally try to equip the item, otherwise we delete it.
        return TryEquip(uid, item, slot, silent, force) || DeleteItem();
    }
}
