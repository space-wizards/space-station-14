using Content.Shared.Inventory.Events;
using Content.Shared.Speech;

namespace Content.Server.VoiceMask;

public sealed partial class VoiceMaskSystem
{
    private void OnEquip(EntityUid uid, VoiceMaskerComponent component, GotEquippedEvent args)
    {
        var comp = EnsureComp<VoiceMaskComponent>(args.Equipee);
        comp.VoiceName = component.LastSetName;
    }

    private void OnUnequip(EntityUid uid, VoiceMaskerComponent compnent, GotUnequippedEvent args)
    {
        RemCompDeferred<VoiceMaskComponent>(args.Equipee);
    }
}
