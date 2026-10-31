using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Shared.Containers.ItemSlots;

/// <summary>
/// Handles interactions related to inserting and ejecting items into and from item slots.
/// </summary>
public sealed partial class ItemSlotsSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private ISerializationManager _serializationManager = default!;

    [Dependency] private EntityQuery<ItemSlotsComponent> _itemSlotsQuery;
    [Dependency] private EntityQuery<HandsComponent> _handsQuery;

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
    /// Eject items from slots configured to do so when the entity is broken or destroyed.
    /// </summary>
    private void EjectOnBreak(Entity<ItemSlotsComponent> ent)
    {
        EjectFromAllSlots(ent, slot => slot.EjectOnBreak);
    }

    private void CopySlotState(ItemSlot source, ref ItemSlot target)
    {
        // These fields are DataFields, so SerializationManager would copy them,
        // but they are excluded from network serialization by NonSerialized.
        // Preserve the local values when applying received slot state.
        var startingItem = target.StartingItem;
        var ejectOnDeconstruct = target.EjectOnDeconstruct;
        var ejectOnBreak = target.EjectOnBreak;

        _serializationManager.CopyTo(source, ref target, notNullableOverride: true);

        target.StartingItem = startingItem;
        target.EjectOnDeconstruct = ejectOnDeconstruct;
        target.EjectOnBreak = ejectOnBreak;
    }

    /// <summary>
    /// Stores a new item slot in the <see cref="ItemSlotsComponent"/> and ensures that it has a backing container.
    /// </summary>
    /// <remarks>
    /// If a local slot replaces one created from component state, the received state is copied onto the local slot.
    /// </remarks>
    public void AddItemSlot(Entity<ItemSlotsComponent?> ent, string id, ItemSlot slot)
    {
        ent.Comp ??= EnsureComp<ItemSlotsComponent>(ent);
        DebugTools.AssertOwner(ent, ent.Comp);

        if (ent.Comp.Slots.TryGetValue(id, out var existing))
        {
            if (existing.Local)
            {
                Log.Error(
                    $"Duplicate item slot key. Entity: {Comp<MetaDataComponent>(ent).EntityName} ({ent.Owner}), key: {id}");
            }
            else
                // Server state takes priority.
                CopySlotState(existing, ref slot);
        }

        slot.ContainerSlot = _containers.EnsureContainer<ContainerSlot>(ent, id);
        ent.Comp.Slots[id] = slot;
        Dirty(ent);
    }

    /// <summary>
    /// Removes an item slot. This should generally be called whenever a component that added a slot is removed.
    /// </summary>
    public void RemoveItemSlot(Entity<ItemSlotsComponent?> ent, ItemSlot slot)
    {
        if (Terminating(ent) || slot.ContainerSlot == null)
            return;

        _containers.ShutdownContainer(slot.ContainerSlot);

        // Don't log missing resolves. When an entity has all of its components removed, the ItemSlotsComponent may
        // have been removed before some other component that added an item slot (and is now trying to remove it).
        if (!_itemSlotsQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Slots.Remove(slot.ContainerSlot.ID);

        if (ent.Comp.Slots.Count == 0)
            RemComp(ent, ent.Comp);
        else
            Dirty(ent);
    }

    public bool TryGetSlot(Entity<ItemSlotsComponent?> ent,
        string slotId,
        [NotNullWhen(true)] out ItemSlot? itemSlot)
    {
        itemSlot = null;

        if (!_itemSlotsQuery.Resolve(ent, ref ent.Comp))
            return false;

        return ent.Comp.Slots.TryGetValue(slotId, out itemSlot);
    }

    /// <summary>
    /// Get the contents of some item slot.
    /// </summary>
    /// <returns>The item in the slot, or null if the slot is empty or the entity doesn't have an <see cref="ItemSlotsComponent"/>.</returns>
    public EntityUid? GetItemOrNull(Entity<ItemSlotsComponent?> ent, string id)
    {
        if (!_itemSlotsQuery.Resolve(ent, ref ent.Comp, false))
            return null;

        return ent.Comp.Slots.GetValueOrDefault(id)?.Item;
    }

    /// <summary>
    /// Reconciles local slot registrations and their serialized configuration with received component state.
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
            RemoveItemSlot((ent.Owner, ent.Comp), slot);
        }

        foreach (var (serverKey, serverSlot) in state.Slots)
        {
            if (ent.Comp.Slots.TryGetValue(serverKey, out var itemSlot))
            {
                CopySlotState(serverSlot, ref itemSlot);
                itemSlot.ContainerSlot = _containers.EnsureContainer<ContainerSlot>(ent, serverKey);
            }
            else
            {
                var slot = new ItemSlot();
                CopySlotState(serverSlot, ref slot);
                slot.Local = false;
                AddItemSlot((ent.Owner, ent.Comp), serverKey, slot);
            }
        }

        ent.Comp.AllowSmartEquip = state.AllowSmartEquip;
    }

    [SubscribeLocalEvent]
    private void GetItemSlotsState(Entity<ItemSlotsComponent> ent, ref ComponentGetState args)
    {
        args.State = new ItemSlotsComponentState(ent.Comp.Slots, ent.Comp.AllowSmartEquip);
    }
}
