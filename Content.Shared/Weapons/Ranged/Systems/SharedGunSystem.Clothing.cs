using Content.Shared.Containers;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SlotBasedConnectedContainerSystem _slotBasedConnectedContainer = default!;

    private void InitializeClothing()
    {
        SubscribeLocalEvent<ClothingSlotAmmoProviderComponent, TakeAmmoEvent>(OnClothingTakeAmmo);
        SubscribeLocalEvent<ClothingSlotAmmoProviderComponent, GetAmmoCountEvent>(OnClothingAmmoCount);
    }

    private void OnClothingTakeAmmo(EntityUid uid, ClothingSlotAmmoProviderComponent component, TakeAmmoEvent args)
    {
        if (!_slotBasedConnectedContainer.TryGetConnectedContainer(uid, out var entity))
            return;
        RaiseLocalEvent(entity.Value, args);
    }

    private void OnClothingAmmoCount(EntityUid uid, ClothingSlotAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        if (!_slotBasedConnectedContainer.TryGetConnectedContainer(uid, out var entity))
            return;
        RaiseLocalEvent(entity.Value, ref args);
    }
}
