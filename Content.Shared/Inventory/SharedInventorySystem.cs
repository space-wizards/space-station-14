using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Shared.Inventory;

public class SharedInventorySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedInventoryComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SharedInventoryComponent, ComponentGetState>(OnGetCompState);
    }

    private void OnGetCompState(EntityUid uid, SharedInventoryComponent component, ref ComponentGetState args)
    {
        args.State = new InventoryComponentState(component.SlotContainers);
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

    #region Slots

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

    /// <summary>
    ///     Removes a slot from this inventory component.
    /// </summary>
    /// <remarks>
    ///     If the slot contains an item, the item is dropped.
    /// </remarks>
    /// <param name="slot">The name of the slot to remove.</param>
    public void RemoveSlot(EntityUid uid, EquipmentSlotDefines.Slots slot, SharedInventoryComponent? inventoryComponent = null, ContainerManagerComponent? containerManagerComponent = null)
    {
        if (!Resolve(uid, ref inventoryComponent, ref containerManagerComponent))
            return;

        if (!HasSlot(uid, slot, inventoryComponent))
        {
            throw new InvalidOperationException($"Slot '{slot}' does not exist.");
        }

        Unequip(uid, slot, force: true);

        var container = containerManagerComponent.Containers[inventoryComponent.SlotContainers[slot]];
        container.Shutdown();
        inventoryComponent.SlotContainers.Remove(slot);

        RaiseLocalEvent(uid, new ItemChangedEvent());

        inventoryComponent.Dirty();
    }

    #endregion

    #region Equip

    /// <summary>
    ///     Equips slothing to the specified slot.
    /// </summary>
    /// <remarks>
    ///     This will fail if there is already an item in the specified slot.
    /// </remarks>
    /// <param name="slot">The slot to put the item in.</param>
    /// <param name="item">The item to insert into the slot.</param>
    /// <param name="mobCheck">Whether to perform an ActionBlocker check to the entity.</param>
    /// <param name="reason">The translated reason why the item cannot be equipped, if this function returns false. Can be null.</param>
    /// <returns>True if the item was successfully inserted, false otherwise.</returns>
    public bool Equip(EntityUid uidInventory, EntityUid uidItem, EquipmentSlotDefines.Slots slot, bool mobCheck,
        SharedInventoryComponent? inventoryComponent = null,
        ContainerManagerComponent? containerManagerComponent = null, SharedItemComponent? item = null)
    {
        if (!Resolve(uidInventory, ref inventoryComponent, ref containerManagerComponent) || !Resolve(uidItem, ref item))
            return false;

        if (!CanEquip(slot, item, mobCheck, out var failReason))
        {
            _popup.PopupCursor(failReason, Filter.Local());
            return false;
        }

        var inventorySlot = inventoryComponent.SlotContainers[slot];
        if (!containerManagerComponent.GetContainer(inventorySlot).Insert(uidItem))
        {
            _popup.PopupCursor(Loc.GetString("inventory-component-on-equip-cannot"), Filter.Local());
            return false;
        }

        // TODO: Make clothing component not inherit ItemComponent, for fuck's sake.
        // TODO: Make clothing component not required for playing a sound on equip... Move it to its own component.
        if (mobCheck && item is ClothingComponent { EquipSound: {} equipSound })
        {
            SoundSystem.Play(Filter.Pvs(Owner), equipSound.GetSound(), Owner, AudioParams.Default.WithVolume(-2f));
        }

        _entitySystemManager.GetEntitySystem<InteractionSystem>().EquippedInteraction(Owner, item.Owner, slot);

        RaiseLocalEvent(uid, new ItemChangedEvent());

        inventoryComponent.Dirty();

        UpdateMovementSpeed(uidInventory);

        return true;
    }

    /// <summary>
    ///     Checks whether an item can be put in the specified slot.
    /// </summary>
    /// <param name="slot">The slot to check for.</param>
    /// <param name="item">The item to check for.</param>
    /// <param name="reason">The translated reason why the item cannot be equiped, if this function returns false. Can be null.</param>
    /// <returns>True if the item can be inserted into the specified slot.</returns>
    public bool CanEquip(EntityUid invUid, EntityUid itemUid, EquipmentSlotDefines.Slots slot, bool mobCheck, out string? reason, SharedInventoryComponent? inventory = null, SharedItemComponent? item = null)
    {
        if (!Resolve(invUid, ref inventory) || !Resolve(itemUid, ref item))
            return false;

        var pass = false;
        reason = null;

        if (mobCheck && !_actionBlocker.CanEquip(invUid))
        {
            reason = Loc.GetString("inventory-component-can-equip-cannot");
            return false;
        }

        if (item is ClothingComponent clothing)
        {
            if (clothing.SlotFlags != EquipmentSlotDefines.SlotFlags.PREVENTEQUIP && (clothing.SlotFlags & SlotMasks[slot]) != 0)
            {
                pass = true;
            }
            else
            {
                reason = Loc.GetString("inventory-component-can-equip-does-not-fit");
            }
        }

        if (Owner.TryGetComponent(out IInventoryController? controller))
        {
            pass = controller.CanEquip(slot, item.Owner, pass, out var controllerReason);
            reason = controllerReason ?? reason;
        }

        if (!pass)
        {
            reason = reason ?? Loc.GetString("inventory-component-can-equip-cannot");
            return false;
        }

        var canEquip = pass && _slotContainers[slot].CanInsert(item.Owner);

        if (!canEquip)
        {
            reason = Loc.GetString("inventory-component-can-equip-cannot");
        }

        return canEquip;
    }

    /// <summary>
    ///     Drops the item in a slot.
    /// </summary>
    /// <param name="slot">The slot to drop the item from.</param>
    /// <returns>True if an item was dropped, false otherwise.</returns>
    /// <param name="mobCheck">Whether to perform an ActionBlocker check to the entity.</param>
    public bool Unequip(EntityUid uid, EquipmentSlotDefines.Slots slot, bool mobCheck = true, bool force = false)
    {
        if (!force && !CanUnequip(slot, mobCheck))
        {
            return false;
        }

        var inventorySlot = _slotContainers[slot];
        var entity = inventorySlot.ContainedEntity;

        if (entity == null)
        {
            return false;
        }

        if (force)
        {
            inventorySlot.ForceRemove(entity);
        }
        else
        {
            if (!inventorySlot.Remove(entity))
            {
                return false;
            }
        }

        // TODO: The item should be dropped to the container our owner is in, if any.
        entity.Transform.AttachParentToContainerOrGrid();

        _entitySystemManager.GetEntitySystem<InteractionSystem>().UnequippedInteraction(Owner, entity, slot);

        OnItemChanged?.Invoke();

        Dirty();

        UpdateMovementSpeed(uid);

        return true;
    }

    private void UpdateMovementSpeed(EntityUid uid)
    {
        Get<MovementSpeedModifierSystem>().RefreshMovementSpeedModifiers(uid);
    }

    /// <summary>
    ///     Checks whether an item can be dropped from the specified slot.
    /// </summary>
    /// <param name="slot">The slot to check for.</param>
    /// <param name="mobCheck">Whether to perform an ActionBlocker check to the entity.</param>
    /// <returns>
    ///     True if there is an item in the slot and it can be dropped, false otherwise.
    /// </returns>
    public bool CanUnequip(EntityUid uid, EquipmentSlotDefines.Slots slot, bool mobCheck = true, SharedInventoryComponent? inventory = null, ContainerManagerComponent? container = null)
    {
        if (!Resolve(uid, ref inventory, ref container))
            return false;

        if (mobCheck && !_actionBlocker.CanUnequip(uid))
            return false;

        var inventoryContainer = container.GetContainer(inventory.SlotContainers[slot]);
        if (inventoryContainer is not ContainerSlot inventorySlot)
            return false;
        return inventorySlot.ContainedEntity != null && inventorySlot.CanRemove(inventorySlot.ContainedEntity);
    }

    #endregion
}
