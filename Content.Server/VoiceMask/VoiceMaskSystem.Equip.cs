using Content.Shared.Inventory.Events;
using Content.Shared.Speech;

namespace Content.Server.VoiceMask;

public sealed partial class VoiceMaskSystem
{
    private void OnEquip(EntityUid uid, VoiceMaskerComponent component, GotEquippedEvent args)
    {
        EnsureComp<VoiceMaskComponent>(args.Equipee);
    }

    private void OnUnequip(EntityUid uid, VoiceMaskerComponent compnent, GotUnequippedEvent args)
    {
        RemCompDeferred<VoiceMaskComponent>(args.Equipee);
    }
}
