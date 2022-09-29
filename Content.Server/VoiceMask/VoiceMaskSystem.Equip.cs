using Content.Server.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Server.VoiceMask;

// This partial deals with equipment, i.e., the syndicate voice mask.
public sealed partial class VoiceMaskSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private const string MaskSlot = "mask";

    private void OnEquip(EntityUid uid, VoiceMaskerComponent component, GotEquippedEvent args)
    {
        var comp = EnsureComp<VoiceMaskComponent>(args.Equipee);
        comp.VoiceName = component.LastSetName;

        if (!_prototypeManager.TryIndex<InstantActionPrototype>(component.Action, out var action))
        {
            throw new ArgumentException("Could not get voice masking prototype.");
        }

        _actions.AddAction(args.Equipee, (InstantAction) action.Clone(), uid);
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
