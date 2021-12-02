using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Inventory.New.Events;
using Content.Shared.Item;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Shared.Inventory.New;

public partial class InventorySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    public void InitializeEquip()
    {

    }

    public void ShutdownEquip()
    {

    }

    public bool TryEquip(EntityUid uid, EntityUid itemUid, string slot, bool silent = false, bool force = false, InventoryComponent? inventory = null, SharedItemComponent? item = null)
    {
        if (!Resolve(uid, ref inventory) || !Resolve(uid, ref item))
        {
            if(!silent) _popup.PopupCursor(Loc.GetString("inventory-component-can-equip-cannot"), Filter.Local());
            return false;
        }

        if (!TryGetSlotContainer(uid, slot, out var slotContainer, out var slotDefinition, inventory))
        {
            if(!silent) _popup.PopupCursor(Loc.GetString("inventory-component-can-equip-cannot"), Filter.Local());
            return false;
        }

        if (!CanEquip(uid, itemUid, slot, out var reason, force, slotDefinition, inventory, item))
        {
            if(!silent) _popup.PopupCursor(Loc.GetString(reason), Filter.Local());
            return false;
        }

        if (!slotContainer.Insert(EntityManager.GetEntity(itemUid)))
        {
            if(!silent)  _popup.PopupCursor(Loc.GetString("inventory-component-on-equip-cannot"), Filter.Local());
            return false;
        }

        if(item.EquipSound != null)
            SoundSystem.Play(Filter.Pvs(uid), item.EquipSound.GetSound(), uid, AudioParams.Default.WithVolume(-2f));

        var equippedEvent = new DidEquipEvent(uid, itemUid);
        RaiseLocalEvent(uid, equippedEvent);

        var gotEquippedEvent = new GotEquippedEvent(uid, itemUid);
        RaiseLocalEvent(uid, gotEquippedEvent);

        inventory.Dirty();

        _movementSpeed.RefreshMovementSpeedModifiers(uid);
        return true;
    }

    public bool CanEquip(EntityUid uid, EntityUid itemUid, string slot, [NotNullWhen(false)] out string? reason, bool force = false, SlotDefinition? slotDefinition = null, InventoryComponent? inventory = null, SharedItemComponent? item = null)
    {
        reason = "inventory-component-can-equip-cannot";
        if (!Resolve(uid, ref inventory) || !Resolve(itemUid, ref item))
            return false;

        if (slotDefinition == null && !TryGetSlot(uid, slot, out slotDefinition, inventory))
            return false;

        if (force) return true;

        if(!item.SlotFlags.HasFlag(slotDefinition.SlotFlags))
        {
            reason = "inventory-component-can-equip-does-not-fit";
            return false;
        }

        if (!_actionBlocker.CanEquip(uid))
            return false;

        var attemptEvent = new EquipAttemptEvent(uid, itemUid, slotDefinition.SlotFlags);
        RaiseLocalEvent(itemUid, attemptEvent);
        if (!attemptEvent.Cancelled) return true;

        if (attemptEvent.Reason != null)
            reason = attemptEvent.Reason;

        return false;
    }
}
