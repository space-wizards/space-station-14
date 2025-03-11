using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Inventory.Events;

namespace Content.Client._Starlight.Overlay;

public sealed class FlashImmunitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashImmunityComponent, GotEquippedEvent>(OnFlashImmunityAdded);
        SubscribeLocalEvent<FlashImmunityComponent, GotUnequippedEvent>(OnFlashImmunityRemoved);
    }

    private void OnFlashImmunityAdded(EntityUid uid, FlashImmunityComponent component, GotEquippedEvent args)
    {
        FlashImmunityChangedEvent flashImmunityChangedEvent = new(uid, true);
        RaiseLocalEvent(args.Equipee, flashImmunityChangedEvent);
    }

    private void OnFlashImmunityRemoved(EntityUid uid, FlashImmunityComponent component, GotUnequippedEvent args)
    {
        FlashImmunityChangedEvent flashImmunityChangedEvent = new(uid, false);
        RaiseLocalEvent(args.Equipee, flashImmunityChangedEvent);
    }
}
