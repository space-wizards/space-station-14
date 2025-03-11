using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Client._Starlight.Overlay;

public sealed class FlashImmunitySystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashImmunityComponent, GotEquippedEvent>(OnFlashImmunityEquipped);
        SubscribeLocalEvent<FlashImmunityComponent, GotUnequippedEvent>(OnFlashImmunityUnEquipped);

        SubscribeLocalEvent<FlashImmunityComponent, ComponentStartup>(OnFlashImmunityChanged);
        SubscribeLocalEvent<FlashImmunityComponent, ComponentShutdown>(OnFlashImmunityChanged);
    }

    private void OnFlashImmunityChanged(EntityUid uid, FlashImmunityComponent component, EntityEventArgs args)
    {
        FlashImmunityChangedEvent flashImmunityChangedEvent = new(uid, CheckForFlashImmunity(uid));
        RaiseLocalEvent(uid, flashImmunityChangedEvent);
    }

    private void OnFlashImmunityEquipped(EntityUid uid, FlashImmunityComponent component, GotEquippedEvent args)
    {
        FlashImmunityChangedEvent flashImmunityChangedEvent = new(uid, CheckForFlashImmunity(args.Equipee));
        RaiseLocalEvent(args.Equipee, flashImmunityChangedEvent);
    }

    private void OnFlashImmunityUnEquipped(EntityUid uid, FlashImmunityComponent component, GotUnequippedEvent args)
    {
        FlashImmunityChangedEvent flashImmunityChangedEvent = new(uid, CheckForFlashImmunity(args.Equipee));
        RaiseLocalEvent(args.Equipee, flashImmunityChangedEvent);
    }

    private bool CheckForFlashImmunity(EntityUid uid)
    {
        if (EntityManager.TryGetComponent(uid, out FlashImmunityComponent? flashImmunityComponent))
        {
            return true;
        }

        if (TryComp<InventoryComponent>(uid, out var inventoryComp))
        {
            //get all worn items
            var slots = _inventory.GetSlotEnumerator((uid, inventoryComp), SlotFlags.WITHOUT_POCKET);
            while (slots.MoveNext(out var slot))
            {
                if (slot.ContainedEntity != null && EntityManager.TryGetComponent(slot.ContainedEntity, out FlashImmunityComponent? wornFlashImmunityComponent))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
