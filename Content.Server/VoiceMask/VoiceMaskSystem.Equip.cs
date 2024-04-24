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
        comp.SpeechVerb = component.LastSpeechVerb;

        _actions.AddAction(user, ref component.ActionEntity, component.Action, uid);
    }

    private void OnUnequip(EntityUid uid, VoiceMaskerComponent compnent, GotUnequippedEvent args)
    {
        RemComp<VoiceMaskComponent>(args.Equipee);
    }

    private VoiceMaskerComponent? TryGetMask(EntityUid user)
    {
        if (!HasComp<VoiceMaskComponent>(user) || !_inventory.TryGetSlotEntity(user, MaskSlot, out var maskEntity))
            return null;

        return CompOrNull<VoiceMaskerComponent>(maskEntity);
    }

    private void TrySetLastKnownName(EntityUid user, string name)
    {
        if (TryGetMask(user) is {} comp)
            comp.LastSetName = name;
    }

    private void TrySetLastSpeechVerb(EntityUid user, string? verb)
    {
        if (TryGetMask(user) is {} comp)
            comp.LastSpeechVerb = verb;
    }
}
