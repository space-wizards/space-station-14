using System;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Strip.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Inventory;

public abstract partial class InventorySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private void InitializeEquip()
    {
        //these events ensure that the client also gets its proper events raised when getting its containerstate updated
        SubscribeLocalEvent<InventoryComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<InventoryComponent, EntRemovedFromContainerMessage>(OnEntRemoved);

        SubscribeAllEvent<UseSlotNetworkMessage>(OnUseSlot);
    }

    private void OnEntRemoved(EntityUid uid, InventoryComponent component, EntRemovedFromContainerMessage args)
    {
        if(!TryGetSlot(uid, args.Container.ID, out var slotDef, inventory: component))
            return;

        var unequippedEvent = new DidUnequipEvent(uid, args.Entity, slotDef);
        RaiseLocalEvent(uid, unequippedEvent);

        var gotUnequippedEvent = new GotUnequippedEvent(uid, args.Entity, slotDef);
        RaiseLocalEvent(args.Entity, gotUnequippedEvent);
    }

    private void OnEntInserted(EntityUid uid, InventoryComponent component, EntInsertedIntoContainerMessage args)
    {
        if(!TryGetSlot(uid, args.Container.ID, out var slotDef, inventory: component))
           return;

        var equippedEvent = new DidEquipEvent(uid, args.Entity, slotDef);
        RaiseLocalEvent(uid, equippedEvent);

        var gotEquippedEvent = new GotEquippedEvent(uid, args.Entity, slotDef);
        RaiseLocalEvent(args.Entity, gotEquippedEvent);
    }

    /// <summary>
    ///     Will attempt to equip or unequip an item to/from the clicked slot. If the user clicked on an occupied slot
    ///     with some entity, will instead attempt to interact with this entity.
    /// </summary>
    private void OnUseSlot(UseSlotNetworkMessage ev, EntitySessionEventArgs eventArgs)
    {
        if (eventArgs.SenderSession.AttachedEntity is not EntityUid { Valid: true } actor)
            return;

        if (!TryComp(actor, out InventoryComponent? inventory) || !TryComp<SharedHandsComponent>(actor, out var hands))
            return;

        hands.TryGetActiveHeldEntity(out var held);
        TryGetSlotEntity(actor, ev.Slot, out var itemUid, inventory);

        // attempt to perform some interaction
        if (held != null && itemUid != null)
        {
            _interactionSystem.InteractUsing(actor, held.Value, itemUid.Value,
                new EntityCoordinates(), predicted: true);
            return;
        }

        // un-equip to hands
        if (itemUid != null)
        {
            if (hands.CanPickupEntityToActiveHand(itemUid.Value) && TryUnequip(actor, ev.Slot, inventory: inventory))
                hands.PutInHand(itemUid.Value, false);
            return;
        }

        // finally, just try to equip the held item.
        if (held == null)
            return;

        // before we drop the item, check that it can be equipped in the first place.
        if (!CanEquip(actor, held.Value, ev.Slot, out var reason))
        {
            if (_gameTiming.IsFirstTimePredicted)
                _popup.PopupCursor(Loc.GetString(reason), Filter.Local());
            return;
        }

        if (hands.TryDropNoInteraction())
            TryEquip(actor, actor, held.Value, ev.Slot, predicted: true, inventory: inventory);
        }

    public bool TryEquip(EntityUid uid, EntityUid itemUid, string slot, bool silent = false, bool force = false, bool predicted = false,
        InventoryComponent? inventory = null, SharedItemComponent? item = null) =>
        TryEquip(uid, uid, itemUid, slot, silent, force, predicted, inventory, item);

    public bool TryEquip(EntityUid actor, EntityUid target, EntityUid itemUid, string slot, bool silent = false, bool force = false, bool predicted = false,
        InventoryComponent? inventory = null, SharedItemComponent? item = null)
    {
        if (!Resolve(target, ref inventory, false) || !Resolve(itemUid, ref item, false))
        {
            if(!silent && _gameTiming.IsFirstTimePredicted)
                _popup.PopupCursor(Loc.GetString("inventory-component-can-equip-cannot"), Filter.Local());
            return false;
        }

        if (!TryGetSlotContainer(target, slot, out var slotContainer, out var slotDefinition, inventory))
        {
            if(!silent && _gameTiming.IsFirstTimePredicted)
                _popup.PopupCursor(Loc.GetString("inventory-component-can-equip-cannot"), Filter.Local());
            return false;
        }

        if (!force && !CanEquip(actor, target, itemUid, slot, out var reason, slotDefinition, inventory, item))
        {
            if(!silent && _gameTiming.IsFirstTimePredicted)
                _popup.PopupCursor(Loc.GetString(reason), Filter.Local());
            return false;
        }

        if (!slotContainer.Insert(itemUid))
        {
            if(!silent && _gameTiming.IsFirstTimePredicted)
                _popup.PopupCursor(Loc.GetString("inventory-component-can-unequip-cannot"), Filter.Local());
            return false;
        }

        if(!silent && item.EquipSound != null && _gameTiming.IsFirstTimePredicted)
        {
            var filter = Filter.Pvs(target);

            // don't play double audio for predicted interactions
            if (predicted)
                filter.RemoveWhereAttachedEntity(entity => entity == actor);

            SoundSystem.Play(filter, item.EquipSound.GetSound(), target, AudioParams.Default.WithVolume(-2f));
        }

        inventory.Dirty();

        _movementSpeed.RefreshMovementSpeedModifiers(target);

        return true;
    }

    public bool CanAccess(EntityUid actor, EntityUid target, EntityUid itemUid)
    {
        // Can the actor reach the target?
        if (actor != target && !(_interactionSystem.InRangeUnobstructed(actor, target) && _containerSystem.IsInSameOrParentContainer(actor, target)))
            return false;

        // Can the actor reach the item?
        if (_interactionSystem.InRangeUnobstructed(actor, itemUid) && _containerSystem.IsInSameOrParentContainer(actor, itemUid))
            return true;

        // Is the item in an open storage UI, i.e., is the user quick-equipping from an open backpack?
        if (_interactionSystem.CanAccessViaStorage(actor, itemUid))
            return true;

        // Is the actor currently stripping the target? Here we could check if the actor has the stripping UI open, but
        // that requires server/client specific code. so lets just check if they **could** open the stripping UI.
        // Note that this doesn't check that the item is equipped by the target, as this is done elsewhere.
        return actor != target
            && TryComp(target, out SharedStrippableComponent? strip)
            && strip.CanBeStripped(actor);
    }

    public bool CanEquip(EntityUid uid, EntityUid itemUid, string slot, [NotNullWhen(false)] out string? reason,
        SlotDefinition? slotDefinition = null, InventoryComponent? inventory = null,
        SharedItemComponent? item = null) =>
        CanEquip(uid, uid, itemUid, slot, out reason, slotDefinition, inventory, item);

    public bool CanEquip(EntityUid actor, EntityUid target, EntityUid itemUid, string slot, [NotNullWhen(false)] out string? reason, SlotDefinition? slotDefinition = null, InventoryComponent? inventory = null, SharedItemComponent? item = null)
    {
        reason = "inventory-component-can-equip-cannot";
        if (!Resolve(target, ref inventory, false) || !Resolve(itemUid, ref item, false))
            return false;

        if (slotDefinition == null && !TryGetSlot(target, slot, out slotDefinition, inventory: inventory))
            return false;

        if (slotDefinition.DependsOn != null && !TryGetSlotEntity(target, slotDefinition.DependsOn, out _, inventory))
            return false;

        if(!item.SlotFlags.HasFlag(slotDefinition.SlotFlags) && (!slotDefinition.SlotFlags.HasFlag(SlotFlags.POCKET) || item.Size > (int) ReferenceSizes.Pocket))
        {
            reason = "inventory-component-can-equip-does-not-fit";
            return false;
        }

        if (!CanAccess(actor, target, itemUid))
        {
            reason = "interaction-system-user-interaction-cannot-reach";
            return false;
        }

        var attemptEvent = new IsEquippingAttemptEvent(actor, target, itemUid, slotDefinition);
        RaiseLocalEvent(target, attemptEvent);
        if (attemptEvent.Cancelled)
        {
            reason = attemptEvent.Reason ?? reason;
            return false;
        }

        if (actor != target)
        {
            //reuse the event. this is gucci, right?
            attemptEvent.Reason = null;
            RaiseLocalEvent(actor, attemptEvent);
            if (attemptEvent.Cancelled)
            {
                reason = attemptEvent.Reason ?? reason;
                return false;
            }
        }

        var itemAttemptEvent = new BeingEquippedAttemptEvent(actor, target, itemUid, slotDefinition);
        RaiseLocalEvent(itemUid, itemAttemptEvent);
        if (itemAttemptEvent.Cancelled)
        {
            reason = itemAttemptEvent.Reason ?? reason;
            return false;
        }

        return true;
    }

    public bool TryUnequip(EntityUid uid, string slot, bool silent = false, bool force = false,
        InventoryComponent? inventory = null) => TryUnequip(uid, uid, slot, silent, force, inventory);

    public bool TryUnequip(EntityUid actor, EntityUid target, string slot, bool silent = false,
        bool force = false, InventoryComponent? inventory = null) =>
        TryUnequip(actor, target, slot, out _, silent, force, inventory);

    public bool TryUnequip(EntityUid uid, string slot, [NotNullWhen(true)] out EntityUid? removedItem, bool silent = false, bool force = false,
        InventoryComponent? inventory = null) => TryUnequip(uid, uid, slot, out removedItem, silent, force, inventory);

    public bool TryUnequip(EntityUid actor, EntityUid target, string slot, [NotNullWhen(true)] out EntityUid? removedItem, bool silent = false,
        bool force = false, InventoryComponent? inventory = null)
    {
        removedItem = null;
        if (!Resolve(target, ref inventory, false))
        {
            if(!silent && _gameTiming.IsFirstTimePredicted)
                _popup.PopupCursor(Loc.GetString("inventory-component-can-unequip-cannot"), Filter.Local());
            return false;
        }

        if (!TryGetSlotContainer(target, slot, out var slotContainer, out var slotDefinition, inventory))
        {
            if(!silent && _gameTiming.IsFirstTimePredicted)
                _popup.PopupCursor(Loc.GetString("inventory-component-can-unequip-cannot"), Filter.Local());
            return false;
        }

        removedItem = slotContainer.ContainedEntity;

        if (!removedItem.HasValue) return false;

        if (!force && !CanUnequip(actor, target, slot, out var reason, slotContainer, slotDefinition, inventory))
        {
            if(!silent && _gameTiming.IsFirstTimePredicted)
                _popup.PopupCursor(Loc.GetString(reason), Filter.Local());
            return false;
        }

        //we need to do this to make sure we are 100% removing this entity, since we are now dropping dependant slots
        if (!force && !slotContainer.CanRemove(removedItem.Value))
            return false;

        foreach (var slotDef in GetSlots(target, inventory))
        {
            if (slotDef != slotDefinition && slotDef.DependsOn == slotDefinition.Name)
            {
                //this recursive call might be risky
                TryUnequip(actor, target, slotDef.Name, true, true, inventory);
            }
        }

        if (force)
        {
            slotContainer.ForceRemove(removedItem.Value);
        }
        else
        {
            if (!slotContainer.Remove(removedItem.Value))
            {
                //should never happen bc of the canremove lets just keep in just in case
                return false;
            }
        }

        Transform(removedItem.Value).Coordinates = Transform(target).Coordinates;

        inventory.Dirty();

        _movementSpeed.RefreshMovementSpeedModifiers(target);

        return true;
    }

    public bool CanUnequip(EntityUid uid, string slot, [NotNullWhen(false)] out string? reason,
        ContainerSlot? containerSlot = null, SlotDefinition? slotDefinition = null,
        InventoryComponent? inventory = null) =>
        CanUnequip(uid, uid, slot, out reason, containerSlot, slotDefinition, inventory);

    public bool CanUnequip(EntityUid actor, EntityUid target, string slot, [NotNullWhen(false)] out string? reason, ContainerSlot? containerSlot = null, SlotDefinition? slotDefinition = null, InventoryComponent? inventory = null)
    {
        reason = "inventory-component-can-unequip-cannot";
        if (!Resolve(target, ref inventory, false))
            return false;

        if ((containerSlot == null || slotDefinition == null) && !TryGetSlotContainer(target, slot, out containerSlot, out slotDefinition, inventory))
            return false;

        if (containerSlot.ContainedEntity == null)
            return false;

        if (!containerSlot.ContainedEntity.HasValue || !containerSlot.CanRemove(containerSlot.ContainedEntity.Value))
            return false;

        var itemUid = containerSlot.ContainedEntity.Value;

        // make sure the user can actually reach the target
        if (!CanAccess(actor, target, itemUid))
        {
            reason = "interaction-system-user-interaction-cannot-reach";
            return false;
        }

        var attemptEvent = new IsUnequippingAttemptEvent(actor, target, itemUid, slotDefinition);
        RaiseLocalEvent(target, attemptEvent);
        if (attemptEvent.Cancelled)
        {
            reason = attemptEvent.Reason ?? reason;
            return false;
        }

        if (actor != target)
        {
            //reuse the event. this is gucci, right?
            attemptEvent.Reason = null;
            RaiseLocalEvent(actor, attemptEvent);
            if (attemptEvent.Cancelled)
            {
                reason = attemptEvent.Reason ?? reason;
                return false;
            }
        }

        var itemAttemptEvent = new BeingUnequippedAttemptEvent(actor, target, itemUid, slotDefinition);
        RaiseLocalEvent(itemUid, itemAttemptEvent);
        if (itemAttemptEvent.Cancelled)
        {
            reason = attemptEvent.Reason ?? reason;
            return false;
        }

        return true;
    }

    public bool TryGetSlotEntity(EntityUid uid, string slot, [NotNullWhen(true)] out EntityUid? entityUid, InventoryComponent? inventoryComponent = null, ContainerManagerComponent? containerManagerComponent = null)
    {
        entityUid = null;
        if (!Resolve(uid, ref inventoryComponent, ref containerManagerComponent, false)
            || !TryGetSlotContainer(uid, slot, out var container, out _, inventoryComponent, containerManagerComponent))
            return false;

        entityUid = container.ContainedEntity;
        return entityUid != null;
    }
}
