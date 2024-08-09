using Content.Shared.ActionBlocker;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.Interaction;

/// <summary>
/// This handles smart equipping or inserting/ejecting from slots through keybinds--generally shift+E and shift+B
/// </summary>
public sealed class SmartEquipSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.SmartEquipBackpack, InputCmdHandler.FromDelegate(HandleSmartEquipBackpack, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.SmartEquipBelt, InputCmdHandler.FromDelegate(HandleSmartEquipBelt, handle: false, outsidePrediction: false))
            .Register<SmartEquipSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<SmartEquipSystem>();
    }

    private void HandleSmartEquipBackpack(ICommonSession? session)
    {
        HandleSmartEquip(session, "back");
    }

    private void HandleSmartEquipBelt(ICommonSession? session)
    {
        HandleSmartEquip(session, "belt");
    }

    private void HandleSmartEquip(ICommonSession? session, string equipmentSlot)
    {
        if (session is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not { Valid: true } uid || !Exists(uid))
            return;

        // early out if we don't have any hands or a valid inventory slot
        if (!TryComp<HandsComponent>(uid, out var hands) || hands.ActiveHand == null)
            return;

        var handItem = hands.ActiveHand.HeldEntity;

        // can the user interact, and is the item interactable? e.g. virtual items
        if (!_actionBlocker.CanInteract(uid, handItem))
            return;

        if (!TryComp<InventoryComponent>(uid, out var inventory) || !_inventory.HasSlot(uid, equipmentSlot, inventory))
        {
            _popup.PopupClient(Loc.GetString("smart-equip-missing-equipment-slot", ("slotName", equipmentSlot)), uid, uid);
            return;
        }

        // early out if we have an item and cant drop it at all
        if (handItem != null && !_hands.CanDropHeld(uid, hands.ActiveHand))
        {
            _popup.PopupClient(Loc.GetString("smart-equip-cant-drop"), uid, uid);
            return;
        }

        // There are eight main cases we want to handle here,
        // so let's write them out

        // if the slot we're trying to smart equip from:
        // 1) doesn't have an item
        //    - with hand item: try to put it in the slot
        //    - without hand item: fail
        // 2) has an item, and that item is a storage item
        //    - with hand item: try to put it in storage
        //    - without hand item: try to take the last stored item and put it in our hands
        // 3) has an item, and that item is an item slots holder
        //    - with hand item: get the highest priority item slot with a valid whitelist and try to insert it
        //    - without hand item: get the highest priority item slot with an item and try to eject it
        // 4) has an item, with no special storage components
        //    - with hand item: fail
        //    - without hand item: try to put the item into your hand

        _inventory.TryGetSlotEntity(uid, equipmentSlot, out var slotEntity);
        var emptyEquipmentSlotString = Loc.GetString("smart-equip-empty-equipment-slot", ("slotName", equipmentSlot));

        // case 1 (no slot item):
        if (slotEntity is not { } slotItem)
        {
            if (handItem == null)
            {
                _popup.PopupClient(emptyEquipmentSlotString, uid, uid);
                return;
            }

            if (!_inventory.CanEquip(uid, handItem.Value, equipmentSlot, out var reason))
            {
                _popup.PopupClient(Loc.GetString(reason), uid, uid);
                return;
            }

            _hands.TryDrop(uid, hands.ActiveHand, handsComp: hands);
            _inventory.TryEquip(uid, handItem.Value, equipmentSlot, predicted: true, checkDoafter:true);
            return;
        }

        // case 2 (storage item):
        if (TryComp<StorageComponent>(slotItem, out var storage))
        {
            switch (handItem)
            {
                case null when storage.Container.ContainedEntities.Count == 0:
                    _popup.PopupClient(emptyEquipmentSlotString, uid, uid);
                    return;
                case null:
                    var removing = storage.Container.ContainedEntities[^1];
                    _container.RemoveEntity(slotItem, removing);
                    _hands.TryPickup(uid, removing, handsComp: hands);
                    return;
            }

            if (!_storage.CanInsert(slotItem, handItem.Value, out var reason))
            {
                if (reason != null)
                    _popup.PopupClient(Loc.GetString(reason), uid, uid);

                return;
            }

            _hands.TryDrop(uid, hands.ActiveHand, handsComp: hands);
            _storage.Insert(slotItem, handItem.Value, out var stacked, out _);

            if (stacked != null)
                _hands.TryPickup(uid, stacked.Value, handsComp: hands);

            return;
        }

        // case 3 (itemslot item):
        if (TryComp<ItemSlotsComponent>(slotItem, out var slots))
        {
            if (handItem == null)
            {
                ItemSlot? toEjectFrom = null;

                foreach (var slot in slots.Slots.Values)
                {
                    if (slot.HasItem && slot.Priority > (toEjectFrom?.Priority ?? int.MinValue))
                        toEjectFrom = slot;
                }

                if (toEjectFrom == null)
                {
                    _popup.PopupClient(emptyEquipmentSlotString, uid, uid);
                    return;
                }

                _slots.TryEjectToHands(slotItem, toEjectFrom, uid, excludeUserAudio: true);
                return;
            }

            ItemSlot? toInsertTo = null;

            foreach (var slot in slots.Slots.Values)
            {
                if (!slot.HasItem
                    && _whitelistSystem.IsWhitelistPassOrNull(slot.Whitelist, handItem.Value)
                    && slot.Priority > (toInsertTo?.Priority ?? int.MinValue))
                {
                    toInsertTo = slot;
                }
            }

            if (toInsertTo == null)
            {
                _popup.PopupClient(Loc.GetString("smart-equip-no-valid-item-slot-insert", ("item", handItem.Value)), uid, uid);
                return;
            }

            _slots.TryInsertFromHand(slotItem, toInsertTo, uid, hands, excludeUserAudio: true);
            return;
        }

        // case 4 (just an item):
        if (handItem != null)
            return;

        if (!_inventory.CanUnequip(uid, equipmentSlot, out var inventoryReason))
        {
            _popup.PopupClient(Loc.GetString(inventoryReason), uid, uid);
            return;
        }

        _inventory.TryUnequip(uid, equipmentSlot, inventory: inventory, predicted: true, checkDoafter: true);
        _hands.TryPickup(uid, slotItem, handsComp: hands);
    }
}
