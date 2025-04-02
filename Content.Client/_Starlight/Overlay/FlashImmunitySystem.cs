using Content.Shared.Clothing.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Player;

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
        SubscribeLocalEvent<FlashImmunityComponent, ComponentRemove>(OnFlashImmunityChanged);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);

        SubscribeLocalEvent<NightVisionComponent, ComponentStartup>(OnVisionChanged);
        SubscribeLocalEvent<NightVisionComponent, ComponentRemove>(OnVisionChanged); //this should use shutdown, but something else is already using it.....

        SubscribeLocalEvent<ThermalVisionComponent, ComponentStartup>(OnVisionChanged);
        SubscribeLocalEvent<ThermalVisionComponent, ComponentRemove>(OnVisionChanged); //this should use shutdown, but something else is already using it.....

        SubscribeLocalEvent<CycloritesVisionComponent, ComponentStartup>(OnVisionChanged);
        SubscribeLocalEvent<CycloritesVisionComponent, ComponentRemove>(OnVisionChanged); //this should use shutdown, but something else is already using it.....
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        FlashImmunityChangedEvent flashImmunityChangedEvent = new(args.Entity, CheckForFlashImmunity(args.Entity));
        RaiseLocalEvent(args.Entity, flashImmunityChangedEvent);
    }

    private void OnFlashImmunityChanged(EntityUid uid, FlashImmunityComponent component, EntityEventArgs args)
    {
        uid = GetPossibleWearer(uid);
        FlashImmunityChangedEvent flashImmunityChangedEvent = new(uid, CheckForFlashImmunity(uid));
        RaiseLocalEvent(uid, flashImmunityChangedEvent);
    }

    private void OnVisionChanged(EntityUid uid, Component component, EntityEventArgs args)
    {
        uid = GetPossibleWearer(uid);
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

    private EntityUid GetPossibleWearer(EntityUid uid)
    {
        if (TryComp<ClothingComponent>(uid, out var clothingComponent))
        {
            //we want to get the wearer of the clothing, not the clothing itself
            return IoCManager.Resolve<IEntityManager>().GetComponentOrNull<TransformComponent>(clothingComponent.Owner)?.ParentUid ?? uid;
        }

        return uid;
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
