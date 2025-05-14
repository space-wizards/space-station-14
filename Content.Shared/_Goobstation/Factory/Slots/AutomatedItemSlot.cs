// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._Goobstation.Factory.Slots;

/// <summary>
/// Abstraction over an <see cref="ItemSlot"/> on the machine.
/// </summary>
public sealed partial class AutomatedItemSlot : AutomationSlot
{
    /// <summary>
    /// The name of the slot to automate.
    /// </summary>
    [DataField(required: true)]
    public string SlotId = string.Empty;

    private ItemSlotsSystem _slots;

    [ViewVariables]
    public ItemSlot Slot;

    public override void Initialize()
    {
        base.Initialize();

        _slots = EntMan.System<ItemSlotsSystem>();

        if (_slots.TryGetSlot(Owner, SlotId, out var slot))
            Slot = slot;
        else
            throw new InvalidOperationException($"Entity {EntMan.ToPrettyString(Owner)} had no item slot {SlotId}");
    }

    public override bool Insert(EntityUid item)
    {
        return base.Insert(item) &&
            _slots.TryInsert(Owner, Slot, item, user: null);
    }

    public override bool CanInsert(EntityUid item)
    {
        return base.CanInsert(item) &&
            _slots.CanInsert(Owner, usedUid: item, user: null, Slot);
    }

    public override EntityUid? GetItem(EntityUid? filter)
    {
        if (Slot.Item is not {} item || _filter.IsBlocked(filter, item))
            return null;

        return item;
    }
}
