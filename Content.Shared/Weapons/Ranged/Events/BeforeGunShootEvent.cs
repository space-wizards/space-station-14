using Content.Shared.Inventory;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Events;
/// <summary>
///     This event is triggered on an entity right before they shoot a gun.
/// </summary>
public sealed partial class SelfBeforeGunShotEvent : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
    public readonly EntityUid Shooter;
    public readonly Entity<GunComponent> Gun;
    public readonly List<(EntityUid? Entity, IShootable Shootable)> Ammo;
    public SelfBeforeGunShotEvent(EntityUid shooter, Entity<GunComponent> gun, List<(EntityUid? Entity, IShootable Shootable)> ammo)
    {
        Shooter = shooter;
        Gun = gun;
        Ammo = ammo;
    }
}
