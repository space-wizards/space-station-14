using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;

namespace Content.Shared.Containers.ItemSlots;

public sealed partial class ItemSlotsSystem
{
    /// <summary>
    /// Attempt to take an item from a slot if any are set to EjectOnInteract.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnInteractHand(Entity<ItemSlotsComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        foreach (var slot in ent.Comp.Slots.Values)
        {
            if (!slot.EjectOnInteract || slot.Item == null || !CanEject(ent, args.User, slot, popup: args.User))
                continue;

            args.Handled = true;
            TryEjectToHands(ent, slot, args.User, true);
            break;
        }
    }

    /// <summary>
    /// Attempt to eject an item from the first valid item slot.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<ItemSlotsComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        foreach (var slot in ent.Comp.Slots.Values)
        {
            if (!slot.EjectOnUse || slot.Item == null || !CanEject(ent, args.User, slot, popup: args.User))
                continue;

            args.Handled = true;
            TryEjectToHands(ent, slot, args.User, true);
            break;
        }
    }

    /// <summary>
    /// Tries to insert a held item in any fitting item slot. If a valid slot already contains an item, it will
    /// swap it out and place the old one in the user's hand.
    /// </summary>
    /// <remarks>
    /// This only handles the event if the user has an applicable entity that can be inserted. This allows for
    /// other interactions to still happen (e.g., open UI, or toggle-open), despite the user holding an item.
    /// </remarks>
    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<ItemSlotsComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.User, out HandsComponent? hands))
            return;

        if (ent.Comp.Slots.Count == 0)
            return;

        var slots = new List<ItemSlot>();
        string? whitelistFailPopup = null;
        string? lockedFailPopup = null;

        foreach (var slot in ent.Comp.Slots.Values)
        {
            if (!slot.InsertOnInteract)
                continue;

            if (CanInsert(ent, args.Used, args.User, slot, slot.Swap))
            {
                slots.Add(slot);
            }
            else
            {
                var allowed = CanInsertWhitelist(args.Used, slot);
                if (lockedFailPopup == null && slot.LockedFailPopup != null && allowed && slot.Locked)
                    lockedFailPopup = slot.LockedFailPopup;

                if (whitelistFailPopup == null && slot.WhitelistFailPopup != null)
                    whitelistFailPopup = slot.WhitelistFailPopup;
            }
        }

        if (slots.Count == 0)
        {
            if (lockedFailPopup != null)
                _popupSystem.PopupEntity(Loc.GetString(lockedFailPopup), ent, args.User);
            else if (whitelistFailPopup != null)
                _popupSystem.PopupEntity(Loc.GetString(whitelistFailPopup), ent, args.User);
            return;
        }

        if (!_handsSystem.TryDrop(args.User, args.Used))
            return;

        slots.Sort(SortEmpty);

        foreach (var slot in slots)
        {
            if (slot.Item != null)
                _handsSystem.TryPickupAnyHand(args.User, slot.Item.Value, handsComp: hands);

            if (!TryInsert(ent, slot, args.Used, args.User, excludeUserAudio: true))
                return;

            if (slot.InsertSuccessPopup.HasValue)
                _popupSystem.PopupEntity(Loc.GetString(slot.InsertSuccessPopup), ent, args.User);

            args.Handled = true;
            return;
        }
    }

    [SubscribeLocalEvent]
    private void HandleButtonPressed(Entity<ItemSlotsComponent> ent, ref ItemSlotButtonPressedEvent args)
    {
        if (!ent.Comp.Slots.TryGetValue(args.SlotId, out var slot))
            return;

        if (args.TryEject && slot.HasItem && !slot.DisableEject)
            TryEjectToHands(ent, slot, args.Actor, true);
        else if (args.TryInsert && !slot.HasItem)
            TryInsertFromHand(ent, slot, args.Actor);
    }
}
