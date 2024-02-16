using Content.Server.Actions;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Server.VoiceMask;

// This partial deals with equipment, i.e., the syndicate voice mask.
public sealed partial class VoiceMaskSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;

    private const string MaskSlot = "mask";

    private void OnEquip(EntityUid uid, VoiceMaskerComponent component, GotEquippedEvent args)
    {
        var user = args.Equipee;
        // have to be wearing the mask to use it, duh.
        if (!_inventory.TryGetSlotEntity(user, MaskSlot, out var maskEntity) || maskEntity != uid)
            return;

        var comp = EnsureComp<VoiceMaskComponent>(user);
        comp.VoiceName = component.LastSetName;

        _actions.AddAction(user, ref component.ActionEntity, component.Action, uid);
    }

    private void OnUnequip(EntityUid uid, VoiceMaskerComponent compnent, GotUnequippedEvent args)
    {
        RemComp<VoiceMaskComponent>(args.Equipee);
    }

    private void TrySetLastKnownName(EntityUid maskWearer, string lastName)
    {
        if (!HasComp<VoiceMaskComponent>(maskWearer)
            || !_inventory.TryGetSlotEntity(maskWearer, MaskSlot, out var maskEntity)
            || !TryComp<VoiceMaskerComponent>(maskEntity, out var maskComp))
        {
            return;
        }

        maskComp.LastSetName = lastName;
    }
}
