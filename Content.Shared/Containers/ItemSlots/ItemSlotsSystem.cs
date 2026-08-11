using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Destructible;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Containers.ItemSlots;

/// <summary>
/// A class that handles interactions related to inserting/ejecting items into/from an item slot.
/// </summary>
/// <remarks>
/// Note when using popups on entities with many slots with InsertOnInteract, EjectOnInteract or EjectOnUse:
/// A single use will try to insert to/eject from every slot and generate a popup for each that fails.
/// </remarks>
public sealed partial class ItemSlotsSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;

    /// <summary>
    /// Spawn in starting items for any item slots that should have one.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ItemSlotsComponent> ent, ref MapInitEvent args)
    {
        foreach (var slot in ent.Comp.Slots.Values)
        {
            if (slot.HasItem || string.IsNullOrEmpty(slot.StartingItem))
                continue;

            var item = Spawn(slot.StartingItem, Transform(ent).Coordinates);

            if (slot.ContainerSlot != null)
                _containers.Insert(item, slot.ContainerSlot);
        }
    }

    /// <summary>
    /// Ensure item slots have containers.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnInitialize(Entity<ItemSlotsComponent> ent, ref ComponentInit args)
    {
        foreach (var (id, slot) in ent.Comp.Slots)
        {
            slot.ContainerSlot = _containers.EnsureContainer<ContainerSlot>(ent, id);
        }
    }

    [SubscribeLocalEvent]
    private void OnBreak(Entity<ItemSlotsComponent> ent, ref BreakageEventArgs args)
    {
        EjectOnBreak(ent);
    }

    [SubscribeLocalEvent]
    private void OnDestruction(Entity<ItemSlotsComponent> ent, ref DestructionEventArgs args)
    {
        EjectOnBreak(ent);
    }

    /// <summary>
    /// Eject items from slots configured to do so when the entity is destroyed.
    /// </summary>
    private void EjectOnBreak(Entity<ItemSlotsComponent> ent)
    {
        EjectFromAllSlots(ent, slot => slot.EjectOnBreak);
    }

    /// <summary>
    /// Given a new item slot, store it in the <see cref="ItemSlotsComponent"/> and ensure the slot has an item
    /// container.
    /// </summary>
    public void AddItemSlot(EntityUid uid, string id, ItemSlot slot, ItemSlotsComponent? itemSlots = null)
    {
        itemSlots ??= EnsureComp<ItemSlotsComponent>(uid);
        DebugTools.AssertOwner(uid, itemSlots);

        if (itemSlots.Slots.TryGetValue(id, out var existing))
        {
            if (existing.Local)
            {
                Log.Error(
                    $"Duplicate item slot key. Entity: {Comp<MetaDataComponent>(uid).EntityName} ({uid}), key: {id}");
            }
            else
                // Server state takes priority.
                slot.CopyFrom(existing);
        }

        slot.ContainerSlot = _containers.EnsureContainer<ContainerSlot>(uid, id);
        itemSlots.Slots[id] = slot;
        Dirty(uid, itemSlots);
    }

    /// <summary>
    /// Remove an item slot. This should generally be called whenever a component that added a slot is being
    /// removed.
    /// </summary>
    public void RemoveItemSlot(EntityUid uid, ItemSlot slot, ItemSlotsComponent? itemSlots = null)
    {
        if (Terminating(uid) || slot.ContainerSlot == null)
            return;

        _containers.ShutdownContainer(slot.ContainerSlot);

        // Don't log missing resolves. When an entity has all of its components removed, the ItemSlotsComponent may
        // have been removed before some other component that added an item slot (and is now trying to remove it).
        if (!Resolve(uid, ref itemSlots, logMissing: false))
            return;

        itemSlots.Slots.Remove(slot.ContainerSlot.ID);

        if (itemSlots.Slots.Count == 0)
            RemComp(uid, itemSlots);
        else
            Dirty(uid, itemSlots);
    }

    public bool TryGetSlot(EntityUid uid,
        string slotId,
        [NotNullWhen(true)] out ItemSlot? itemSlot,
        ItemSlotsComponent? component = null)
    {
        itemSlot = null;

        if (!Resolve(uid, ref component))
            return false;

        return component.Slots.TryGetValue(slotId, out itemSlot);
    }

    /// <summary>
    /// Get the contents of some item slot.
    /// </summary>
    /// <returns>The item in the slot, or null if the slot is empty or the entity doesn't have an <see cref="ItemSlotsComponent"/>.</returns>
    public EntityUid? GetItemOrNull(EntityUid uid, string id, ItemSlotsComponent? itemSlots = null)
    {
        if (!Resolve(uid, ref itemSlots, logMissing: false))
            return null;

        return itemSlots.Slots.GetValueOrDefault(id)?.Item;
    }

    /// <summary>
    /// Update the locked state of the managed item slots.
    /// </summary>
    /// <remarks>
    /// The slot's ContainerSlot performs its own networking, so the contained entity is not sent here.
    /// </remarks>
    [SubscribeLocalEvent]
    private void HandleItemSlotsState(Entity<ItemSlotsComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not ItemSlotsComponentState state)
            return;

        var removed = new List<ItemSlot>();
        foreach (var (key, slot) in ent.Comp.Slots)
        {
            if (!state.Slots.ContainsKey(key))
                removed.Add(slot);
        }

        foreach (var slot in removed)
        {
            RemoveItemSlot(ent, slot, ent.Comp);
        }

        foreach (var (serverKey, serverSlot) in state.Slots)
        {
            if (ent.Comp.Slots.TryGetValue(serverKey, out var itemSlot))
            {
                itemSlot.CopyFrom(serverSlot);
                itemSlot.ContainerSlot = _containers.EnsureContainer<ContainerSlot>(ent, serverKey);
            }
            else
            {
                var slot = new ItemSlot(serverSlot)
                {
                    Local = false
                };
                AddItemSlot(ent, serverKey, slot);
            }
        }
    }

    [SubscribeLocalEvent]
    private void GetItemSlotsState(Entity<ItemSlotsComponent> ent, ref ComponentGetState args)
    {
        args.State = new ItemSlotsComponentState(ent.Comp.Slots);
    }
}
