using System;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Inventory;

public class SharedInventorySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedInventoryComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, SharedInventoryComponent component, ComponentInit args)
    {
        foreach (var slotName in component.InventoryInstance.SlotMasks)
        {
            if (slotName != EquipmentSlotDefines.Slots.NONE)
            {
                TryAddSlot(uid, slotName, out _, component);
            }
        }
    }

    /// <summary>
    ///     Adds a new slot to this inventory component.
    /// </summary>
    /// <param name="uid">The uid of the entity.</param>
    /// <param name="slot">The name of the slot to add.</param>
    /// <param name="inventoryComponent">The inventorycomponent, if provided</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the slot with specified name already exists.
    /// </exception>
    public bool TryAddSlot(EntityUid uid, EquipmentSlotDefines.Slots slot, [NotNullWhen(true)] out ContainerSlot? containerSlot, SharedInventoryComponent? inventoryComponent = null)
    {
        containerSlot = null;
        if (!Resolve(uid, ref inventoryComponent) || HasSlot(uid, slot, inventoryComponent))
            return false;

        inventoryComponent.Dirty();

        containerSlot = ContainerHelpers.CreateContainer<ContainerSlot>(EntityManager, uid, GetSlotString(uid, slot, inventoryComponent));
        containerSlot.OccludesLight = false;
        inventoryComponent.SlotContainers[slot] = containerSlot.ID;

        RaiseLocalEvent(uid, new ItemChangedEvent());

        return true;
    }

    /// <summary>
    ///     Checks whether a slot with the specified name exists.
    /// </summary>
    /// <param name="slot">The slot name to check.</param>
    /// <returns>True if the slot exists, false otherwise.</returns>
    public bool HasSlot(EntityUid uid, EquipmentSlotDefines.Slots slot, SharedInventoryComponent? inventoryComponent = null)
    {
        if (!Resolve(uid, ref inventoryComponent))
            return false;

        return inventoryComponent.SlotContainers.ContainsKey(slot);
    }

    /// <summary>
    /// Helper to get container name for specified slot on this component
    /// </summary>
    /// <param name="slot"></param>
    /// <returns></returns>
    private string GetSlotString(EntityUid uid, EquipmentSlotDefines.Slots slot, SharedInventoryComponent? inventoryComponent = null)
    {
        if (!Resolve(uid, ref inventoryComponent))
            return string.Empty;

        return inventoryComponent.Name + "_" + Enum.GetName(typeof(EquipmentSlotDefines.Slots), slot);
    }
}
