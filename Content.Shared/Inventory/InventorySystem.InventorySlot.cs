using System.Diagnostics.CodeAnalysis;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;

namespace Content.Shared.Inventory;

public partial class InventorySystem
{
    void InitializeInventorySlot()
    {
        SubscribeLocalEvent<InventorySlotComponent, GotEquippedEvent>(OnInvSlotGotEquipped);
        SubscribeLocalEvent<InventorySlotComponent, GotUnequippedEvent>(OnInvSlotGotUnequipped);
        SubscribeLocalEvent<InventorySlotComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<InventorySlotComponent, EntInsertedIntoContainerMessage>(OnSlotEntInserted);
        SubscribeLocalEvent<InventorySlotComponent, EntRemovedFromContainerMessage>(OnSlotEntRemoved);
    }

    private void OnSlotEntRemoved(EntityUid uid, InventorySlotComponent component, EntRemovedFromContainerMessage args)
    {
        var parent = Transform(uid).ParentUid;
        if (!TryInvCompWarn(parent, out var invComp))
            return;

        OnEntRemoved(parent, invComp, args);
    }

    private void OnSlotEntInserted(EntityUid uid, InventorySlotComponent component, EntInsertedIntoContainerMessage args)
    {
        var parent = Transform(uid).ParentUid;
        if (!TryInvCompWarn(parent, out var invComp))
            return;

        OnEntInserted(parent, invComp, args);
    }

    private void OnEquipAttempt(EntityUid uid, InventorySlotComponent component, BeingEquippedAttemptEvent args)
    {
        if (!TryInvCompWarn(args.Equipee, out var invComp))
        {
            args.Cancel();
            return;
        }

        foreach (var invSlotSlotDef in component.Slots)
        {
            if (!_prototypeManager.TryIndex<InventoryTemplatePrototype>(invComp.TemplateId, out var template))
                return;
            foreach (var invSlotDef in template.Slots)
            {
                if (invSlotDef.ConflictsWith(invSlotSlotDef))
                {
                    Logger.Error($"Found conflicting slots {invSlotDef.Name} and {invSlotSlotDef.Name} when trying to equip invslotcomp.");
                    args.Cancel();
                    return;
                }
            }
        }
    }

    private bool TryInvCompWarn(EntityUid uid, [NotNullWhen(true)] out InventoryComponent component)
    {
        if (!TryComp(uid, out component!))
        {
            Logger.Error("Could not find inventorycomponent on entity that equipped an inventoryslot-entity.");
            return false;
        }

        return true;
    }

    private void OnInvSlotGotUnequipped(EntityUid uid, InventorySlotComponent component, GotUnequippedEvent args)
    {
        if(!TryInvCompWarn(args.Equipee, out var invComp)) return;

        invComp.InventorySlotSlots.Remove(args.Slot);
    }

    private void OnInvSlotGotEquipped(EntityUid uid, InventorySlotComponent component, GotEquippedEvent args)
    {
        if(!TryInvCompWarn(args.Equipee, out var invComp)) return;

        invComp.InventorySlotSlots.Add(args.Slot);
    }
}
