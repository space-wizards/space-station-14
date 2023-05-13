using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Radio.Components;

namespace Content.Shared.Radio.EntitySystems;

public abstract class SharedHeadsetSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeadsetComponent, InventoryRelayedEvent<GetDefaultRadioChannelEvent>>(OnGetDefault);
        SubscribeLocalEvent<HeadsetComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<HeadsetComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGetDefault(EntityUid uid, HeadsetComponent component, InventoryRelayedEvent<GetDefaultRadioChannelEvent> args)
    {
        if (!component.Enabled || !component.IsEquipped)
        {
            // don't provide default channels from pocket slots.
            return;
        }

        if (TryComp(uid, out EncryptionKeyHolderComponent? keyHolder))
            args.Args.Channel ??= keyHolder.DefaultChannel; 
    }

    protected virtual void OnGotEquipped(EntityUid uid, HeadsetComponent component, GotEquippedEvent args)
    {
        component.IsEquipped = args.SlotFlags.HasFlag(component.RequiredSlot);
    }

    protected virtual void OnGotUnequipped(EntityUid uid, HeadsetComponent component, GotUnequippedEvent args)
    {
        component.IsEquipped = false;
    }
}
