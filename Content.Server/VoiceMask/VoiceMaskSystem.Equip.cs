using Content.Server.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.VoiceMask;

// This partial deals with equipment, i.e., the syndicate voice mask.
public sealed partial class VoiceMaskSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    private const string MaskSlot = "mask";

    private void OnEquip(EntityUid uid, VoiceMaskerComponent component, GotEquippedEvent args)
    {
        var comp = EnsureComp<VoiceMaskComponent>(args.Equipee);
        comp.VoiceName = component.LastSetName;

        if (!_prototypeManager.TryIndex<InstantActionPrototype>(component.Action, out var action))
        {
            throw new ArgumentException("Could not get voice masking prototype.");
        }

        _actions.AddAction(args.Equipee, _serialization.Copy(action.Value.InstantAction), uid);
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
