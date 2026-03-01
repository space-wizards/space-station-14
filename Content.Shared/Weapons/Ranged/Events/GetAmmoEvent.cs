using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised before a entity is loaded into a gun as ammo
/// </summary>
[ByRefEvent]
public struct GetAmmoEvent(Entity<BallisticAmmoProviderComponent> ballisticProvider)
{
    public Entity<BallisticAmmoProviderComponent> BallisticProvider => ballisticProvider;
    public bool CanLoad => true;
    public EntityUid? AmmoOverride;
}
