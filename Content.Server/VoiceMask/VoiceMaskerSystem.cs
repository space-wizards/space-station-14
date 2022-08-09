using Content.Shared.Inventory.Events;

namespace Content.Server.VoiceMask;

public sealed class VoiceMaskerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<VoiceMaskerComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<VoiceMaskerComponent, GotUnequippedEvent>(OnUnequip);
    }

    private void OnEquip(EntityUid uid, VoiceMaskerComponent component, GotEquippedEvent args)
    {
        EnsureComp<VoiceMaskComponent>(args.Equipee);
    }

    private void OnUnequip(EntityUid uid, VoiceMaskerComponent compnent, GotUnequippedEvent args)
    {
        RemCompDeferred<VoiceMaskComponent>(args.Equipee);
    }
}
