using System.Diagnostics.CodeAnalysis;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    private void InitializeClothing()
    {
        SubscribeLocalEvent<ClothingSlotAmmoProviderComponent, TakeAmmoEvent>(OnClothingTakeAmmo);
        SubscribeLocalEvent<ClothingSlotAmmoProviderComponent, GetAmmoCountEvent>(OnClothingAmmoCount);
    }

    private void OnClothingTakeAmmo(EntityUid uid, ClothingSlotAmmoProviderComponent component, TakeAmmoEvent args)
    {
        if (!TryGetClothingSlotEntity(uid, component, out var entity))
            return;
        RaiseLocalEvent(entity.Value, args);
    }

    private void OnClothingAmmoCount(EntityUid uid, ClothingSlotAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        if (!TryGetClothingSlotEntity(uid, component, out var entity))
            return;
        RaiseLocalEvent(entity.Value, ref args);
    }

    private bool TryGetClothingSlotEntity(EntityUid uid, ClothingSlotAmmoProviderComponent component, [NotNullWhen(true)] out EntityUid? slotEntity)
    {
        slotEntity = null;
        if (!_container.TryGetContainingContainer(uid, out var container))
            return false;
        var user = container.Owner;

        if (!TryComp<InventoryComponent>(user, out var inventory))
            return false;
        var slots = _inventory.GetSlots(user, inventory);
        foreach (var slot in slots)
        {
            if (slot.SlotFlags != component.TargetSlot)
                continue;
            if (!_inventory.TryGetSlotEntity(user, slot.Name, out var e, inventory))
                continue;
            if (component.ProviderWhitelist != null && !component.ProviderWhitelist.IsValid(e.Value, EntityManager))
                continue;
            slotEntity = e;
        }

        return slotEntity != null;
    }
}
